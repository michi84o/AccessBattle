using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// TODO

namespace AccessBattle
{
    public class GameServer
    {
        // _games[0] is always local game
        List<Game> _games = new List<Game>();
        public List<Game> Games { get { return _games; } }

        ushort _port;
        public ushort Port { get { return _port; } }

        Task _serverTask;

        public GameServer(ushort port = 3221)
        {
            if (port < 1024)
            {
                // 3221 is the sum of the bytes in the ASCII string "Rai-Net Access Battlers Steins;Gate"
                port = 3221;
            }
            _port = port;
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
