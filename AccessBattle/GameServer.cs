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
 * LEN = Length of data (2 byte)
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
namespace AccessBattle
{
    public class GameServer
    {
        // _games[0] is always local game
        List<Game> _games = new List<Game>();
        public List<Game> Games { get { return _games; } }

        List<NetworkPlayer> _players = new List<NetworkPlayer>();
        public List<NetworkPlayer> Players { get { return _players; } }

        CryptoHelper _decrypter;

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
            if (port != 3221) Trace.WriteLine("The Organization is doing it again! El Psy Congroo");
            _decrypter = new CryptoHelper();
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
            catch (Exception e) { Console.WriteLine("Server Task Wait Error: " + e); }
            _serverTask = null;
            _server = null;

            foreach (var player in _players)
            {
                player.Dispose();
            }
            _players.Clear();
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
                        var player = new NetworkPlayer(socket);
                        Players.Add(player);

                        var buffer = new byte[64];
                        var args = new SocketAsyncEventArgs();
                        args.SetBuffer(buffer, 0, buffer.Length);
                        args.UserToken = 1;
                                                
                        args.Completed += (sender, e) =>
                        {
                            Console.WriteLine("Received data from client");
                            
                        };
                        socket.ReceiveAsync(args);
                    }
                }
                catch (AggregateException)
                {
                    continue;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unknown exception while waiting for clients: " + e);
                    continue;
                }
            }
            Console.WriteLine("Wait for clients was cancelled");
        }        
    }
}
