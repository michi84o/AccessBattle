using AccessBattle.Networking.Packets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
    /// Args for game synchronization.
    /// </summary>
    public class GameSyncEventArgs : EventArgs
    {
        /// <summary>
        /// Game sync packet.
        /// </summary>
        public GameSync Sync { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sync"></param>
        public GameSyncEventArgs(GameSync sync)
        {
            Sync = sync;
        }
    }

    public class GameCommandEventArgs : EventArgs
    {
        public uint UID { get; private set; }
        public string Command { get; private set; }

        public GameCommandEventArgs(GameCommand cmd)
        {
            UID = cmd.UID;
            Command = cmd.Command;
        }
    }

    // TODO: UID is now a property. The uid parameters are not required anymore
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
        /// <summary>Received game synchronization packet.</summary>
        public event EventHandler<GameSyncEventArgs> GameSyncReceived;
        /// <summary>Received game exit. The game on the server was closed.</summary>
        public event EventHandler GameExitReceived;

        /// <summary>
        /// The method ConfirmJoin was called. Used for communication between view models.
        /// The two parameters of ConfirmJoin are encoded in the event args.
        /// The used uid is stored in UID. The accept parameter is stored as 'Accept' (true) or 'Decline' (false).
        /// TODO: This is strange design. There might be a more elegant way.
        /// </summary>
        public event EventHandler<GameJoinRequestedEventArgs> ConfirmJoinCalled;
        /// <summary>
        /// Response to a game command received from server.
        /// </summary>
        public event EventHandler<GameCommandEventArgs> GameCommandReceived;

        uint _uid;
        /// <summary>
        /// Unique ID of the current game. Used for network games.
        /// </summary>
        public uint UID
        {
            get { return _uid; }
            set { SetProp(ref _uid, value); }
        }

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
                if (SetProp(ref _isConnected, value))
                {
                    if (_isConnected == false)
                        IsLoggedIn = false;
                }
            }
        }

        bool? _isJoined = false;
        /// <summary>Has joined a game if true.</summary>
        public bool? IsJoined
        {
            get { return _isJoined; }
            private set
            {
                if (SetProp(ref _isJoined, value))
                {
                    if (_isJoined == false)
                        UID = 0;
                }
            }
        }

        bool? _isLoggedIn = false;
        /// <summary>Login status. Is null during log in.</summary>
        public bool? IsLoggedIn
        {
            get { return _isLoggedIn; }
            private set
            {
                if (SetProp(ref _isLoggedIn, value))
                {
                    if (_isLoggedIn == false)
                        IsJoined = false;
                }
            }
        }

        string _loginName;
        /// <summary>Login name</summary>
        public string LoginName
        {
            get { return _loginName; }
            private set { SetProp(ref _loginName, value); }
        }

        ByteBuffer _receiveBuffer = new ByteBuffer(4096);

        CryptoHelper _encrypter;
        CancellationTokenSource _encrypterWaiter;

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
                Log.WriteLine(LogPriority.Information, "NetworkGameClient: Connecting...");
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
                    Log.WriteLine(LogPriority.Error, "NetworkGameClient: Connect failed: No key received from server!");
                    IsConnected = false;
                    return false;
                }
                // Send own public key
                Send(_decrypter.GetPublicKey(), NetworkPacketType.PublicKey, _connection, _encrypter);
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "NetworkGameClient: Connect failed: " + e.Message);
                IsConnected = false;
                return false;
            }
            Log.WriteLine(LogPriority.Information, "NetworkGameClient: Connect success!");
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
                Log.WriteLine(LogPriority.Warning, "Game Client: Cannot login with empty user name!");
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
                    if (Send(JsonConvert.SerializeObject(login, _serializerSettings), NetworkPacketType.ClientLogin))
                    {
                        result = await source.Task;
                    }
                }
                catch (Exception e) { Log.WriteLine(LogPriority.Error, "NetworkGameClient::Login(): " + e.Message); }
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
                catch (Exception e) { Log.WriteLine(LogPriority.Error, "NetworkGameClient::RequestGameList(): " + e.Message); }
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
                Log.WriteLine(LogPriority.Warning, "Game Client: Cannot create game when not logged in!");
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
                    if (Send(JsonConvert.SerializeObject(info, _serializerSettings), NetworkPacketType.CreateGame))
                    {
                        result = await source.Task;
                    }
                }
                catch (Exception e) { Log.WriteLine(LogPriority.Error, "NetworkGameClient::CreateGame(): " + e.Message); }
                finally { GameCreated -= handler; }
            }

            return result?.GameInfo;
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
                return Send(JsonConvert.SerializeObject(req, _serializerSettings), NetworkPacketType.JoinGame);
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "NetworkGameClient::RequestJoinGame(): " + e.Message);
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
                var req = new JoinMessage { UID = uid, Request = accept ? JoinRequestType.Accept : JoinRequestType.Decline };
                IsJoined = Send(JsonConvert.SerializeObject(req, _serializerSettings), NetworkPacketType.JoinGame) && accept;
                ConfirmJoinCalled?.Invoke(this, new GameJoinRequestedEventArgs(new JoinMessage() { UID = uid, Request = accept ? JoinRequestType.Accept : JoinRequestType.Decline }));
                return IsJoined == accept;
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "NetworkGameClient::AnswerJoinRequest(): " + e.Message);
                IsJoined = false;
                return false;
            }
        }

        /// <summary>
        /// Sends game command to server.
        /// </summary>
        /// <param name="uid">UID of game.</param>
        /// <param name="command">The game command.</param>
        /// <returns></returns>
        public async Task<bool> SendGameCommand(uint uid, string command)
        {
            var cmd = new GameCommand { UID = uid, Command = command };

            // Catch the event for receiving the server response.
            GameCommandEventArgs result = null;
            var source = new TaskCompletionSource<GameCommandEventArgs>();
            EventHandler<GameCommandEventArgs> handler = (sender, args) =>
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
                    GameCommandReceived += handler;
                    if (Send(JsonConvert.SerializeObject(cmd, _serializerSettings), NetworkPacketType.GameCommand))
                    {
                        result = await source.Task;
                    }
                }
                catch (Exception e) { Log.WriteLine(LogPriority.Error, "NetworkGameClient::SendGameCommand(): " + e.Message); }
                finally { GameCommandReceived -= handler; }
            }
            return result?.Command == "OK";
        }

        /// <summary>
        /// Exit a game. If game was active and had not finished, player will loose the game.
        /// </summary>
        /// <param name="uid"></param>
        /// <returns>True if request was sent.</returns>
        public async Task<bool> ExitGame(uint uid)
        {
            var result = false;

            // Catch the event for game exit
            var source = new TaskCompletionSource<bool>();
            EventHandler handler = (sender, args) =>
            {
                source.TrySetResult(true);
            };

            // In case no answer was received, we have to cancel
            using (var ct = new CancellationTokenSource(NetworkTimeout * 1000))
            {
                ct.Token.Register(() => source.TrySetResult(false));
                try
                {
                    GameExitReceived += handler;
                    var pak = new ExitGame { UID = uid };
                    if (Send(JsonConvert.SerializeObject(pak, _serializerSettings), NetworkPacketType.ExitGame))
                    {
                        result = await source.Task;
                    }
                }
                catch (Exception e) { Log.WriteLine(LogPriority.Error, "NetworkGameClient::ExitGame(): " + e.Message); }
                finally { GameExitReceived -= handler; }
            }
            if (!result) // Screw it!
            {
                UID = 0;
                IsJoined = false;
            }
            return result;
        }

        /// <summary>
        /// Request the server to rematch
        /// </summary>
        /// <returns></returns>
        public bool Rematch()
        {
            if (IsJoined != true) return false;
            var pak = new Rematch { UID = UID };
            try
            {
                return Send(JsonConvert.SerializeObject(pak, _serializerSettings), NetworkPacketType.Rematch);
            }
            catch (Exception e) { Log.WriteLine(LogPriority.Error, "NetworkGameClient::Rematch(): " + e.Message); }
            return false;
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
                    Log.WriteLine(LogPriority.Error, "NetworkGameClient: Receive not successful: " + e.SocketError);
                }
                else if (e.BytesTransferred > 0)
                {
                    Log.WriteLine(LogPriority.Debug, "NetworkGameClient: Received " + e.BytesTransferred + " bytes of data");
                    _receiveBuffer.Add(e.Buffer, 0, e.BytesTransferred);
                    Log.WriteLine(LogPriority.Debug, "NetworkGameClient: Receive buffer has now " + _receiveBuffer.Length + " bytes of data");

                    byte[] packData;
                    while (_receiveBuffer.Take(NetworkPacket.STX, NetworkPacket.ETX, out packData))
                    {
                        Log.WriteLine(LogPriority.Debug, "NetworkGameClient: Received full packet");
                        var packet = NetworkPacket.FromByteArray(packData);
                        if (packet == null)
                            Log.WriteLine(LogPriority.Error, "NetworkGameClient: Could not parse packet!");
                        else
                        {
                            Log.WriteLine(LogPriority.Debug, "NetworkGameClient: Received packet of type " + packet.PacketType + " with " + packet.Data.Length + " bytes of data");
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
                if (packet.PacketType != NetworkPacketType.ServerInfo)
                    data = _decrypter.Decrypt(packet.Data);
                if (data == null)
                {
                    Log.WriteLine(LogPriority.Error, "NetworkGameClient: Error! Could not decrypt message from server");
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
                        Log.WriteLine(LogPriority.Error, "NetworkGameClient: Received key is invalid! " + ex.Message);
                    }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
#pragma warning disable CC0004 // Catch block cannot be empty
                    finally { try { _encrypterWaiter?.Cancel(); } catch { /* Ignore*/ } }
#pragma warning restore CC0004 // Catch block cannot be empty
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                    break;
                case NetworkPacketType.ListGames:
                    try
                    {
                        var glistString = Encoding.ASCII.GetString(data);
                        var glist = JsonConvert.DeserializeObject<List<GameInfo>>(glistString);
                        Log.WriteLine(LogPriority.Verbose, "NetworkGameClient: Received list of games on server. Game count: " + glist.Count);
                        GameListReceived?.Invoke(this, new GameListEventArgs(glist));
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine(LogPriority.Error, "NetworkGameClient: Received list of games from server could not be read. " + e.Message);
                    }
                    break;
                case NetworkPacketType.ClientLogin:
                    try
                    {

                        if (data.Length != 1 || data[0] > 4)
                        {
                            Log.WriteLine(LogPriority.Information, "NetworkGameClient: Received login confirmation could not be read: No data!");
                            break;
                        }
                        LoginCheckResult loginResult = (LoginCheckResult)(int)data[0];
                        if (loginResult == LoginCheckResult.LoginOK)
                        {
                            IsLoggedIn = true;
                            Log.WriteLine(LogPriority.Information, "NetworkGameClient: Login to server successful!");
                        }
                        else
                        {
                            IsLoggedIn = false;
                            var error = "NetworkGameClient: Login to server failed! ";
                            switch (loginResult)
                            {
                                case LoginCheckResult.InvalidUser:
                                    error += "Invalid user name";
                                    break;
                                case LoginCheckResult.InvalidPassword:
                                    error += "Invalid password";
                                    break;
                                case LoginCheckResult.DatabaseError:
                                    error += "Database error";
                                    break;
                                default:
                                    error += "Unknown error";
                                    break;
                            }
                            Log.WriteLine(LogPriority.Information, error);
                        }
                        LoggedIn?.Invoke(this, new LoggedInEventArgs(IsLoggedIn == true));
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine(LogPriority.Error, "NetworkGameClient: Received login confirmation could not be read." + e.Message);
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
                                GameCreated?.Invoke(this, new GameCreatedEventArgs(ginfo));
                            }
                        }
                        else
                            Log.WriteLine(LogPriority.Error, "NetworkGameClient: Received CreateGame confirmation could not be read.");
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine(LogPriority.Error, "NetworkGameClient: Received CreateGame confirmation could not be read." + e.Message);
                    }
                    break;
                case NetworkPacketType.JoinGame:
                    try
                    {
                        var jMsg = JsonConvert.DeserializeObject<JoinMessage>(Encoding.ASCII.GetString(data));
                        if (jMsg != null)
                        {
                            GameJoinRequested?.Invoke(this, new GameJoinRequestedEventArgs(jMsg));

                            // If joining and other side declined then set IsJoined to false
                            if (jMsg.Request == JoinRequestType.Decline)
                                IsJoined = false;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine(LogPriority.Error, "NetworkGameClient: Received JoinGame confirmation could not be read." + e.Message);
                    }
                    break;
                case NetworkPacketType.ServerInfo:
                    try
                    {
                        // Server data is always unencrypted
                        var jMsg = JsonConvert.DeserializeObject<ServerInfo>(Encoding.ASCII.GetString(packet.Data));
                        if (jMsg != null)
                        {
                            ServerRequiresLogin = jMsg.RequiresLogin;
                            ServerInfoReceived?.Invoke(this, new ServerInfoEventArgs(jMsg));
                        }
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine(LogPriority.Error, "NetworkGameClient: Received ServerInfo could not be read." + e.Message);
                    }
                    break;
                case NetworkPacketType.ExitGame:
                    try
                    {
                        var eMsg = JsonConvert.DeserializeObject<ExitGame>(Encoding.ASCII.GetString(data));
                        if (eMsg != null)
                        {
                            if (eMsg.UID == UID)
                            {
                                IsJoined = false;
                                UID = 0;
                                GameExitReceived?.Invoke(this, EventArgs.Empty);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine(LogPriority.Error, "NetworkGameClient: Received ExitGame message could not be read." + e.Message);
                    }
                    break;
                case NetworkPacketType.GameSync:
                    try
                    {
                        var eMsg = JsonConvert.DeserializeObject<GameSync>(Encoding.ASCII.GetString(data));
                        if (eMsg != null && eMsg.UID == UID)
                        {
                            GameSyncReceived?.Invoke(this, new GameSyncEventArgs(eMsg));
                        }
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine(LogPriority.Error, "NetworkGameClient: Received GameSync message could not be read." + e.Message);
                    }
                    break;
                case NetworkPacketType.GameCommand:
                    try
                    {
                        var eMsg = JsonConvert.DeserializeObject<GameCommand>(Encoding.ASCII.GetString(data));
                        if (eMsg != null)
                        {
                            GameCommandReceived?.Invoke(this, new GameCommandEventArgs(eMsg));
                        }
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine(LogPriority.Error, "NetworkGameClient: Received GameSync message could not be read." + e.Message);
                    }
                    break;
                default:
                    Log.WriteLine(LogPriority.Error, "NetworkGameClient: Packet type " + packet.PacketType + " not recognized!");
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
            ServerRequiresLogin = null;
            _encrypter = null;
            try
            {
                Log.WriteLine(LogPriority.Information, "NetworkGameClient: Closing connection...");
                _connection.Close();
                _connection.Dispose();
                Log.WriteLine(LogPriority.Information, "NetworkGameClient: Connection closed");
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "NetworkGameClient: Closing connection caused an exception: " + e.Message);
            }
            // Implicitly sets IsLoggedIn and IsJoined to false and UID to 0
            IsConnected = false;
            _connection = null;
        }
    }
}
