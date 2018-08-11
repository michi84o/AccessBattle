using AccessBattle.Networking.Packets;
using AccessBattle.Plugins;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

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
 *  TODO: Behavior when user logs in with serveral client instances. Also reconnect behavior.
 *  TODO: Cleanup game list if a player creates a game and disconnects
 *  TODO: Allow reconnects after connection abort
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
        public Dictionary<uint, NetworkGame> Games => _games;

        /// <summary>List of currently connected players.</summary>
        public Dictionary<uint, NetworkPlayer> Players => _players;

        /// <summary>Network port to use for accepting connections.</summary>
        public ushort Port => _port;

        TimeSpan _gameTimeout = TimeSpan.FromHours(1); // Games will timeout after 1 hour

        bool _acceptAnyClient;
        /// <summary>
        /// If true, any client is accepted. Else the database of registered users is used.
        /// If the user database is null, this will always return true.
        /// </summary>
        public bool AcceptAnyClient
        {
            get => _acceptAnyClient || _userDatabase == null;
            set { _acceptAnyClient = value; }
        }

        Thread _serverThread;
        ushort _port;
        Dictionary<uint, NetworkGame> _games = new Dictionary<uint, NetworkGame>();
        Dictionary<uint, NetworkPlayer> _players = new Dictionary<uint, NetworkPlayer>();
        TcpListener _server;
        System.Timers.Timer _cleanupTimer = new System.Timers.Timer(300000) { AutoReset = false }; // Called every 5 minutes
        CancellationTokenSource _serverCts;
        IUserDatabaseProvider _userDatabase;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="port">
        /// Network port to use for accepting connections.
        /// It is vital for our plans to use port 3221, or otherwise the organization will notice us!</param>
        /// <param name="userDatabase"></param>
        public GameServer(ushort port = 3221, IUserDatabaseProvider userDatabase = null)
        {
            if (port < 1024)
            {
                // 3221 is the sum of the bytes in the ASCII string "Rai-Net Access Battlers Steins;Gate"
                // Also the sum of the digits is 8, which is the final number of lab members of the first VN.
                port = 3221; // OSH MK UF A 2010
            }
            _port = port;
            if (port != 3221) Log.WriteLine(LogPriority.Warning, "The Organization has made its move! El Psy Congroo");

            _userDatabase = userDatabase;

            _cleanupTimer.Elapsed += CleanupTimerEvent;
        }

        private void CleanupTimerEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            var gamesToKill = new Dictionary<uint, NetworkGame>();

            lock (Games)
            {
                foreach (var kv in Games)
                {
                    if (kv.Value.CheckForInactivityTimeout(_gameTimeout))
                        gamesToKill.Add(kv.Key, kv.Value);
                }
            }

            if (gamesToKill.Count == 0)
            {
                if (_server != null)
                    _cleanupTimer.Start();
                return;
            }

            foreach (var id in gamesToKill)
            {
                try
                {
                    NetworkPlayer p1, p2;
                    NetworkGame game;
                    if (GetGameAndPlayers(id.Key, out game, out p1, out p2))
                    {
                        game.ExitGame(null);

                        var syncP1 = GameSync.FromGame(game, game.UID, 1);
                        var syncP2 = GameSync.FromGame(game, game.UID, 2);
                        if (p1 != null)
                            Send(JsonConvert.SerializeObject(syncP1, _serializerSettings), NetworkPacketType.GameSync, p1.Connection, p1.ClientCrypto);
                        if (p2 != null)
                            Send(JsonConvert.SerializeObject(syncP2, _serializerSettings), NetworkPacketType.GameSync, p2.Connection, p2.ClientCrypto);

                        // This will disable the rematch button in the WPF UI and close the game:
                        var ans = new ExitGame { UID = game.UID, Reason = ExitGameReason.Inactivity };
                        if (p1 != null)
                            Send(JsonConvert.SerializeObject(ans, _serializerSettings), NetworkPacketType.ExitGame, p1.Connection, p1.ClientCrypto);
                        if (p2 != null)
                            Send(JsonConvert.SerializeObject(ans, _serializerSettings), NetworkPacketType.ExitGame, p2.Connection, p2.ClientCrypto);

                        Log.WriteLine(LogPriority.Verbose, "Removed game with id {0}", id.Key);
                        Games.Remove(id.Key);
                    }
                    else
                    {
                        // Something is not right. Just delete
                        lock (Games)
                        {
                            Log.WriteLine(LogPriority.Verbose, "Removed game with id {0}", id.Key);
                            Games.Remove(id.Key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine(LogPriority.Error, "Cleanup: Error while removing game with id {0}: ({1})", id.Key, ex.Message);
                    Log.WriteLine(LogPriority.Verbose, "Removed game with id {0}", id.Key);
                    Games.Remove(id.Key);
                }
            }
            if (_server != null)
                _cleanupTimer.Start();
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

            _cleanupTimer.Start();
        }

        /// <summary>
        /// Stops the server and clears all players and games. Can block up to 10 seconds.
        /// </summary>
        public void Stop()
        {
            _cleanupTimer.Stop();

            if (_server == null) return;
            _serverCts.Cancel();
            _server.Stop();
            try { _serverThread.Join(10000); }
            catch (Exception e) { Log.WriteLine(LogPriority.Error, "Server thread join error: " + e); }
            try { _serverThread.Abort(); }
            catch (Exception e) { Log.WriteLine(LogPriority.Error, "Server thread abort failed " + e); }
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
                            // Send server info
                            Send(JsonConvert.SerializeObject(
                                new ServerInfo(!AcceptAnyClient), _serializerSettings),
                                NetworkPacketType.ServerInfo, socket, null);

                            var serverCrypto = new CryptoHelper();
                            // Send public key. There is one key-pair for every client!
                            Log.WriteLine(LogPriority.Verbose, "GameServer: Sending public key...");
                            if (!Send(serverCrypto.GetPublicKey(), NetworkPacketType.PublicKey, socket))
                            {
                                Log.WriteLine(LogPriority.Error, "GameServer: Sending public key failed! Disconnecting...");
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
                        if (!(e is OperationCanceledException)) // Expected when server is closed
                            Log.WriteLine(LogPriority.Error, "GameServer: Unknown exception while waiting for clients: " + e);
                        continue;
                    }
                }
                Log.WriteLine(LogPriority.Warning, "GameServer: Wait for clients was cancelled.");
            }
            catch (ThreadAbortException)
            {
                Log.WriteLine(LogPriority.Warning, "GameServer: ListenForClients aborted!");
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
                    Log.WriteLine(LogPriority.Error, "GameServer: Receive for client (UID:" + e.UserToken + ") not successful: " + e.SocketError);
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
                    //Log.WriteLine("GameServer: Received data from client, UID: " + e.UserToken);
                    if (!Players.TryGetValue((uint)e.UserToken, out player))
                    {
                        Log.WriteLine(LogPriority.Warning, "GameServer: Client with UID " + e.UserToken + " does not exist");
                        return;
                    }
                    // Process the packet ======================================
                    //Log.WriteLine("GameServer: Received " + e.BytesTransferred + " bytes of data");
                    player.ReceiveBuffer.Add(e.Buffer, 0, e.BytesTransferred);
                    //Log.WriteLine("GameServer: Receive buffer of player " + e.UserToken + " has now " + player.ReceiveBuffer.Length + " bytes of data");
                    byte[] packData;
                    while (player.ReceiveBuffer.Take(NetworkPacket.STX, NetworkPacket.ETX, out packData))
                    {
                        //Log.WriteLine("GameServer: Received full packet");
                        //Log.WriteLine("GameServer: Receive buffer of player " + e.UserToken + " has now " + player.ReceiveBuffer.Length + " bytes of data");
                        var packet = NetworkPacket.FromByteArray(packData);
                        if (packet != null)
                        {
                            //Log.WriteLine("GameServer: Received packet of type " + packet.PacketType + " with " + packet.Data.Length + " bytes of data");
                            ProcessPacket(packet, player);
                        }
                        else
                        {
                            Log.WriteLine(LogPriority.Error, "GameServer: Could not parse packet!");
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

        void KickPlayerOut(NetworkPlayer player, uint uid)
        {
            try
            {
                var ans = new ExitGame { UID = uid };
                Send(JsonConvert.SerializeObject(ans, _serializerSettings), NetworkPacketType.ExitGame, player.Connection, player.ClientCrypto);
            }
            catch (Exception)  { return; }
        }

        /// <summary>
        /// Force one player to win the game. Can be used for debugging the server.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="key"></param>
        public void Win(int player, uint key)
        {
            NetworkGame game;
            NetworkPlayer p1, p2;
            if (!GetGameAndPlayers(key, out game, out p1, out p2)) return;
            game.Win(player);
            var syncP1 = GameSync.FromGame(game, game.UID, 1);
            var syncP2 = GameSync.FromGame(game, game.UID, 2);
            Send(JsonConvert.SerializeObject(syncP1, _serializerSettings), NetworkPacketType.GameSync, p1.Connection, p1.ClientCrypto);
            Send(JsonConvert.SerializeObject(syncP2, _serializerSettings), NetworkPacketType.GameSync, p2.Connection, p2.ClientCrypto);
        }

        /// <summary>
        /// Processes the received packets.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="player"></param>
        // TODO: Convert to async Task
        void ProcessPacket(NetworkPacket packet, NetworkPlayer player)
        {
            // Decrypt data
            var data = packet.Data;
            if (data != null && data.Length > 0)
            {
                data = player.ServerCrypto.Decrypt(packet.Data);
                if (data == null)
                {
                    Log.WriteLine(LogPriority.Error, "GameServer: Error! Could not decrypt message of client " + player.UID);
                    // TODO: Notify client or close connection?
                    return;
                }
            }

            if (packet.PacketType > NetworkPacketType.ClientLogin && !player.IsLoggedIn)
            {
                Log.WriteLine(LogPriority.Information, "GameServer: Player " + player.UID + " is not logged it but tried to send packet of type " + packet.PacketType + ". Packet is ignored!");
                return;
            }

            switch (packet.PacketType)
            {
                case NetworkPacketType.PublicKey:
                    try
                    {
                        Log.WriteLine(LogPriority.Debug, "GameServer: Received public key of player " + player.UID);
                        player.ClientCrypto = new CryptoHelper(Encoding.ASCII.GetString(data));
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(LogPriority.Warning, "GameServer: Received public key of player " + player.UID + " is invalid! " + ex.Message);
                    }
                    break;
                case NetworkPacketType.ListGames:
                    Log.WriteLine(LogPriority.Debug, "GameServer: Player " + player.UID + " requesting game list");
                    var glist = Games.Select(kv => kv.Value).Where(v => v.Phase == GamePhase.WaitingForPlayers && v.Players[1].Player == null && v.UID != 0).ToList();
                    var info = new List<GameInfo>();
                    foreach (var game in glist)
                    {
                        info.Add(new GameInfo { UID = game.UID, Name = game.Name, Player1 = game.Players[0].Name });
                    }
                    var infoJson =
                    Send(JsonConvert.SerializeObject(info, _serializerSettings), NetworkPacketType.ListGames, player.Connection, player.ClientCrypto);
                    break;
                case NetworkPacketType.ClientLogin:
                    try
                    {
                        var login = JsonConvert.DeserializeObject<Login>(Encoding.ASCII.GetString(data));
                        if (login?.Name?.Length > 0)
                        {
                            if (!AcceptAnyClient && _userDatabase != null)
                            {
                                var loginResult = _userDatabase.CheckLoginAsync(login.Name, login.Password?.ConvertToSecureString()).GetAwaiter().GetResult(); // TODO: Change to async call later
                                if (loginResult != 0)
                                {
                                    Send(new byte[] { (byte)(int)loginResult }, NetworkPacketType.ClientLogin, player.Connection, player.ClientCrypto);

                                    if (loginResult == LoginCheckResult.LoginOK)
                                    {
                                        Log.WriteLine(LogPriority.Verbose, "GameServer: Player " + (player.Name ?? "?") + " (" + player.UID + ") logged in successfully!");
                                        player.Name = login.Name;

                                        var elo = _userDatabase.GetELO(player.Name).GetAwaiter().GetResult(); // TODO: async
                                        if (elo != null)
                                            player.ELO = elo.Value;

                                        player.IsLoggedIn = true;
                                    }

                                    // Disabled this. Would be a dick move to disconnect when a user mistyped his login.
                                    //lock (Players)
                                    //{
                                    //    Players.Remove(player.UID);
                                    //    player.Connection?.Close();
                                    //    return;
                                    //}
                                }
                            }
                            else
                            {
                                Log.WriteLine(LogPriority.Verbose, "GameServer: Player " + (player.Name ?? "?") + " (" + player.UID + ") logged in successfully!");
                                player.Name = login.Name;
                                player.ELO = 0; // No ELO rating without user database
                                player.IsLoggedIn = true;
                                Send(new byte[] { (byte)(int)LoginCheckResult.LoginOK }, NetworkPacketType.ClientLogin, player.Connection, player.ClientCrypto);
                            }
                        }
                        else
                            Send(new byte[] { (byte)(int)LoginCheckResult.InvalidUser }, NetworkPacketType.ClientLogin, player.Connection, player.ClientCrypto);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(LogPriority.Error, "GameServer: Received login of player " + player.UID + " could not be read!" + ex.Message);
                    }
                    break;
                case NetworkPacketType.CreateGame:
                    try
                    {
                        var ginfo = JsonConvert.DeserializeObject<GameInfo>(Encoding.ASCII.GetString(data));
                        if (ginfo != null)
                        {
                            Log.WriteLine(LogPriority.Debug, "GameServer: Received CreateGame packet from player " + player.UID + ": [" + ginfo.Name + "/" + ginfo.UID + "/" + ginfo.Player1 + "]");
                            uint uid;
                            lock (Games)
                            {
                                while (Games.ContainsKey((uid = GetUid()))) { }
                                var game = new NetworkGame(uid);
                                game.Players[0].Player = player;
                                game.Players[0].Name = player.Name; // Ignore name from packet or people start faking names
                                game.Players[0].ELO = player.ELO;
                                game.Name = ginfo.Name.ToUpper();
                                Games.Add(uid, game);
                                Log.WriteLine(LogPriority.Information, "GameServer: Player " + player.UID + " created game " + game.UID + " ("+ ginfo.Name +")");
                                player.CurrentGame = game;
                            }
                            ginfo.UID = uid;
                            Send(JsonConvert.SerializeObject(ginfo, _serializerSettings), NetworkPacketType.CreateGame, player.Connection, player.ClientCrypto);
                        }
                        else
                        {
                            Log.WriteLine(LogPriority.Error, "GameServer: Received CreateGame packet of player " + player.UID + " could not be read!");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(LogPriority.Debug, "GameServer: Received CreateGame packet of player " + player.UID + " could not be read! " + ex.Message);
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
                            if (jMsg.Request == JoinRequestType.Join)
                            {
                                var reqOK = false;
                                if (game.BeginJoinPlayer(player))
                                {
                                    if (p1 != null && player != p1) // Cannot request to join own game!
                                    {
                                        var p1Req = new JoinMessage
                                        { UID = game.UID, Request = JoinRequestType.RequestAccept, JoiningUser = player.Name };
                                        Send(JsonConvert.SerializeObject(p1Req, _serializerSettings), NetworkPacketType.JoinGame, p1.Connection, p1.ClientCrypto);
                                        reqOK = true;
                                    }
                                }
                                if (!reqOK) // Error while joining
                                {
                                    var p2Err = new JoinMessage { UID = game.UID, Request = JoinRequestType.Error };
                                    Send(JsonConvert.SerializeObject(p2Err, _serializerSettings), NetworkPacketType.JoinGame, player.Connection, player.ClientCrypto);
                                }
                            }
                            else if (jMsg.Request == JoinRequestType.Accept)
                            {
                                if (player == p1)
                                {
                                    // Notify p2
                                    var p2Answ = new JoinMessage { UID = game.UID, Request = JoinRequestType.Accept };
                                    Send(JsonConvert.SerializeObject(p2Answ, _serializerSettings), NetworkPacketType.JoinGame, p2.Connection, p2.ClientCrypto);
                                    // At this point client of p1 expects game to start
                                    // p2 will send a final confirm. Wait for it.
                                }
                                else if (player == p2)
                                {
                                    if (game.JoinPlayer(p2, true))
                                    {
                                        p2.CurrentGame = game;
                                        // At this point client of p2 expects game to start
                                        game.InitGame();
                                        var syncP1 = GameSync.FromGame(game, game.UID, 1);
                                        var syncP2 = GameSync.FromGame(game, game.UID, 2);
                                        Send(JsonConvert.SerializeObject(syncP1, _serializerSettings), NetworkPacketType.GameSync, p1.Connection, p1.ClientCrypto);
                                        Send(JsonConvert.SerializeObject(syncP2, _serializerSettings), NetworkPacketType.GameSync, p2.Connection, p2.ClientCrypto);
                                    }
                                    else
                                    {
                                        Log.WriteLine(LogPriority.Error, "GameServer: Joining player 2 (" + p2.UID + ") for game " + game.UID + " failed!");
                                    }
                                }
                            }
                            else if (jMsg.Request == JoinRequestType.Decline)
                            {
                                if (player == p1)
                                {
                                    // Notify p2
                                    if (p2 == null) Log.WriteLine(LogPriority.Warning, "GameServer: Player " + player.UID + " sent a decline join, but there is no second player!");
                                    else
                                    {
                                        game.JoinPlayer(p2, false);
                                        var p2Answ = new JoinMessage { UID = game.UID, Request = JoinRequestType.Decline };
                                        Send(JsonConvert.SerializeObject(p2Answ, _serializerSettings), NetworkPacketType.JoinGame, p2.Connection, p2.ClientCrypto);
                                    }
                                }
                                else if (player == p2)
                                {
                                    // A player that might have wanted to join could send a decline to cancel the join
                                    game.JoinPlayer(p2, false);
                                    // Notify p1 so that his 'Player Joining' screen gets closed
                                    var p1Answ = new JoinMessage { UID = game.UID, Request = JoinRequestType.Decline, JoiningUser = p2.Name };
                                    Send(JsonConvert.SerializeObject(p1Answ, _serializerSettings), NetworkPacketType.JoinGame, p1.Connection, p1.ClientCrypto);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(LogPriority.Error, "GameServer: Received JoinGame packet of player " + player.UID + " could not be read! " + ex.Message);
                    }
                    break;
                case NetworkPacketType.ExitGame:
                    try
                    {
                        var eMsg = JsonConvert.DeserializeObject<ExitGame>(Encoding.ASCII.GetString(data));
                        if (eMsg != null)
                        {
                            NetworkPlayer p1, p2;
                            NetworkGame game;
                            if (GetGameAndPlayers(eMsg.UID, out game, out p1, out p2) && (p1 == player || p2 == player))
                            {
                                game.ExitGame(player);
                                lock (Games)
                                {
                                    Games.Remove(game.UID);
                                }

                                // Notify who has won the game
                                var syncP1 = GameSync.FromGame(game, game.UID, 1);
                                var syncP2 = GameSync.FromGame(game, game.UID, 2);
                                Send(JsonConvert.SerializeObject(syncP1, _serializerSettings), NetworkPacketType.GameSync, p1.Connection, p1.ClientCrypto);
                                Send(JsonConvert.SerializeObject(syncP2, _serializerSettings), NetworkPacketType.GameSync, p2.Connection, p2.ClientCrypto);

                                var ans = new ExitGame { UID = game.UID };
                                if (p1 != null)
                                    Send(JsonConvert.SerializeObject(ans, _serializerSettings), NetworkPacketType.ExitGame, p1.Connection, p1.ClientCrypto);
                                if (p2 != null)
                                    Send(JsonConvert.SerializeObject(ans, _serializerSettings), NetworkPacketType.ExitGame, p2.Connection, p2.ClientCrypto);
                            }
                            else KickPlayerOut(player, eMsg.UID);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(LogPriority.Error, "GameServer: Received ExitGame packet of player " + player.UID + " could not be read! " + ex.Message);
                    }
                    break;
                case NetworkPacketType.Rematch:
                    try
                    {
                        var eMsg = JsonConvert.DeserializeObject<Rematch>(Encoding.ASCII.GetString(data));
                        if (eMsg != null)
                        {
                            NetworkPlayer p1, p2;
                            NetworkGame game;
                            if (GetGameAndPlayers(eMsg.UID, out game, out p1, out p2) && (p1 == player || p2 == player))
                            {
                                if (player == p1)
                                    game.RematchRequested[0] = true;
                                if (player == p2)
                                    game.RematchRequested[1] = true;

                                if (game.RematchRequested[0] && game.RematchRequested[1])
                                {
                                    game.RematchRequested[0] = false;
                                    game.RematchRequested[1] = false;
                                    game.InitGame();
                                    var syncP1 = GameSync.FromGame(game, game.UID, 1);
                                    var syncP2 = GameSync.FromGame(game, game.UID, 2);
                                    Send(JsonConvert.SerializeObject(syncP1, _serializerSettings), NetworkPacketType.GameSync, p1.Connection, p1.ClientCrypto);
                                    Send(JsonConvert.SerializeObject(syncP2, _serializerSettings), NetworkPacketType.GameSync, p2.Connection, p2.ClientCrypto);
                                }
                            }
                            else KickPlayerOut(player, eMsg.UID);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(LogPriority.Error, "GameServer: Received Rematch packet of player " + player.UID + " could not be read! " + ex.Message);
                    }
                    break;
                case NetworkPacketType.GameCommand:
                    try
                    {
                        var cmdMsg = JsonConvert.DeserializeObject<GameCommand>(Encoding.ASCII.GetString(data));
                        if (cmdMsg != null)
                        {
                            NetworkPlayer p1, p2;
                            NetworkGame game;
                            if (GetGameAndPlayers(cmdMsg.UID, out game, out p1, out p2) && (p1 == player || p2 == player))
                            {
                                var oldPhase = game.Phase;
                                var result = game.ExecuteCommand(cmdMsg.Command, p1 == player ? 1 : 2).GetAwaiter().GetResult();
                                var response = new GameCommand
                                {
                                    UID = game.UID,
                                    Command = result ? "OK" : "FAIL"
                                };
                                Send(JsonConvert.SerializeObject(response, _serializerSettings), NetworkPacketType.GameCommand, player.Connection, player.ClientCrypto);

                                if (oldPhase == GamePhase.Deployment && game.Phase == GamePhase.Deployment)
                                    return; // The following game sync would reset the cards of the player who hasn't deployed yet

                                if (result)
                                {
                                    // Apply ELO
                                    if ((oldPhase == GamePhase.Player1Turn || oldPhase == GamePhase.Player2Turn) &&
                                         (game.Phase == GamePhase.Player1Win || game.Phase == GamePhase.Player2Win)
                                         && _userDatabase != null)
                                    {
                                        try
                                        {
                                            var elop1 = game.Players[0].ELO;
                                            var elop2 = game.Players[1].ELO;
                                            int winner = game.Phase == GamePhase.Player1Win ? 1 : 2;
                                            int elop1New, elop2New;

                                            if (elop1 > 0 && elop2 > 0)
                                            {
                                                EloRating.Calculate(elop1, elop2, winner, out elop1New, out elop2New);
                                                game.Players[0].ELO = elop1New;
                                                game.Players[1].ELO = elop2New;
                                                p1.ELO = elop1New;
                                                p2.ELO = elop2New;
                                                _userDatabase.SetELO(game.Players[0].Name, elop1New).GetAwaiter().GetResult(); // TODO async
                                                _userDatabase.SetELO(game.Players[1].Name, elop2New).GetAwaiter().GetResult();
                                            }
                                        }
                                        catch (Exception ee)
                                        {
                                            Log.WriteLine(LogPriority.Error, "Error while updating ELO:" + ee.Message );
                                        }
                                    }

                                    var syncP1 = GameSync.FromGame(game, game.UID, 1);
                                    var syncP2 = GameSync.FromGame(game, game.UID, 2);
                                    Send(JsonConvert.SerializeObject(syncP1, _serializerSettings), NetworkPacketType.GameSync, p1.Connection, p1.ClientCrypto);
                                    Send(JsonConvert.SerializeObject(syncP2, _serializerSettings), NetworkPacketType.GameSync, p2.Connection, p2.ClientCrypto);
                                }
                            }
                            else KickPlayerOut(player, cmdMsg.UID);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(LogPriority.Error, "GameServer: Received GameCommand packet of player " + player.UID + " could not be read! " + ex.Message);
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
