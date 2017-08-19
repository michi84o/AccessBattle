using AccessBattle.Networking;
using AccessBattle.Networking.Packets;
using AccessBattle.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

// TODO: Behavior when opponent disconnects
// TODO: Option to give up game
// TODO: Add a "register local game" method
// TODO: Add a "register remote game" method
// ==> Maybe just pass a reference to the game model?

namespace AccessBattle.Wpf.ViewModel
{
    /// <summary>
    /// View model for displaying the state of a game.
    /// Includes board and cards.
    /// Provides abstraction of the game class for remote and local games.
    /// </summary>
    public class GameViewModel : PropChangeNotifier, IBoardGame
    {
        IMenuHolder _parent;

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
                //x = 7 - x;
            }
            // Stack P2
            else if (y == 9)
            {
                y = 8;
                //x = 7 - x;
            }
            // Server area
            else if (y == 10)
            {
                if (x == 4) x = 5;
                else if (x == 5) x = 4;
            }
        }

        // This field must only be changed in the HandleFieldSelection method!
        // Allowed range: -1 to (BoardFieldList.Count-1)
        int _selectedField = -1;

        public bool CanConfirmDeploy =>
            Phase == GamePhase.Deployment &&
            BoardFields[0, 0].HasCard &&
            BoardFields[1, 0].HasCard &&
            BoardFields[2, 0].HasCard &&
            BoardFields[3, 1].HasCard &&
            BoardFields[4, 1].HasCard &&
            BoardFields[5, 0].HasCard &&
            BoardFields[6, 0].HasCard &&
            BoardFields[7, 0].HasCard;


        public event EventHandler CardMoved;

        public void HandleFieldSelection(int index)
        {
            if (_parent.IsBusy) return;
            if (index < 0 || index >= BoardFieldList.Count) return;

            // index can be calculated as: 8*y + x

            var vm = BoardFieldList[index];
            if (vm == null ||vm.Field == null) return;

            if ((!(Phase == GamePhase.Deployment) &&
               !(IsPlayerHost && Phase == GamePhase.Player1Turn) &&
               !(!IsPlayerHost && Phase == GamePhase.Player2Turn))) return;

            // In this mode we do not send packets to server while moving cards
            if (Phase == GamePhase.Deployment)
            {
                #region Deployment
                if (_parent.CurrentMenu != MenuType.Deployment) return; // Already deployed
                if (!vm.IsDeploymentField || (_selectedField < 0 && !vm.HasCard))
                {
                    ClearHighlighting();
                    _selectedField = -1;
                    return;
                }
                if (_selectedField < 0)
                {
                    _selectedField = index;
                    // Highlight all other deployment fields
                    for (int x = 0; x <= 7; ++x)
                    {
                        var y = 0;
                        if (x == 3 || x == 4) y = 1;
                        BoardFields[x, y].IsHighlighted = (8 * y + x) != index;
                    }
                    vm.IsHighlighted = false;
                    return;
                }
                // Move card. If target field already has card, switch them
                var source = BoardFieldList[_selectedField];
                var sourceCard = source.Field.Card;
                var targetCard = vm.Field.Card;
                vm.Field.Card = sourceCard;
                source.Field.Card = targetCard;
                ClearHighlighting();
                _selectedField = -1;
                OnPropertyChanged(nameof(CanConfirmDeploy));
                CardMoved?.Invoke(this, EventArgs.Empty);
                CommandManager.InvalidateRequerySuggested();
                return;
                #endregion
            }
            else if (Phase == GamePhase.Player1Turn && IsPlayerHost ||
                     Phase == GamePhase.Player2Turn && !IsPlayerHost)
            {
                var player = IsPlayerHost ? 1 : 2;
                // Possible actions:
                #region 1 Select / Deselect card
                if (_selectedField == index)
                {
                    ClearHighlighting();
                    _selectedField = -1;
                    return;
                }
                if (_selectedField < 0)
                {
                    if (!vm.HasCard || vm.Field.Card?.Owner?.PlayerNumber != player) return;
                    _selectedField = index;
                }
                #endregion
                #region 2 Move card to empty field
                #endregion
                #region 3 Move card to opponent card
                #endregion
                #region 4 Place/Remove Boost
                #endregion
                #region 5 Place/Remove Firewall
                #endregion
                #region 6 Move Card to Stack (when on exit field)
                #endregion
                #region 7 Use Error 404
                #endregion
                #region 8 Use Virus Check
                #endregion

            }
            return;

            //if (index < 64)
            //{
            //    // Main board field clicked
            //    if (_selectedField < 0)
            //    {
            //        _selectedField = index;
            //        return;
            //    }
            //    if (_selectedField == index)
            //    {
            //        _selectedField = -1;
            //        return;
            //    }
            //    if (_selectedField < 0 || _selectedField > BoardFieldList.Count) return;
            //    // Send movement request to server
            //    var from = BoardFieldList[_selectedField];
            //    if (from == null || from.Field == null || vm.Field == null) return;
            //    _client.SendGameCommand(_uid, string.Format("mv {0},{1},{2},{3}",
            //        from.Field.X, from.Field.Y, vm.Field.X, vm.Field.Y ));
            //    _selectedField = -1;
            //}
            //else if (index < 72)
            //{
            //    // Stack p1
            //    MessageBox.Show("Stack P1");
            //}
            //else if (index < 80)
            //{
            //    // Stack p2
            //    MessageBox.Show("Stack P2");
            //}
            //else if (index == 83)
            //{
            //    // Server area p1
            //    MessageBox.Show("Server P1");
            //}
            //else if (index == 84)
            //{
            //    // Server area p2
            //    MessageBox.Show("Server P2");
            //}
        }

        /// <summary>
        /// Disable all flashing
        /// </summary>
        void ClearHighlighting()
        {
            foreach (var field in BoardFieldList)
                field.IsHighlighted = false;
            UiGlobals.Instance.StopFlashing();
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

        GamePhase _phase;
        /// <summary>
        /// Current game phase.
        /// </summary>
        public GamePhase Phase
        {
            get { return _phase; }
            set
            {
                SetProp(_phase, value, ()=>
                {
                    if ((_phase == GamePhase.Deployment || _phase == GamePhase.Player1Turn || _phase == GamePhase.Player2Turn)
                      && !(value == GamePhase.Deployment || value == GamePhase.Player1Turn || value == GamePhase.Player2Turn))
                    {
                        ClearHighlighting();
                    }
                    _phase = value;
                    IsActionsMenuVisible = false;
                });
            }
        }

        PlayerState[] _players;
        public PlayerState[] Players => _players;

        public List<BoardFieldViewModel> BoardFieldList { get; private set; }
        public BoardFieldViewModel[,] BoardFields { get; private set; }
        public BoardField[,] Board { get; private set; }

        public GameViewModel(IMenuHolder parent)
        {
            _parent = parent;
            BoardFieldList = new List<BoardFieldViewModel>();
            BoardFields = new BoardFieldViewModel[8, 11];
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
            CommandManager.InvalidateRequerySuggested(); // Confirm button on deployment field does not get enabled
        }

        #endregion

        bool _isActionsMenuVisible;
        public bool IsActionsMenuVisible
        {
            get { return _isActionsMenuVisible; }
            set
            {
                if (value &&
                    !(Phase == GamePhase.Player1Turn && IsPlayerHost ||
                      Phase == GamePhase.Player2Turn && !IsPlayerHost))
                {
                    return; // Actions menu can only be opened when it is players turn
                }
                SetProp(ref _isActionsMenuVisible, value);
            }
        }
    }
}
