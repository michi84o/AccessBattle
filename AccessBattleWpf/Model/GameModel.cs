using AccessBattle.Networking;
using AccessBattle.Networking.Packets;
using AccessBattle.Wpf.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AccessBattle.Wpf.Model
{
    public class GameModel : PropChangeNotifier
    {
        GameViewModel _game;
        public GameViewModel Game => _game;

        uint _uid;
        /// <summary>
        /// Unique ID of the current game. Used for network games.
        /// </summary>
        public uint UID
        {
            get { return _uid; }
            set { SetProp(ref _uid, value); }
        }

        NetworkGameClient _client = new NetworkGameClient();
        public NetworkGameClient Client => _client;

        bool _isPlayerHost = true;
        public bool IsPlayerHost
        {
            get { return _isPlayerHost; }
            set { SetProp(ref _isPlayerHost, value); }
        }

        // TODO: Behavior when opponent disconnects
        // TODO: Option to give up game

        // For UI synchronization
        //SynchronizationContext _context; // TODO: Maybe later

        public GameModel()
        {
             _game = new GameViewModel(this);
            _client.GameSyncReceived += GameSyncReceived;
            //_context = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        public void ConvertCoordinates(ref int x, ref int y)
        {
            if (IsPlayerHost) return; // No rotation

            // If player is not host, the board is rotated by 180°
            // This only affects the board itself

            // Board fields:
            if (y >= 0 && y <= 7)
            {
                x = 7 - x;
                y = 7 - y;
            }
            // Stack P1
            else if (y == 8)
            {
                y = 9;
                x = 7 - x;
            }
            // Stack P2
            else if (y == 9)
            {
                y = 8;
                x = 7 - x;
            }
            // Server area
            else if (y == 10)
            {
                if (x == 4) x = 5;
                else if (x == 5) x = 4;
            }
        }

        #region Game Synchronization

        void GameSyncReceived(object sender, GameSyncEventArgs e)
        {
            if (UID != e.Sync.UID) return; // TODO: Tell server?
            //_context.Post(o => { _game.Synchronize(e.Sync); }, null);

            if (e.Sync.UID != UID)
            {
                Log.WriteLine("GameModel: Error! Server sent GameSync for wrong game! Local UID: " + UID + ", server UID: " + e.Sync.UID);
                // TODO: Tell server?
                return;
            }
            Application.Current.Dispatcher.Invoke(() => { _game.Synchronize(e.Sync); });
        }

        #endregion

    }
}
