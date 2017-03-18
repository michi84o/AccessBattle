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

    public class GameClient : NetworkBase
    {
        public event EventHandler<GameListEventArgs> GameListReceived;
        public event EventHandler<GameCreatedEventArgs> GameCreated;

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
                IsLoggedIn = result;
            }
            return result;
        }

        public async Task<bool> RequestGameList()
        {
            bool result;
            using (var t = Task.Run(() =>
            {
                return Send(new byte[0], NetworkPacketType.ListGames);
            }))
            {
                result = await t;
            }
            return result;             
        }

        public async Task<bool> CreateGame(string gameName, string playerName)
        {
            bool result;
            using (var t = Task.Run(() =>
            {
                var info = new GameInfo
                {
                    UID = 0,
                    Name = gameName,
                    Player1 = playerName
                };
                return Send(JsonConvert.SerializeObject(info), NetworkPacketType.CreateGame);
            }))
            {
                result = await t;
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
                default:
                    Log.WriteLine("");
                    break;
            }
        }

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
