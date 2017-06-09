using AccessBattle.Networking.Packets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AccessBattle.Networking
{
    /// <summary>
    /// Args for game list events.
    /// </summary>
    public class GameListEventArgs : EventArgs
    {
        /// <summary>List of games.</summary>
        public List<GameInfo> GameList { get; private set; }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gameList">List of games.</param>
        public GameListEventArgs(List<GameInfo> gameList)
        {
            GameList = gameList;
        }
    }

    /// <summary>
    /// Args for game created events.
    /// </summary>
    public class GameCreatedEventArgs : EventArgs
    {
        /// <summary>Game information.</summary>
        public GameInfo GameInfo { get; private set; }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gameInfo">Game information.</param>
        public GameCreatedEventArgs(GameInfo gameInfo)
        {
            GameInfo = gameInfo;
        }
    }

    /// <summary>
    /// Args for game join request events.
    /// </summary>
    public class GameJoinRequestedEventArgs : EventArgs
    {
        /// <summary>JoinMessage.</summary>
        public JoinMessage Message;
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Join message.</param>
        public GameJoinRequestedEventArgs(JoinMessage message)
        {
            Message = new JoinMessage
            {
                JoiningUser = message.JoiningUser,
                Request = message.Request,
                UID = message.UID
            };
        }
    }

    /// <summary>
    /// Event for login.
    /// </summary>
    public class LoggedInEventArgs : EventArgs
    {
        /// <summary>Login success flag.</summary>
        public bool LoggedIn { get; private set; }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="loggedIn">Login status.</param>
        public LoggedInEventArgs(bool loggedIn)
        {
            LoggedIn = loggedIn;
        }
    }

    /// <summary>
    /// Args for server info.
    /// </summary>
    public class ServerInfoEventArgs : EventArgs
    {
        /// <summary>
        /// Information about the server.
        /// </summary>
        public ServerInfo Info { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info"></param>
        public ServerInfoEventArgs(ServerInfo info)
        {
            Info = info;
        }
    }

    /// <summary>
    /// Class for network clients. Used for communication with the server.
    /// </summary>
    public class NetworkGameClient : NetworkBase
    {
        /// <summary>Timeout for network operations in seconds.</summary>
        const int NetworkTimeout = 20;

        /// <summary>Game list was received.</summary>
        public event EventHandler<GameListEventArgs> GameListReceived;
        /// <summary>A game was created.</summary>
        public event EventHandler<GameCreatedEventArgs> GameCreated;
        /// <summary>Game join was requested.</summary>
        public event EventHandler<GameJoinRequestedEventArgs> GameJoinRequested;
        /// <summary>Logged into server or login failed.</summary>
        public event EventHandler<LoggedInEventArgs> LoggedIn;
        /// <summary>Received server info. This normally happens during connect.</summary>
        public event EventHandler<ServerInfoEventArgs> ServerInfoReceived;

        bool? _serverRequiresLogin;
        /// <summary>
        /// True if the currently connected server requires a login with username and password.
        /// A login is always required. If this value is false, it only means the user can use anly login values.
        /// </summary>
        public bool? ServerRequiresLogin
        {
            get { return _serverRequiresLogin; }
            set { _serverRequiresLogin = value; }
        }

        bool? _isConnected = false;
        /// <summary>Is connected to server. Null during connect.</summary>
        public bool? IsConnected
        {
            get { return _isConnected; }
            private set
            {
                _isConnected = value;
                if (_isConnected == false)
                {
                    IsLoggedIn = false;
                }
            }
        }

        bool? _isJoined = false;
        /// <summary>Has joined a game if true.</summary>
        public bool? IsJoined
        {
            get { return _isJoined; }
            private set { _isJoined = value; }
        }

        bool? _isLoggedIn = false;
        /// <summary>Login status. Is null during log in.</summary>
        public bool? IsLoggedIn
        {
            get { return _isLoggedIn; }
            private set
            {
                _isLoggedIn = value;
                if (IsLoggedIn == false)
                {
                    IsJoined = false;
                }
            }
        }

        /// <summary>Login name</summary>
        public string LoginName { get; private set; }

        ByteBuffer _receiveBuffer = new ByteBuffer(4096);

        CryptoHelper _encrypter;
        CancellationTokenSource _encrypterWaiter = null;

        CryptoHelper _decrypter;
        Socket _connection;
        readonly object _clientLock = new object();

        /// <summary>Constructor.</summary>
        public NetworkGameClient()
        {
            _decrypter = new CryptoHelper();
        }

        /// <summary>
        /// Connects to the server and returns after the keys have been exchanged.
        /// </summary>
        /// <param name="server">Server ip address or host name.</param>
        /// <param name="port">Server port.</param>
        /// <returns>True if connection was established and keys could be exchanged.</returns>
        /// <remarks>
        /// If the connect worked but the key exchange not, a disconnect is performed.
        /// </remarks>
        public async Task<bool> Connect(string server, ushort port)
        {
            if (IsConnected == null) return false;
            lock (_clientLock)
            {
                if (IsConnected == null) return false;
                Disconnect();
                IsConnected = null;
                Log.WriteLine("NetworkGameClient: Connecting...");
            }
            try
            {
                _connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                var connectTcs = new TaskCompletionSource<bool>();
                using (var args = new SocketAsyncEventArgs())
                {
                    EventHandler<SocketAsyncEventArgs> connectHandler = (s, e) =>
                    {
                        connectTcs.TrySetResult(true);
                    };
                    args.Completed += connectHandler;
                    args.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(server), port);

                    using (var tcs = new CancellationTokenSource(TimeSpan.FromSeconds(NetworkTimeout)))
                    {
                        tcs.Token.Register(() =>
                        {
                            connectTcs.TrySetResult(true);
                        });
                        if (_connection.ConnectAsync(args))
                        {
                            await connectTcs.Task;
                        }
                    }
                    if (!_connection.Connected)
                    {
                        _connection.Dispose();
                        IsConnected = false;
                        return false;
                    }
                }

                ReceiveAsync(_connection, 0);

                // Block until keys are exchanged
                var keyExchangeTcs = new TaskCompletionSource<bool>();
                using (_encrypterWaiter = new CancellationTokenSource(TimeSpan.FromSeconds(NetworkTimeout)))
                {
                    _encrypterWaiter.Token.Register(() => { keyExchangeTcs.SetResult(true); });
                    await keyExchangeTcs.Task;
                }
                if (_encrypter == null)
                {
                    Disconnect();
                    Log.WriteLine("NetworkGameClient: Connect failed: No key received from server!");
                    IsConnected = false;
                    return false;
                }
                // Send own public key
                Send(_decrypter.GetPublicKey(), NetworkPacketType.PublicKey, _connection, _encrypter);
            }
            catch (Exception e)
            {
                Log.WriteLine("NetworkGameClient: Connect failed: " + e.Message);
                IsConnected = false;
                return false;
            }
            Log.WriteLine("NetworkGameClient: Connect success!");
            IsConnected = true;
            return true;
        }

        /// <summary>
        /// Tries to login to the server using a username and password.
        /// </summary>
        /// <param name="name">Login name.</param>
        /// <param name="password">Login password.</param>
        /// <returns>True if login was successful.</returns>
        public async Task<bool> Login(string name, string password)
        {
            if (string.IsNullOrEmpty(name))
            {
                Log.WriteLine("Game Client: Cannot login with empty user name!");
                return false;
            }
            IsLoggedIn = null;
            LoginName = "";
            var login = new Login { Name = name, Password = password };

            // Catch the event for receiving the list.
            LoggedInEventArgs result = null;
            var source = new TaskCompletionSource<LoggedInEventArgs>();
            EventHandler<LoggedInEventArgs> handler = (sender, args) =>
            {
                result = args;
                source.TrySetResult(args);
            };

            // In case no answer was received, we have to cancel
            using (var ct = new CancellationTokenSource(NetworkTimeout * 1000))
            {
                ct.Token.Register(() => source.TrySetResult(null));

                try
                {
                    LoggedIn += handler;
                    if (Send(JsonConvert.SerializeObject(login), NetworkPacketType.ClientLogin))
                    {
                        result = await source.Task;
                    }
                }
                catch (Exception e) { Log.WriteLine("NetworkGameClient::Login(): " + e.Message); }
                finally { LoggedIn -= handler; }
            }

            if (result?.LoggedIn == true)
            {
                LoginName = name;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Requests a list of current games from the server.
        /// </summary>
        /// <returns>List of games on success, null on error..</returns>
        public async Task<List<GameInfo>> RequestGameList()
        {
            GameListEventArgs result = null;

            // Catch the event for receiving the list.
            var source = new TaskCompletionSource<GameListEventArgs>();
            EventHandler<GameListEventArgs> handler = (sender, args) =>
            {
                result = args;
                source.TrySetResult(args);
            };

            // In case no answer was received, we have to cancel
            using (var ct = new CancellationTokenSource(NetworkTimeout * 1000))
            {
                ct.Token.Register(() => source.TrySetResult(null));

                try
                {
                    GameListReceived += handler;
                    if (Send(new byte[0], NetworkPacketType.ListGames))
                    {
                        result = await source.Task;
                    }
                }
                catch (Exception e) { Log.WriteLine("NetworkGameClient::RequestGameList(): " + e.Message); }
                finally { GameListReceived -= handler; }
            }
            return result?.GameList;
        }

        /// <summary>
        /// Creates a new game on the server.
        /// </summary>
        /// <param name="gameName">Name of the game.</param>
        /// <returns>Info for the created game on success, else null.</returns>
        public async Task<GameInfo> CreateGame(string gameName)
        {
            if (IsLoggedIn != true || string.IsNullOrEmpty(LoginName))
            {
                Log.WriteLine("Game Client: Cannot create game when not logged in!");
                return null;
            }

            GameCreatedEventArgs result = null;

            var info = new GameInfo
            {
                UID = 0,
                Name = gameName,
                Player1 = LoginName
            };

            // Catch the event for creating the game.
            var source = new TaskCompletionSource<GameCreatedEventArgs>();
            EventHandler<GameCreatedEventArgs> handler = (sender, args) =>
            {
                result = args;
                source.TrySetResult(args);
            };

            // In case no answer was received, we have to cancel
            using (var ct = new CancellationTokenSource(NetworkTimeout * 1000))
            {
                ct.Token.Register(() => source.TrySetResult(null));

                try
                {
                    GameCreated += handler;
                    if (Send(JsonConvert.SerializeObject(info), NetworkPacketType.CreateGame))
                    {
                        result = await source.Task;
                    }
                }
                catch (Exception e) { Log.WriteLine("NetworkGameClient::CreateGame(): " + e.Message); }
                finally { GameCreated -= handler; }
            }

            // Do not set Game UID here.
            // It is better if the client program sets the UID after checking the game info.
            return result?.GameInfo;
        }

        async Task<bool> ExecuteGameCommand()
        {
            if (IsConnected != true || IsLoggedIn != true) return false;

            // TODO Send packet to server and return answer
            await Task.Delay(1);

            return false;
        }

        /// <summary>
        /// Request to join a game on the server. Other player has to confirm.
        /// </summary>
        /// <param name="uid">UID of the game.</param>
        /// <returns>true if the request was sent.</returns>
        public bool RequestJoinGame(uint uid)
        {
            try
            {
                var req = new JoinMessage { UID = uid, Request = 0 };
                IsJoined = null;
                return Send(JsonConvert.SerializeObject(req), NetworkPacketType.JoinGame);
            }
            catch (Exception e)
            {
                Log.WriteLine("NetworkGameClient::RequestJoinGame(): " + e.Message);
                IsJoined = false;
                return false;
            }
        }

        /// <summary>
        /// Used to accept and confirm a join. Used by both players.
        /// </summary>
        /// <param name="uid">UID of game.</param>
        /// <param name="accept">true if join is accepted, else false.</param>
        /// <returns>true if the request was sent.</returns>
        public bool ConfirmJoin(uint uid, bool accept)
        {
            try
            {
                var req = new JoinMessage { UID = uid, Request = accept ? 3 : 4 };
                IsJoined = Send(JsonConvert.SerializeObject(req), NetworkPacketType.JoinGame);
                return IsJoined == true;
            }
            catch (Exception e)
            {
                Log.WriteLine("NetworkGameClient::AnswerJoinRequest(): " + e.Message);
                IsJoined = false;
                return false;
            }
        }

        /// <summary>
        /// Handler for the ReceiveAsync method.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event args.</param>
        protected override void Receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError != SocketError.Success)
                {
                    // Will be hit after client disconnect
                    // TODO Supress error message on clean disconnect
                    Log.WriteLine("NetworkGameClient: Receive not successful: " + e.SocketError);
                }
                else if (e.BytesTransferred > 0)
                {
                    Log.WriteLine("NetworkGameClient: Received " + e.BytesTransferred + " bytes of data");
                    _receiveBuffer.Add(e.Buffer, 0, e.BytesTransferred);
                    Log.WriteLine("NetworkGameClient: Receive buffer has now " + _receiveBuffer.Length + " bytes of data");

                    byte[] packData;
                    while (_receiveBuffer.Take(NetworkPacket.STX, NetworkPacket.ETX, out packData))
                    {
                        Log.WriteLine("NetworkGameClient: Received full packet");
                        var packet = NetworkPacket.FromByteArray(packData);
                        if (packet == null)
                            Log.WriteLine("NetworkGameClient: Could not parse packet!");
                        else
                        {
                            Log.WriteLine("NetworkGameClient: Received packet of type " + packet.PacketType + " with " + packet.Data.Length + " bytes of data");
                            ProcessPacket(packet);
                        }
                    }
                }

            }
            // No catch for now. Exceptions will crash the program.
            finally
            {
                e.Dispose();
                // Call not required after disconnect
                if (_connection != null) ReceiveAsync(_connection, 0);
            }
        }

        /// <summary>
        /// Processes received packets.
        /// </summary>
        /// <param name="packet">Received packet.</param>
        void ProcessPacket(NetworkPacket packet)
        {
            // Decrypt data
            var data = packet.Data;
            if (data != null && data.Length > 0 && packet.PacketType != NetworkPacketType.PublicKey)
            {
                data = _decrypter.Decrypt(packet.Data);
                if (data == null)
                {
                    Log.WriteLine("NetworkGameClient: Error! Could not decrypt message from server");
                    return;
                }
            }
            switch (packet.PacketType)
            {
                case NetworkPacketType.PublicKey:
                    try
                    {
                        var serverPubKey = Encoding.ASCII.GetString(packet.Data);
                        _encrypter = new CryptoHelper(serverPubKey);
                    }
                    catch (Exception ex)
                    {
                        _encrypter = null;
                        Log.WriteLine("NetworkGameClient: Received key is invalid! " + ex.Message);
                    }
                    finally { _encrypterWaiter?.Cancel(); }
                    break;
                case NetworkPacketType.ListGames:
                    try
                    {
                        var glistString = Encoding.ASCII.GetString(data);
                        var glist = JsonConvert.DeserializeObject<List<GameInfo>>(glistString);
                        Log.WriteLine("NetworkGameClient: Received list of games on server. Game count: " + glist.Count);
                        var gListHandler = GameListReceived;
                        if (gListHandler != null)
                            gListHandler(this, new GameListEventArgs(glist));
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("NetworkGameClient: Received list of games from server could not be read. " + e.Message);
                    }
                    break;
                case NetworkPacketType.ClientLogin:
                    try
                    {
                        if (data.Length > 0 && data[0] == 0)
                        {
                            IsLoggedIn = true;
                            Log.WriteLine("NetworkGameClient: Login to server successful!");
                        }
                        else
                        {
                            IsLoggedIn = false;
                            var error = "NetworkGameClient: Login to server failed! ";
                            if (data[0] == 1) error += "Invalid user name";
                            else if (data[0] == 2) error += "Invalid password";
                            else error += "Unknown error";
                        }
                        var handler = LoggedIn;
                        if (handler != null)
                            handler(this, new LoggedInEventArgs(IsLoggedIn == true));
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("NetworkGameClient: Received login confirmation could not be read." + e.Message);
                    }
                    break;
                case NetworkPacketType.CreateGame:
                    try
                    {
                        // Check if an UID was assigned
                        var ginfo = JsonConvert.DeserializeObject<GameInfo>(Encoding.ASCII.GetString(data));
                        if (ginfo != null)
                        {
                            if (ginfo.UID != 0)
                            {
                                var gCreatedHandler = GameCreated;
                                if (gCreatedHandler != null)
                                    gCreatedHandler(this, new GameCreatedEventArgs(ginfo));
                            }
                        }
                        else
                            Log.WriteLine("NetworkGameClient: Received CreateGame confirmation could not be read.");
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("NetworkGameClient: Received CreateGame confirmation could not be read." + e.Message);
                    }
                    break;
                case NetworkPacketType.JoinGame:
                    try
                    {
                        var jMsg = JsonConvert.DeserializeObject<JoinMessage>(Encoding.ASCII.GetString(data));
                        if (jMsg != null)
                        {
                            var handler = GameJoinRequested;
                            if (handler != null)
                                handler(this, new GameJoinRequestedEventArgs(jMsg));
                        }
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("NetworkGameClient: Received JoinGame confirmation could not be read." + e.Message);
                    }
                    break;
                case NetworkPacketType.ServerInfo:
                    try
                    {
                        // Server data is always unencrypted
                        var jMsg = JsonConvert.DeserializeObject<ServerInfo>(Encoding.ASCII.GetString(packet.Data));
                        if (jMsg != null)
                        {
                            var handler = ServerInfoReceived;
                            if (handler != null)
                                handler(this, new ServerInfoEventArgs(jMsg));
                            ServerRequiresLogin = jMsg.RequiresLogin;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("NetworkGameClient: Received ServerInfo could not be read." + e.Message);
                    }
                    break;
                default:
                    Log.WriteLine("NetworkGameClient: Packet type " + packet.PacketType + " not recognized!");
                    break;
            }
        }

        /// <summary>
        /// Sends message to server.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="packetType">Packet type.</param>
        /// <returns></returns>
        bool Send(string message, byte packetType)
        {
            return Send(Encoding.ASCII.GetBytes(message), packetType);
        }

        /// <summary>
        /// Sends message to server.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="packetType">Packet type.</param>
        /// <returns></returns>
        bool Send(byte[] message, byte packetType)
        {
            return Send(message, packetType, _connection, _encrypter);
        }

        /// <summary>
        /// Close connection to server.
        /// </summary>
        public void Disconnect()
        {
            if (_connection == null) return;
            IsLoggedIn = false;
            ServerRequiresLogin = null;
            _encrypter = null;
            try
            {
                Log.WriteLine("NetworkGameClient: Closing connection...");
                _connection.Close();
                _connection.Dispose();
                Log.WriteLine("NetworkGameClient: Connection closed");
            }
            catch (Exception e)
            {
                Log.WriteLine("NetworkGameClient: Closing connection caused an exception: " + e.Message);
            }
            IsConnected = false;
            _connection = null;
        }
    }
}
