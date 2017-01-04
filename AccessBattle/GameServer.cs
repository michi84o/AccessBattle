using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// NOT WORKING... Still in concept phase

namespace AccessBattle
{
    public class GameServer : PropChangeNotifier
    {
        bool _isRunning = false;
        bool _isWaitingForClients = false;
        bool _isManagingGames = false;
        public bool IsRunning
        {
            get { return _isRunning; }
        }

        List<Player> _currentPlayers = new List<Player>();
        List<Game> _gameSessions = new List<Game>();

        SynchronizationContext _context;

        public GameServer()
        {
            _context = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        public async void Start(ushort port)
        {
            if (_isRunning) return;

            _isRunning = true;
            _isWaitingForClients = true;
            OnPropertyChanged("IsRunning");


#pragma warning disable CC0022 // Should dispose object

            // Task for awaiting new Clients
            (new Task(
            () =>
            {
                try
                {
                    var listener = new TcpListener(IPAddress.Any, port);
                    listener.Start();
                    while (listener.Pending())
                    {
                        Thread.Sleep(50);
                        if (!_isRunning)
                        {
                            listener.Stop();
                            return;
                        }
                    }
                    var sock = listener.AcceptSocket();
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e);
                }
                finally
                {
                    _isRunning = false;
                    _isWaitingForClients = false;
                    _context.Send(o => OnPropertyChanged("IsRunning"),null);
                }
            }
            )).Start();

            _isManagingGames = true;

            // Task for managing the game
            (new Task(() => 
            {
                try
                {

                }
                catch (Exception e)
                {
                    Trace.WriteLine(e);
                }
                finally
                {
                    _isManagingGames = false;
                }
            })).Start();            


#pragma warning restore CC0022 // Should dispose object
        }

        public void Stop()
        {
            _isRunning = false;
            while (_isWaitingForClients || _isManagingGames)
            {
                Thread.Sleep(25);
            }
            OnPropertyChanged("IsRunning");
        }
    }

    public class GameSession
    {
        public Player Player1 { get; set; }
        public Player Player2 { get; set; }
    }
}
