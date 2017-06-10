using AccessBattle.Networking;
using AccessBattle.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Wpf.Model
{
    public class GameModel : PropChangeNotifier
    {
        Game _game = new Game();
        public Game Game { get { return _game; } }

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

        public GameModel()
        {
            _client.GameSyncReceived += GameSyncReceived;
        }

        #region Game Synchronization

        void GameSyncReceived(object sender, GameSyncEventArgs e)
        {
            if (UID != e.Sync.UID) return; // TODO: Tell server
        }

        #endregion

    }
}
