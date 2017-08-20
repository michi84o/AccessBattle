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

// TODO: Remove action items that cannot be played from UI

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
        // Only used for Error 404
        int _secondSelectedField = -1;

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

        bool _isVirusCheckSelected;
        public bool IsVirusCheckSelected
        {
            get { return _isVirusCheckSelected; }
            set { SetProp(ref _isVirusCheckSelected, value); }
        }

        bool _isFirewallSelected;
        public bool IsFirewallSelected
        {
            get { return _isFirewallSelected; }
            set { SetProp(ref _isFirewallSelected, value); }
        }

        bool _isLineBoostSelected;
        public bool IsLineBoostSelected
        {
            get { return _isLineBoostSelected; }
            set { SetProp(ref _isLineBoostSelected, value); }
        }

        bool _isError404Selected;
        public bool IsError404Selected
        {
            get { return _isError404Selected; }
            set { SetProp(ref _isError404Selected, value); }
        }

        public event EventHandler CardMoved;

        bool IsAnyActionItemSelected => IsError404Selected || IsLineBoostSelected || IsVirusCheckSelected || IsFirewallSelected;

        public void HandleActionItem(ActionItem item)
        {
            if (_parent.IsBusy) return;
            if (!(IsPlayerHost && Phase == GamePhase.Player1Turn) &&
                !(!IsPlayerHost && Phase == GamePhase.Player2Turn)) return;

            if (IsAnyActionItemSelected)
            {
                ClearFieldSelection();
                return;
            }

            var player = IsPlayerHost ? 1 : 2;
            var pl = Players[player - 1];

            switch (item)
            {
                case ActionItem.VirusCheck:
                    if (pl.DidVirusCheck) return;
                    IsVirusCheckSelected = true;
                    break;
                case ActionItem.Firewall:
                    // Check if firewall was placed
                    var firewallCard = BoardFieldList.FirstOrDefault(f => f.Field.Y < 8 && f.HasCard && f.Field.Card is FirewallCard && f.Field.Card.Owner.PlayerNumber == player);
                    if (firewallCard != null)
                    {
                        SendGameCommand(string.Format("fw {0},{1},{2}", firewallCard.Field.X, firewallCard.Field.Y, 0));
                        return;
                    }
                    IsFirewallSelected = true;
                    break;
                case ActionItem.LineBoost:
                    // Check if line boost was placed
                    // TODO: Access must be synchronized
                    var lineBoostCard = BoardFieldList.FirstOrDefault(f => f.Field.Y < 8 && f.HasCard && f.Field.Card is OnlineCard && f.Field.Card.Owner.PlayerNumber == player && ((OnlineCard)f.Field.Card).HasBoost);
                    if (lineBoostCard != null)
                    {
                        SendGameCommand(string.Format("bs {0},{1},{2}", lineBoostCard.Field.X, lineBoostCard.Field.Y, 0));
                        return;
                    }
                    IsLineBoostSelected = true;
                    break;
                case ActionItem.Error404:
                    if (pl.Did404NotFound) return;
                    IsError404Selected = true;
                    break;
            }
        }

        void SendGameCommand(string command)
        {
            MessageBox.Show("TODO: Send game command\r\n" + command);
            //_client.SendGameCommand(UID, command);
        }

        public void PlayError404(bool switchCards)
        {
            BoardFieldViewModel vm1 = null;
            BoardFieldViewModel vm2 = null;
            if (_selectedField >= 0 && _selectedField < 64)
                vm1 = BoardFieldList[_selectedField];
            if (_secondSelectedField >= 0 && _secondSelectedField < 64)
                vm2 = BoardFieldList[_secondSelectedField];

            var card1 = vm1.Field.Card;
            var card2 = vm2.Field.Card;
            var playerNum = IsPlayerHost ? 1 : 2;

            _parent.CurrentMenu = MenuType.None;

            if (card1?.Owner?.PlayerNumber == playerNum && card2?.Owner?.PlayerNumber == playerNum &&
                card1 is OnlineCard && card2 is OnlineCard)
            {
                SendGameCommand(string.Format("er {0},{1},{2},{3},{4}", vm1.Field.X, vm1.Field.Y, vm2.Field.X, vm2.Field.Y, switchCards ? 1 : 0));
            }
            ClearFieldSelection();
            ClearHighlighting();
        }

        public void HandleFieldSelection(int index)
        {
            if (_parent.IsBusy) return;
            if (index < 0 || index >= BoardFieldList.Count) return;

            // index can be calculated as: 8*y + x

            var vm = BoardFieldList[index];
            if (vm == null || vm.Field == null) return;

            if (!(Phase == GamePhase.Deployment) &&
               !(IsPlayerHost && Phase == GamePhase.Player1Turn) &&
               !(!IsPlayerHost && Phase == GamePhase.Player2Turn)) return;

            // In this mode we do not send packets to server while moving cards
            if (Phase == GamePhase.Deployment)
            {
                if (_parent.CurrentMenu != MenuType.Deployment) return; // Already deployed
                if (!vm.IsDeploymentField || (_selectedField < 0 && !vm.HasCard))
                {
                    ClearHighlighting();
                    ClearFieldSelection();
                    return;
                }
                if (_selectedField < 0)
                {
                    _selectedField = index;
                    vm.IsSelected = true;
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
                ClearFieldSelection();
                OnPropertyChanged(nameof(CanConfirmDeploy));
                CardMoved?.Invoke(this, EventArgs.Empty);
                CommandManager.InvalidateRequerySuggested();
                return;
            }
            if (Phase == GamePhase.Player1Turn && IsPlayerHost ||
                Phase == GamePhase.Player2Turn && !IsPlayerHost)
            {
                var playerNum = IsPlayerHost ? 1 : 2;
                var opponent = IsPlayerHost ? 2 : 1;
                var player = Players[playerNum - 1]; // is never null

                if (_parent.CurrentMenu == MenuType.SwitchCards)
                {
                    // User tried to play Error 404 but aborted
                    ClearFieldSelection();
                    ClearHighlighting();
                    _parent.CurrentMenu = MenuType.None;
                    return;
                }

                // Action item handling is special. Do it first
                if (IsAnyActionItemSelected)
                {
                    if (_isLineBoostSelected)
                    {
                        if (vm.HasCard && vm.Field.Card?.Owner?.PlayerNumber == playerNum)
                            SendGameCommand(string.Format("bs {0},{1},{2}", vm.Field.X, vm.Field.Y, 1));
                    }
                    else if (_isFirewallSelected)
                    {
                        if (!vm.HasCard &&
                            index != 83 && index != 84
                            && index != 3 && index != 4 && index != 3 + 8 * 7 && index != 4 + 8 * 7)
                            SendGameCommand(string.Format("fw {0},{1},{2}", vm.Field.X, vm.Field.Y, 1));
                    }
                    else if (_isVirusCheckSelected)
                    {
                        if (vm.Field?.Card?.Owner.PlayerNumber == opponent)
                            SendGameCommand(string.Format("vc {0},{1}", vm.Field.X, vm.Field.Y));
                    }
                    else if (_isError404Selected)
                    {
                        if (vm.Field.Card?.Owner?.PlayerNumber == playerNum && vm.Field.Card is OnlineCard)
                        {
                            if (_selectedField < 0)
                            {
                                _selectedField = index;
                                vm.IsSelected = true;
                                return;
                            }
                            // Unselect
                            if (_selectedField == index)
                            {
                                vm.IsSelected = false;
                                _selectedField = -1;
                                return;
                            }
                            // Second card selected
                            // Check if first selected field is OK
                            OnlineCard firstCard = null;
                            if (_selectedField < BoardFieldList.Count)
                                firstCard = BoardFieldList[_selectedField].Field.Card as OnlineCard;
                            if (firstCard != null)
                            {
                                vm.IsSelected = true;
                                _secondSelectedField = index;
                                _parent.CurrentMenu = MenuType.SwitchCards;
                                return;
                            }
                        }
                    }
                    ClearFieldSelection();
                    ClearHighlighting();
                    return;
                }

                // If we reach this point, then no action items are selected

                // If selected field is clicked, deselect it
                if (_selectedField == index)
                {
                    ClearFieldSelection();
                    ClearHighlighting();
                    return;
                }

                // Select field
                if (_selectedField < 0)
                {
                    if (!vm.HasCard || vm.Field.Card?.Owner?.PlayerNumber != playerNum) return;
                    ClearFieldSelection();
                    ClearHighlighting();
                    vm.IsSelected = true;
                    _selectedField = index;
                    return;
                }

                // Move card
                if ((index < 64 || index == 83 || index == 84)
                    && _selectedField < 64)
                {
                    var from = BoardFieldList[_selectedField];
                    // TODO: Ask Game class if move is possible
                    // TODO: Exclude own exit fields
                    SendGameCommand(string.Format("mv {0},{1},{2},{3}", from.Field.X, from.Field.Y, vm.Field.X, vm.Field.Y));
                }
                // Any other field that was clicked resets the selection
                ClearFieldSelection();
                ClearHighlighting();
            }
            return;
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

        void ClearFieldSelection()
        {
            foreach (var field in BoardFieldList)
                field.IsSelected = false;
            IsFirewallSelected = false;
            IsLineBoostSelected = false;
            IsVirusCheckSelected = false;
            IsError404Selected = false;
            _selectedField = -1;
            _secondSelectedField = -1;
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
                        ClearFieldSelection();
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
