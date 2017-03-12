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
 *      - 0x02: Login Client[user;password (string)]
 *      - 0x03: List available games Client[] Server[game1;game2;... (string)]
 *      - 0x04: Create Game Client[name (string)] Server[0/1 (string, 0=OK, 1=Error)]
 *      - 0x05: Join Game Client[name (string)] Server[0/1 (string, 0=OK, 1=Error)]
 *      - 0x06: Game Init [opponent name, client player number]
 *      - 0x07: Game Status Change [??? TODO]
 *      - 0x08: Game Command
 * DATA = Packet data (encrypted, except public key)
 * CHSUM = Checksum (all bytes XOR'ed. except STX,CHSUM,ETX)
 * ETX = End of packet   0x03
 *  
 */
namespace AccessBattle.Networking
{
    public class GameServer : NetworkBase
    {
        // _games[0] is always local game
        public Dictionary<uint, Game> _games = new Dictionary<uint, Game>();
        public Dictionary<uint, Game> Games { get { return _games; } }

        Dictionary<uint,NetworkPlayer> _players = new Dictionary<uint, NetworkPlayer>();
        public Dictionary<uint, NetworkPlayer> Players { get { return _players; } }

        ushort _port;
        public ushort Port { get { return _port; } }

        Task _serverTask;

        public GameServer(ushort port = 3221)
        {
            if (port < 1024)
            {
                // 3221 is the sum of the bytes in the ASCII string "Rai-Net Access Battlers Steins;Gate"
                // Also the sum of the digits is 8, which is the final number of lab members of the first VN.
                port = 3221; // OSH MK UF A 2010
            }
            _port = port;
            if (port != 3221) Trace.WriteLine("The Organization has made their move! El Psy Congroo");
        }

        TcpListener _server;
        CancellationTokenSource _serverCts;

        public void Start()
        {
            Stop();
            _server = new TcpListener(IPAddress.Any, _port);
            _server.Start();
            _serverCts = new CancellationTokenSource();
            _serverTask = Task.Run(() => ListenForClients(_serverCts.Token));
        }

        public void Stop()
        {
            if (_server == null) return;
            _serverCts.Cancel();
            _server.Stop();            
            try { _serverTask.Wait(); }
            catch (Exception e) { Log.WriteLine("Server Task Wait Error: " + e); }
            _serverTask = null;
            _server = null;

            foreach (var item in _players)
            {
                item.Value.Dispose();
            }
            _players.Clear();
        }

        uint GetUid()
        {
            var guid = Guid.NewGuid().ToByteArray(); // 16 byte
            var uid = guid[0] | guid[0] << 8 | guid[0] << 16 | guid[0] << 24;
            return (uint)uid;
        }

        void ListenForClients(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var socketTask = _server.AcceptSocketAsync();
                    socketTask.Wait();
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
                            while (Players.ContainsKey((uid = GetUid()))) { }
                            var player = new NetworkPlayer(socket, uid, serverCrypto);
                            Players.Add(uid, player);
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
                    Log.WriteLine("Unknown exception while waiting for clients: " + e);
                    continue;
                }
            }
            Log.WriteLine("Wait for clients was cancelled");
        }

        protected override void Receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError != SocketError.Success)
                {
                    // Will be hit after client disconnect
                    Log.WriteLine("Server receive for client (UID:"+e.UserToken+") not successful: " + e.SocketError);
                }
                else if (e.BytesTransferred > 0)
                {
                    Log.WriteLine("Received data from client, UID: " + e.UserToken);
                    NetworkPlayer player;
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
                        Log.WriteLine("GameServer: Received public key of player " + player.UID + " is invalid!");
                    }
                    break;
                case NetworkPacketType.ListGames:
                    Log.WriteLine("GameServer: Player " + player.UID + " requesting game list");
                    var glist = Games.Select(kv => kv.Value).Where(v => v.Phase == GamePhase.Init && v.Players[1].Client == null && v.UID != 0).ToList();
                    var info = new List<GameInfo>();
                    foreach (var game in glist)
                    {
                        info.Add(new GameInfo { UID = game.UID, Name = game.Name, Player1 = game.Players[0].Name });
                    }
                    var infoJson =
                    Send(JsonConvert.SerializeObject(info), NetworkPacketType.ListGames, player.Connection, player.ClientCrypto);
                    break;
            }
        }
    }
}
