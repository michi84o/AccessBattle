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
    public class GameListEventArgs : EventArgs
    {
        public List<GameInfo> GameList { get; private set; }
        public GameListEventArgs(List<GameInfo> gameList)
        {
            GameList = gameList;
        }
    }
    public class GameCreatedEventArgs : EventArgs
    {
        public GameInfo GameInfo { get; private set; }
        public GameCreatedEventArgs(GameInfo gameInfo)
        {
            GameInfo = gameInfo;
        }
    }
    public class GameJoinedEventArgs : EventArgs
    {
        public uint Uid { get; private set; }
        public bool Joined { get; private set; }
        public GameJoinedEventArgs(uint uid, bool joined)
        {
            Uid = uid;
            Joined = joined;
        }
    }

    public class GameClient : NetworkBase
    {
        const int NetworkTimeout = 15000;

        public event EventHandler<GameListEventArgs> GameListReceived;
        public event EventHandler<GameCreatedEventArgs> GameCreated;
        public event EventHandler<GameJoinedEventArgs> GameJoined;

        bool? _isConnected = false;
        public bool? IsConnected
        {
            get { return _isConnected; }
            private set { _isConnected = value; }
        }

        bool? _isLoggedIn = false;
        public bool? IsLoggedIn
        {
            get { return _isLoggedIn; }
            private set { _isLoggedIn = value; }
        }

        public string LoginName { get; private set; }

        Game _game = new Game();
        public Game Game { get { return _game; } }
        ByteBuffer _receiveBuffer = new ByteBuffer(4096);
        CryptoHelper _encrypter;
        CryptoHelper _decrypter;
        Socket _connection;
        readonly object _clientLock = new object();

        public GameClient()
        {
            _decrypter = new CryptoHelper();
        }

        public async Task<bool> Connect(string server, ushort port)
        {
            if (IsConnected == null) return false;
            lock (_clientLock)
            {
                if (IsConnected == null) return false;
                Disconnect();
                IsConnected = null;
                Log.WriteLine("GameClient: Connecting...");
            }
            bool result;
            using (var task = Task.Run(() =>
            {
                #region Task
                try
                {                    
                    _connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    _connection.Connect(server, port);
                    ReceiveAsync(_connection, 0);
                    
                    // Block until keys are exchanged
                    var then = DateTime.UtcNow.AddSeconds(30);
                    while (_encrypter == null && (then - DateTime.UtcNow).TotalSeconds > 0)
                    {
                        Thread.Sleep(100);
                    }
                    if (_encrypter == null)
                    {
                        Disconnect();
                        Log.WriteLine("GameClient: Connect failed: No key received from server!");
                        IsConnected = false;
                        return false;
                    }
                    // Send own public key
                    Send(_decrypter.GetPublicKey(), NetworkPacketType.PublicKey, _connection, _encrypter);
                }
                catch (Exception e)
                {
                    Log.WriteLine("GameClient: Connect failed: " + e.Message);
                    IsConnected = false;
                    return false;
                }
                Log.WriteLine("GameClient: Connect success!");
                return true;
                #endregion
            }))
            {
                result = await task;
            }
            IsConnected = result;
            return result;                
        }

        public async Task<bool> Login(string name, string password)
        {
            if (string.IsNullOrEmpty(name))
            {
                Log.WriteLine("Game Client: Cannot login with empty user name!");
                return false;
            }
            bool result;
            using (var t = Task.Run(() =>
            {
                #region Task
                IsLoggedIn = null;
                var login = new Login { Name = name, Password = password };
                if (!Send(JsonConvert.SerializeObject(login), NetworkPacketType.ClientLogin))
                    return false;
                var then = DateTime.UtcNow.AddSeconds(30);
                while (IsLoggedIn == null && (then - DateTime.UtcNow).TotalSeconds > 0)
                {
                    Thread.Sleep(100);
                }
                return IsLoggedIn == true;
                #endregion
            }))
            {
                result = await t;
                LoginName = name;
                IsLoggedIn = result;                
            }
            return result;
        }

        public async Task<List<GameInfo>> RequestGameList()
        {
            List<GameInfo> result = null;
            using (var t = Task.Run(() =>
            {
                using (var waitHandle = new AutoResetEvent(false))
                {
                    EventHandler<GameListEventArgs> handler = (sender, args) =>
                    {
                        result = args.GameList;
                        waitHandle.Set();
                    };

                    try
                    {
                        GameListReceived += handler;
                        if (Send(new byte[0], NetworkPacketType.ListGames))
                        {
                            waitHandle.WaitOne(NetworkTimeout);
                        }
                    }
                    catch (Exception e) { Log.WriteLine("GameClient::RequestGameList(): " + e.Message); }
                    finally { GameListReceived -= handler; }
                }
            }))
            {
                await t;
            }
            return result;
        }

        public async Task<GameInfo> CreateGame(string gameName)
        {
            if (IsLoggedIn != true || string.IsNullOrEmpty(LoginName))
            {
                Log.WriteLine("Game Client: Cannot create game when not logged in!");
                return null;
            }
            GameInfo result = null;
            using (var t = Task.Run(() =>
            {
                var info = new GameInfo
                {
                    UID = 0,
                    Name = gameName,
                    Player1 = LoginName
                };

                using (var waitHandle = new AutoResetEvent(false))
                {
                    EventHandler<GameCreatedEventArgs> handler = (sender, args) =>
                    {
                        if (args.GameInfo != null && args.GameInfo.Name == gameName && args.GameInfo.Player1 == LoginName)
                        {
                            result = args.GameInfo;
                            waitHandle.Set();
                        }
                    };
                    try
                    {
                        GameCreated += handler;
                        if (Send(JsonConvert.SerializeObject(info), NetworkPacketType.CreateGame))
                        {
                            waitHandle.WaitOne(NetworkTimeout);
                        }
                    }
                    catch (Exception e) { Log.WriteLine("GameClient::CreateGame(): " + e.Message); }
                    finally { GameCreated += handler; }
                }
            }))
            {
                await t;
            }
            return result;
        }

        public async Task<bool> JoinGame(uint uid)
        {
            var result = false;
            using (var t = Task.Run(() =>
            {
                using (var waitHandle = new AutoResetEvent(false))
                {
                    EventHandler<GameJoinedEventArgs> handler = (sender, args) =>
                    {
                        if (args.Uid == uid)
                        {
                            result = args.Joined;
                            waitHandle.Set();
                        }
                    };
                    try
                    {
                        GameJoined += handler;
                        if (Send(uid.ToString(), NetworkPacketType.JoinGame))
                        {
                            waitHandle.WaitOne(NetworkTimeout);
                        }
                    }
                    catch (Exception e) { Log.WriteLine("GameClient::JoinGame(): " + e.Message); }
                    finally { GameJoined -= handler; }
                }
            }))
            {
                await t;
            }
            return result;
        }

        protected override void Receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError != SocketError.Success)
                {
                    // Will be hit after client disconnect
                    Log.WriteLine("GameClient: Receive not successful: " + e.SocketError);
                }
                else if (e.BytesTransferred > 0)
                {
                    Log.WriteLine("GameClient: Received " + e.BytesTransferred + " bytes of data");
                    _receiveBuffer.Add(e.Buffer, 0, e.BytesTransferred);
                    Log.WriteLine("GameClient: Receive buffer has now " + _receiveBuffer.Length + " bytes of data");

                    byte[] packData;
                    while (_receiveBuffer.Take(NetworkPacket.STX, NetworkPacket.ETX, out packData))
                    {
                        Log.WriteLine("GameClient: Received full packet");
                        var packet = NetworkPacket.FromByteArray(packData);
                        if (packet == null)
                            Log.WriteLine("GameClient: Could not parse packet!");
                        else
                        {
                            Log.WriteLine("GameClient: Received packet of type " + packet.PacketType + " with " + packet.Data.Length + " bytes of data");
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

        void ProcessPacket(NetworkPacket packet)
        {
            // Decrypt data
            var data = packet.Data;
            if (data != null && data.Length > 0 && packet.PacketType != NetworkPacketType.PublicKey)
            {
                data = _decrypter.Decrypt(packet.Data);
                if (data == null)
                {
                    Log.WriteLine("GameClient: Error! Could not decrypt message from server");
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
                        Log.WriteLine("GameClient: Received key is invalid! " + ex.Message);
                    }
                    break;
                case NetworkPacketType.ListGames:
                    try
                    {
                        var glistString = Encoding.ASCII.GetString(data);
                        var glist = JsonConvert.DeserializeObject<List<GameInfo>>(glistString);
                        Log.WriteLine("GameClient: Received list of games on server. Game count: " + glist.Count);
                        var gListHandler = GameListReceived;
                        if (gListHandler != null)
                            gListHandler(this, new GameListEventArgs(glist));
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("GameClient: Received list of games from server could not be read. " + e.Message);
                    }
                    break;
                case NetworkPacketType.ClientLogin:
                    try
                    {
                        if (data.Length > 0 && data[0] == 0)
                        {
                            IsLoggedIn = true;
                            Log.WriteLine("GameClient: Login to server successful!");
                        }
                        else
                        {
                            IsLoggedIn = false;
                            var error = "GameClient: Login to server failed! ";
                            if (data[0] == 1) error += "Invalid user name";
                            else if (data[0] == 2) error += "Invalid password";
                            else error += "Unknown error";                            
                        }
                        
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("GameClient: Received login confirmation could not be read." + e.Message);
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
                            Log.WriteLine("GameClient: Received CreateGame confirmation could not be read.");
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("GameClient: Received CreateGame confirmation could not be read." + e.Message);
                    }
                    break;
                case NetworkPacketType.JoinGame:
                    try
                    {
                        var jData = Encoding.ASCII.GetString(data).Split(new[] { ';' });
                        uint iJoinResult = uint.Parse(jData[0]);
                        bool jResult = jData[1] == "0";
                        var handler = GameJoined;
                        if (handler != null)
                            handler(this, new GameJoinedEventArgs(iJoinResult, jResult));
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("GameClient: Received JoinGame confirmation could not be read." + e.Message);
                    }
                    break;
                default:
                    Log.WriteLine("");
                    break;
            }
        }

        //public bool CreateGame()
        //{
        //    return Send(new GameInfo {Name }
        //}

        bool Send(string message, byte packetType)
        {
            return Send(Encoding.ASCII.GetBytes(message), packetType);
        }

        bool Send(byte[] message, byte packetType)
        {
            return Send(message, packetType, _connection, _encrypter);
        }

        /// <summary>
        /// </summary>
        public void Disconnect()
        {            
            if (_connection == null) return;
            IsLoggedIn = false;
            _encrypter = null;
            try
            {
                Log.WriteLine("GameClient: Closing connection...");
                _connection.Close();
                _connection.Dispose();
                Log.WriteLine("GameClient: Connection closed");
            }
            catch (Exception e)
            {
                Log.WriteLine("GameClient: Closing connection caused an exception: " + e.Message);
            }
            _isConnected = false;
            _connection = null;
        }
    }
}
