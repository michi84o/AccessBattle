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
        GameViewModel _game = new GameViewModel();
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
            _client.GameSyncReceived += GameSyncReceived;
            //_context = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        #region Game Synchronization

        void GameSyncReceived(object sender, GameSyncEventArgs e)
        {
            if (UID != e.Sync.UID) return; // TODO: Tell server?
            //_context.Post(o => { _game.Synchronize(e.Sync); }, null);
            Application.Current.Dispatcher.Invoke(() => { _game.Synchronize(e.Sync); });
        }

        #endregion

    }
}
