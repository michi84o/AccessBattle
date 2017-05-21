using AccessBattle.Networking.Packets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/* Protocol:
 * 1. Client connects
 * 2. Server sends public key
 * 3. Client sends public key and player info
 * ----------------------------------------
 * Packet Format: [STX|LEN|TYPE|DATA|CHSUM|ETX]
 * ----------------------------------------
 * STX = Start of packet 0x02
 * LEN = Length of data (2 byte) (1st byte = upper bytes, 2nd byte = lower bytes
 * TYPE = Packet type (1 byte)
 *      - 0x01: RSA public key [XML (string)]
 *      - 0x02: Login Client[Login (JSON)] Server[0/1 (byte, 0=OK, 1=invalid name, 2=invalid password)]
 *      - 0x03: List available games Client[] Server[List<GameInfo> (JSON string)]
 *      - 0x04: Create Game Client[GameInfo (UID=0,JSON)] Server[GameInfo (UID != 0 => OK)]
 *      - 0x05: Join Game 
 *              This is complicated:
 *              -> Player 2 sends requests to join game A
 *              -> Player 1 accepts
 *              -> Server acknowledges accept and sends confirmation to Player 2
 *              -> Player 2 confirms again
 *              -> Game state change (Init) is forwarded to both players
 *              Packet format:
 *              [Game UID (string); Type (string); Player2Name (string, optional))]
 *              Types:
 *              "0" = Request to join (Server to Client P1)
 *              "1" = Error, join not possible (Server)
 *              "2" = Request to accept join (Server, P2 name appended)
 *              "3" = Accept join (Client P1 to Server, Server to P2)
 *              "4" = Decline join (Client P1 to Server, Server to P2)
 *      - 0x06: Game Init [opponent name, client player number]
 *      - 0x07: Game Sync [??? TODO]
 *      - 0x08: Game Command
 * DATA = Packet data (encrypted, except public key)
 * CHSUM = Checksum (all bytes XOR'ed. except STX,CHSUM,ETX)
 * ETX = End of packet   0x03
 *  
 *  TODO: Cleanup mechanism for games and logins
 *  TODO: Lock Access to Game list
 *  
 */
namespace AccessBattle.Networking
{
    // TODO: Create pool for random matches (Flag in CreateGame?)
    /// <summary>
    /// Class for creating a game server.
    /// </summary>
    public class GameServer : NetworkBase
    {
        /// <summary>List of current games.</summary>
        public Dictionary<uint, NetworkGame> Games { get { return _games; } }
        
        /// <summary>List of currently connected players.</summary>
        public Dictionary<uint, NetworkPlayer> Players { get { return _players; } }
        
        /// <summary>Network port to use for accepting connections.</summary>
        public ushort Port { get { return _port; } }
     
        /// <summary>If true, any client is accepted. Else the database of registered users is used (TODO: NOT IMPLEMENTED).</summary>
        public bool AcceptAnyClient { get; set; }

        Thread _serverThread;
        ushort _port;
        Dictionary<uint, NetworkGame> _games = new Dictionary<uint, NetworkGame>();
        Dictionary<uint, NetworkPlayer> _players = new Dictionary<uint, NetworkPlayer>();
        TcpListener _server;
        CancellationTokenSource _serverCts;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="port">
        /// Network port to use for accepting connections.
        /// It is vital for our plans to use port 3221, or otherwise the organization will notice us!</param>
        public GameServer(ushort port = 3221)
        {
            if (port < 1024)
            {
                // 3221 is the sum of the bytes in the ASCII string "Rai-Net Access Battlers Steins;Gate"
                // Also the sum of the digits is 8, which is the final number of lab members of the first VN.
                port = 3221; // OSH MK UF A 2010
            }
            _port = port;
            if (port != 3221) Trace.WriteLine("The Organization has made its move! El Psy Congroo");
        }

        /// <summary>
        /// Starts the server. Implicitly calls Stop().
        /// </summary>
        public void Start()
        {
            Stop();
            _server = new TcpListener(IPAddress.Any, _port);
            _serverCts = new CancellationTokenSource();
            _server.Start();            
            _serverThread = new Thread(() => ListenForClients(_serverCts.Token)) { IsBackground = true };
            _serverThread.Start();
        }

        /// <summary>
        /// Stops the server and clears all players and games. Can block up to 10 seconds.
        /// </summary>
        public void Stop()
        {
            if (_server == null) return;
            _serverCts.Cancel();
            _server.Stop();
            try { _serverThread.Join(10000); }
            catch (Exception e) { Log.WriteLine("Server thread join error: " + e); }
            try { _serverThread.Abort(); }
            catch (Exception e) { Log.WriteLine("Server thread abort failed " + e); }
            _server = null;
            
            // No try catch here. If this goes wrong we should restart the server.
            foreach (var item in _players)
            {
                item.Value.Dispose();
            }
            _players.Clear();
            _games.Clear();
        }

        /// <summary>
        /// Gets a new ID that can be used for players or games.
        /// </summary>
        /// <returns>The generated ID.</returns>
        /// <remarks>ID might not be unique. You should check to be sure.</remarks>
        uint GetUid()
        {
            var guid = Guid.NewGuid().ToByteArray(); // 16 byte
            var uid = guid[0] | guid[0] << 8 | guid[0] << 16 | guid[0] << 24;
            if (uid == 0) return GetUid();
            return (uint)uid;
        }

        /// <summary>
        /// Main server loop. Accepts new connections.
        /// </summary>
        /// <param name="token">Token for cancelling the loop.</param>
        void ListenForClients(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var socketTask = _server.AcceptSocketAsync();

                        // Wait for accept or cancellation
                        socketTask.Wait(token);
                        if (token.IsCancellationRequested)
                            break;

                        var socket = socketTask.Result;
                        if (socket != null)
                        {
                            var serverCrypto = new CryptoHelper();
                            // Send public key. There is one key-pair for every client!
                            Log.WriteLine("GameServer: Sending public key...");
                            if (!Send(serverCrypto.GetPublicKey(), NetworkPacketType.PublicKey, socket))
                            {
                                Log.WriteLine("GameServer: Sending public key failed! Disconnecting...");
                                socket.Dispose();
                            }
                            else
                            {
                                // We got connected. The new client has not been authenticated yet.
                                // Give him a uid. GUID has 128 bit, but 32 bits seems enough:
                                uint uid;
                                // Make sure we do not accidentally generate the same uid:
                                lock(Players)
                                { 
                                    while (Players.ContainsKey((uid = GetUid()))) { }
                                    var player = new NetworkPlayer(socket, uid, serverCrypto);
                                    Players.Add(uid, player);
                                }
                                ReceiveAsync(socket, uid);
                            }
                        }
                    }
                    catch (AggregateException)
                    {
                        continue;
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("GameServer: Unknown exception while waiting for clients: " + e);
                        continue;
                    }
                }
                Log.WriteLine("GameServer: Wait for clients was cancelled.");
            }
            catch (ThreadAbortException)
            {
                Log.WriteLine("GameServer: ListenForClients aborted!");
            }            
        }

        /// <summary>
        /// Called when data was received from one of the clients.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        protected override void Receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            try // Only for dispose. No exceptions catched here!
            {                
                if (_serverCts.IsCancellationRequested) return;

                NetworkPlayer player;

                if (e.SocketError != SocketError.Success)
                {
                    // Will be hit after client disconnect
                    Log.WriteLine("GameServer: Receive for client (UID:" + e.UserToken + ") not successful: " + e.SocketError);
                    // Remove client...
                    lock(Players)
                    {
                        if (Players.ContainsKey((uint)e.UserToken))
                        {
                            Players.Remove((uint)e.UserToken);
                        }
                    }
                }
                else if (e.BytesTransferred > 0)
                {
                    Log.WriteLine("GameServer: Received data from client, UID: " + e.UserToken);
                    if (!Players.TryGetValue((uint)e.UserToken, out player))
                    {
                        Log.WriteLine("GameServer: Client with UID " + e.UserToken + " does not exist");
                        return;
                    }
                    // Process the packet ======================================
                    Log.WriteLine("GameServer: Received " + e.BytesTransferred + " bytes of data");
                    player.ReceiveBuffer.Add(e.Buffer, 0, e.BytesTransferred);
                    Log.WriteLine("GameServer: Receive buffer of player " + e.UserToken + " has now " + player.ReceiveBuffer.Length + " bytes of data");
                    byte[] packData;
                    while (player.ReceiveBuffer.Take(NetworkPacket.STX, NetworkPacket.ETX, out packData))
                    {
                        Log.WriteLine("GameServer: Received full packet");
                        Log.WriteLine("GameServer: Receive buffer of player " + e.UserToken + " has now " + player.ReceiveBuffer.Length + " bytes of data");
                        var packet = NetworkPacket.FromByteArray(packData);
                        if (packet != null)
                        {
                            Log.WriteLine("GameServer: Received packet of type " + packet.PacketType + " with " + packet.Data.Length + " bytes of data");
                            ProcessPacket(packet, player);
                        }
                        else
                        {
                            Log.WriteLine("GameServer: Could not parse packet!");
                        }
                    }
                    // =========================================================
                    ReceiveAsync(player.Connection, player.UID);
                }
            }
            // No catch for now. Exceptions will crash the server.
            finally
            {
                e.Dispose();
            }
        }

        /// <summary>
        /// Get game and both players.
        /// </summary>
        /// <param name="gameId">Game ID.</param>
        /// <param name="game">Game instance.</param>
        /// <param name="player1">Player 1.</param>
        /// <param name="player2">Player 2.</param>
        /// <returns></returns>
        bool GetGameAndPlayers(uint gameId, out NetworkGame game, out NetworkPlayer player1, out NetworkPlayer player2)
        {
            player1 = null;
            player2 = null;
            try
            {
                if (_games.TryGetValue(gameId, out game))
                {
                    player1 = game.Players[0].Player as NetworkPlayer;
                    player2 = game.Players[1].Player as NetworkPlayer;
                    return true;
                }                
            }
            catch (Exception)
            {
                game = null;
                player1 = null;
                player2 = null;
            }
            return false;
        }

        /// <summary>
        /// Processes the received packets.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="player"></param>
        void ProcessPacket(NetworkPacket packet, NetworkPlayer player)
        {
            // Decrypt data
            var data = packet.Data;
            if (data != null && data.Length > 0)
            {
                data = player.ServerCrypto.Decrypt(packet.Data);
                if (data == null)
                {
                    Log.WriteLine("GameServer: Error! Could not decrypt message of client " + player.UID);
                    // TODO: Notify client or close connection?
                    return;
                }
            }

            if (packet.PacketType > NetworkPacketType.ClientLogin && !player.IsLoggedIn)
            {
                Log.WriteLine("GameServer: Player " + player.UID + " is not logged it but tried to send packet of type " + packet.PacketType + ". Packet is ignored!");
                return;
            }

            switch (packet.PacketType)
            {
                case NetworkPacketType.PublicKey:
                    try
                    {
                        Log.WriteLine("GameServer: Received public key of player " + player.UID);
                        player.ClientCrypto = new CryptoHelper(Encoding.ASCII.GetString(data));
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("GameServer: Received public key of player " + player.UID + " is invalid! " + ex.Message);
                    }
                    break;
                case NetworkPacketType.ListGames:
                    Log.WriteLine("GameServer: Player " + player.UID + " requesting game list");
                    var glist = Games.Select(kv => kv.Value).Where(v => v.Phase == GamePhase.WaitingForPlayers && v.Players[1].Player == null && v.UID != 0).ToList();
                    var info = new List<GameInfo>();
                    foreach (var game in glist)
                    {
                        info.Add(new GameInfo { UID = game.UID, Name = game.Name, Player1 = game.Players[0].Name });
                    }
                    var infoJson =
                    Send(JsonConvert.SerializeObject(info), NetworkPacketType.ListGames, player.Connection, player.ClientCrypto);
                    break;
                case NetworkPacketType.ClientLogin:
                    try
                    {
                        var login = JsonConvert.DeserializeObject<Login>(Encoding.ASCII.GetString(data));
                        if (login != null && login.Name.Length > 0)
                        {
                            player.Name = login.Name;
                            player.IsLoggedIn = true;
                            Log.WriteLine("GameServer: Player " + player.UID + " logged in successfully!");
                            // TODO Check AcceptAnyClient variable and compare with whitelist
                            Send(new byte[] { 0 }, NetworkPacketType.ClientLogin, player.Connection, player.ClientCrypto);
                        }
                        else
                            Send(new byte[] { 1 }, NetworkPacketType.ClientLogin, player.Connection, player.ClientCrypto);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("GameServer: Received login of player " + player.UID + " could not be read!" + ex.Message);
                    }
                    break;
                case NetworkPacketType.CreateGame:
                    try
                    {
                        var ginfo = JsonConvert.DeserializeObject<GameInfo>(Encoding.ASCII.GetString(data));
                        if (ginfo != null)
                        {
                            Log.WriteLine("GameServer: Received CreateGame packet from player " + player.UID + ": [" + ginfo.Name + "/" + ginfo.UID + "/" + ginfo.Player1 + "]");
                            uint uid;
                            lock (Games)
                            {
                                while (Games.ContainsKey((uid = GetUid()))) { }
                                var game = new NetworkGame(uid);
                                game.Players[0].Player = player;
                                game.Players[0].Name = ginfo.Player1;
                                game.Name = ginfo.Name;
                                Games.Add(uid, game);
                                player.CurrentGame = game;
                            }
                            ginfo.UID = uid;
                            Send(JsonConvert.SerializeObject(ginfo), NetworkPacketType.CreateGame, player.Connection, player.ClientCrypto);
                        }
                        else
                        {
                            Log.WriteLine("GameServer: Received CreateGame packet of player " + player.UID + " could not be read!");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("GameServer: Received CreateGame packet of player " + player.UID + " could not be read! " + ex.Message);
                    }
                    break;
                case NetworkPacketType.JoinGame:
                    try
                    {
                        var jMsg = JsonConvert.DeserializeObject<JoinMessage>(Encoding.ASCII.GetString(data));
                        NetworkGame game;
                        NetworkPlayer p1, p2;
                        if (GetGameAndPlayers(jMsg.UID, out game, out p1, out p2))                        
                        {
                            // Check type of messagae
                            if (jMsg.Request == 0) // Request to join game
                            {
                                var reqOK = false;
                                if (game.BeginJoinPlayer(player))
                                {                                    
                                    if (p1 != null && player != p1) // Cannot request to join own game!
                                    {                                        
                                        var p1Req = new JoinMessage
                                        { UID = game.UID, Request = 2, JoiningUser = player.Name };
                                        Send(JsonConvert.SerializeObject(p1Req), NetworkPacketType.JoinGame, p1.Connection, p1.ClientCrypto);
                                        reqOK = true;                                        
                                    }
                                }
                                if (!reqOK) // Error while joining
                                {
                                    var p2Err = new JoinMessage { UID = game.UID, Request = 1 };
                                    Send(JsonConvert.SerializeObject(p2Err), NetworkPacketType.JoinGame, player.Connection, player.ClientCrypto);
                                }
                            }
                            else if (jMsg.Request == 3) // Join Accept
                            {
                                if (player == p1)
                                {
                                    // Notify p2
                                    var p2Answ = new JoinMessage { UID = game.UID, Request = 3 };
                                    Send(JsonConvert.SerializeObject(p2Answ), NetworkPacketType.JoinGame, p2.Connection, p2.ClientCrypto);
                                }
                                else if (player == p2)
                                {
                                    if (game.JoinPlayer(p2, true))
                                    {
                                        p2.CurrentGame = game;
                                    }
                                }
                            }
                            else if (jMsg.Request == 4) // Join Declined
                            {
                                if (player == p1)
                                {
                                    // Notify p2
                                    var p2Answ = new JoinMessage { UID = game.UID, Request = 4 };
                                    Send(JsonConvert.SerializeObject(p2Answ), NetworkPacketType.JoinGame, player.Connection, player.ClientCrypto);
                                }
                                else if (player == p2)
                                {
                                    game.JoinPlayer(p2, false);                                    
                                }
                            }
                        }                        
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("GameServer: Received JoinGame packet of player " + player.UID + " could not be read! " + ex.Message);
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
