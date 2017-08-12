using AccessBattle.Networking;
using AccessBattle.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

// TODO: Behavior when opponent disconnects
// TODO: Option to give up game

namespace AccessBattle.Wpf.ViewModel
{
    /// <summary>
    /// View model for displaying the state of a game.
    /// Includes board and cards.
    /// Provides abstraction of the game class for remote and local games.
    /// </summary>
    public class GameViewModel : PropChangeNotifier, IBoardGame
    {
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



        public void HandleFieldSelection(BoardFieldViewModel vm)
        {
            if (vm?.Field == null) return;
            var field = vm.Field;
            var index = 8 * field.Y + field.X;

            if (index < 64)
            {
                // Main board field clicked
                MessageBox.Show("Main field");
                //field.Card = new OnlineCard { HasBoost = true, Owner = _game.Players[0], IsFaceUp = true, Type = OnlineCardType.Link };
                //vm.IsHighlighted = true;
                //UiGlobals.Instance.StartFlashing();
                //UiGlobals.Instance.StartMultiOverlayFlashing();
            }
            else if (index < 72)
            {
                // Stack p1
                MessageBox.Show("Stack P1");
            }
            else if (index < 80)
            {
                // Stack p2
                MessageBox.Show("Stack P2");
            }
            else if (index == 83)
            {
                // Server area p1
                MessageBox.Show("Server P1");
            }
            else if (index == 84)
            {
                // Server area p2
                MessageBox.Show("Server P2");
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
            Application.Current.Dispatcher.Invoke(() => { Synchronize(e.Sync); });
        }

        #endregion

        #region Game

        // TODO: Add a "register local game" method
        // TODO: Add a "register remote game" method
        // ==> Maybe just pass a reference to the game model?

        GamePhase _phase;
        /// <summary>
        /// Current game phase.
        /// </summary>
        public GamePhase Phase
        {
            get { return _phase; }
            set { SetProp(ref _phase, value); }
        }

        PlayerState[] _players;
        public PlayerState[] Players => _players;

        public BoardField[,] Board { get; private set; }

        public GameViewModel()
        {
            Board = new BoardField[8, 11];
            for (ushort y = 0; y < 11; ++y)
                for (ushort x = 0; x < 8; ++x)
                {
                    Board[x, y] = new BoardField(x, y);
                }

            _players = new PlayerState[2];
            _players[0] = new PlayerState(1);
            _players[1] = new PlayerState(2);
            _client.GameSyncReceived += GameSyncReceived;
        }

        public void Synchronize(GameSync sync)
        {
            // Clear board
            for (ushort y = 0; y < 11; ++y)
                for (ushort x = 0; x < 8; ++x)
                {
                    Board[x, y].Card = null;
                }
            // Update all fields
            _players[0].Update(sync.Player1);
            _players[1].Update(sync.Player2);
            foreach (var field in sync.FieldsWithCards)
            {
                int x = field.X;
                int y = field.Y;
                ConvertCoordinates(ref x, ref y);
                Board[x, y].Update(field, _players);
            }
            Phase = sync.Phase;
        }

        #endregion
    }
}
