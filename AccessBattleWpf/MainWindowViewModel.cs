using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AccessBattle;
using System.Diagnostics;

namespace AccessBattleWpf
{
    // TODO: Make GameV variables globally accessible

    public class BlinkChangedEventArgs : EventArgs
    {
        public Vector Position { get; private set; }
        public bool ForceAll { get; private set; }
        public BlinkChangedEventArgs(Vector position, bool forceAll = false)
        {
            Position = position;
            ForceAll = forceAll;
        }
    }

    public class MainWindowViewModel : PropChangeNotifier
    {
        Game _game;
        public Game Game { get {return _game;}}
        GameAI _gameAI;     

        bool _designerMode;

        public event EventHandler<BlinkChangedEventArgs> BlinkStateChanged;

        public MainWindowViewModel()
        {
            _game = new Game();

            // TODO: Not on network game
            // Or use this interface for the server
            _gameAI = new GameAI(_game);

            _game.PropertyChanged += Game_PropertyChanged;
            _designerMode = WpfHelper.IsInDesignerMode;
        }

        void Game_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Phase")
            {
                CurrentDeploymentType = OnlineCardType.Unknown;
                ResetBlink();
                _isLineBoostP1Selected = false;
                _isFirewallP1Selected = false;
                _isVirusCheckP1Selected = false;
                _isError404P1Selected = false;
                _VirusCheckP1Count = 0;
                _Error404P1Count = 0;
                OnPropertyChanged("IsVirusCheckVisible");
                OnPropertyChanged("IsError404P1Visible");
                switch (_game.Phase)
                {
                    case GamePhase.Init:
                        break;
                    case GamePhase.Deployment:
                        // Empty deployment fields must be blinking
                        foreach (var field in Game.Board.GetPlayerDeploymentFields(1))
                        {
                            SetBlink(field.Position, true);
                        }
                        LinkCardsToDeploy = 4;
                        VirusCardsToDeploy = 4;
                        CurrentDeploymentType = OnlineCardType.Link;
                        break;
                    case GamePhase.PlayerTurns:
                        // TODO: Decide which player starts
                        Game.CurrentPlayer = 1;
                        break;
                    case GamePhase.GameOver:
                        OnPropertyChanged("PlayerWinMessage"); 
                        break;
                }
                OnPropertyChanged("IsNewGamePopupVisible");
                OnPropertyChanged("IsDeploymentPopupVisible");
                OnPropertyChanged("IsGameOverPopupVisible");
            }
            else if (e.PropertyName == "CurrentPlayer")
            {
                OnPropertyChanged("IsWaitingForPlayer2");
                // TODO Server P2 move
                if (_game.CurrentPlayer == 2 && _gameAI != null)
                {
                    _gameAI.PlayTurn();
                }
                OnPropertyChanged("IsDeploymentPopupVisible");
            }
        }

        public bool GetBlink(Vector position)
        {
            if (position.X > 7 || position.Y > 10) return false;

            var num = (byte)(1 << position.X);
            return (_blinkMap[position.Y] & num) > 0;
        }

        void ResetBlink()
        {
            for (int i = 0; i < 11; ++i)
            {
                _blinkMap[i] = 0;
            }
            var handler = BlinkStateChanged;
            if (handler != null) handler(this, new BlinkChangedEventArgs(new Vector(0, 0), true));
            //if (handler != null) handler(this, new BlinkChangedEventArgs(new Vector(0, 0), true)); ?????
        }

        bool SetBlink(Vector position, bool isBlinking)
        {
            // horizontal: always 8 fields -> 1 byte
            // vertical: 11 fields: -> array index
            if (position.X > 7 || position.Y > 10) return false;
            if (GetBlink(position) == isBlinking) return true; // Nothing to do

            // Some bit magic. From good old atmega8 µC programming
                var num = (byte)(1 << position.X);
            if (isBlinking) _blinkMap[position.Y] |= num;
            else _blinkMap[position.Y] &= (byte)~num;

            var handler = BlinkStateChanged;
            if (handler != null) handler(this, new BlinkChangedEventArgs(position));
            return true;
        }

        byte[] _blinkMap = new byte[11];
        public byte[] BlinkMap { get { return _blinkMap; } }

        OnlineCardType _currentDeploymentType;
        public OnlineCardType CurrentDeploymentType
        {
            get { return _currentDeploymentType; }
            set { SetProp(ref _currentDeploymentType, value);  }
        }
        int _linkCardsToDeploy;
        public int LinkCardsToDeploy
        {
            get { return _linkCardsToDeploy; }
            set { SetProp(ref _linkCardsToDeploy, value); }
        }
        int _virusCardsToDeploy;
        public int VirusCardsToDeploy
        {
            get { return _virusCardsToDeploy; }
            set { SetProp(ref _virusCardsToDeploy, value); }
        }

        BoardFieldViewModel _currentlySelectedField;
        //BoardFieldViewModel _currentlySelectedField1; // Only used for switching when Error 404 is applied
        bool _isLineBoostP1Selected;
        bool _isFirewallP1Selected;
        bool _isVirusCheckP1Selected;
        int _VirusCheckP1Count = 0;
        int _Error404P1Count = 0;
        bool _isError404P1Selected;

        public System.Windows.Visibility IsVirusCheckVisible
        {
            get { return _VirusCheckP1Count == 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; }
        }

        public System.Windows.Visibility IsError404P1Visible
        {
            get { return _Error404P1Count == 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; }
        }

        public void FieldClicked(BoardFieldViewModel field)
        {
            try
            {
                Trace.WriteLine("Field clicked: " + field.Position.X + "," + field.Position.Y);
                #region Deployment
                if (_game.Phase == GamePhase.Deployment)
                {
                    if (_game.Board.GetPlayerDeploymentFields(1).Contains(field.Field))
                    {
                        var type = OnlineCardType.Unknown;
                        // Check if field already contains a card:
                        if (field.Card != null)
                        {
                            // Put card back
                            var stackfields = _game.Board.GetPlayerStackFields(1);
                            type = ((OnlineCard)field.Card).Type;
                            int startIndex = 0;
                            if (type == OnlineCardType.Virus) startIndex = 4;
                            for (int i = startIndex; i < stackfields.Count; ++i)
                            {
                                if (stackfields[i].Card == null)
                                {
                                    if (_game.ExecuteCommand(_game.CreateMoveCommand(field.Position, stackfields[i].Position)))
                                    {
                                        if (type == OnlineCardType.Link) ++LinkCardsToDeploy;
                                        if (type == OnlineCardType.Virus) ++VirusCardsToDeploy;
                                    }
                                    return;
                                }
                            }
                        }

                        var cardsOnStack = _game.Board.GetPlayerStackFields(1).FindAll(
                                o => o.Card != null && o.Card is OnlineCard).ToList();
                        BoardField fieldToMove = null;
                        if (_currentDeploymentType == OnlineCardType.Link && LinkCardsToDeploy > 0)
                        {   // Find next link card in stack
                            fieldToMove = cardsOnStack.FirstOrDefault(o => ((OnlineCard)o.Card).Type == OnlineCardType.Link);
                            type = OnlineCardType.Link;
                        }
                        else if (_currentDeploymentType == OnlineCardType.Virus && VirusCardsToDeploy > 0)
                        {   // Find next virus card in stack
                            fieldToMove = cardsOnStack.FirstOrDefault(o => ((OnlineCard)o.Card).Type == OnlineCardType.Virus);
                            type = OnlineCardType.Virus;
                        }
                        if (fieldToMove != null)
                        {
                            if (_game.ExecuteCommand(_game.CreateMoveCommand(fieldToMove.Position, field.Position)))
                            {
                                if (type == OnlineCardType.Link) --LinkCardsToDeploy;
                                if (type == OnlineCardType.Virus) --VirusCardsToDeploy;
                            }
                        }
                        if (LinkCardsToDeploy == 0 && VirusCardsToDeploy > 0)
                            CurrentDeploymentType = OnlineCardType.Virus;
                        else if (LinkCardsToDeploy > 0 && VirusCardsToDeploy == 0)
                            CurrentDeploymentType = OnlineCardType.Link;
                        else if (LinkCardsToDeploy == 0 && VirusCardsToDeploy == 0)
                        {
                            _game.CurrentPlayer = 2;
                        }
                    }
                }
                #endregion
                #region PlayerTurns
                else if (_game.Phase == GamePhase.PlayerTurns)
                {
                    // This only applies actions for player 1
                    if (_game.CurrentPlayer != 1) return;
                    if (_currentlySelectedField == null)
                    {
                        if (field.Card != null && field.Card.Owner.PlayerNumber == _game.CurrentPlayer)
                        {
                            // Check if Boost should be placed
                            if (_isLineBoostP1Selected)
                            {
                                if (_game.ExecuteCommand(
                                    _game.CreateSetBoostCommand(field.Position, true)))
                                {
                                    ResetBlink();
                                    _isLineBoostP1Selected = false;
                                    _game.CurrentPlayer = 2;
                                }
                                return;
                            }

                            if (field.Card is FirewallCard) return; // Firewall can be removed through action menu

                            // Disable selection if a special card is selected
                            if (_isFirewallP1Selected || _isVirusCheckP1Selected) // Line boost was already checked
                                return;

                            _currentlySelectedField = field;
                            SetBlink(field.Position, !_isError404P1Selected);

                            if (_isError404P1Selected)
                            {
                                _currentlySelectedField.IsHighlighted = true;
                                return;
                            }

                            // Highlight all fields that card can be moved to
                            foreach (var f in _game.GetTargetFields(field.Field))
                            {
                                SetBlink(f.Position, true);
                            }
                            return;
                        } // field card != null && owner == current
                        else if (field.Card != null && field.Card.Owner.PlayerNumber == 2 && field.Card is OnlineCard)
                        {
                            if (_isVirusCheckP1Selected)
                            {
                                if (_game.ExecuteCommand(_game.CreateSetVirusCheckCommand(field.Position)))
                                {
                                    _isVirusCheckP1Selected = false;
                                    ResetBlink();
                                    _game.CurrentPlayer = 2;
                                    ++_VirusCheckP1Count;
                                    OnPropertyChanged("IsVirusCheckVisible");
                                    return;
                                }
                            }
                            return;
                        }
                        else if (_isFirewallP1Selected && field.Card == null && field.Type == BoardFieldType.Main)
                        {
                            // Place firewall
                            if (!_game.ExecuteCommand(_game.CreateSetFirewallCommand(field.Position, true)))
                            {
                                return;
                            }
                            _isFirewallP1Selected = false;
                            ResetBlink();
                            _game.CurrentPlayer = 2;
                            return;
                        }
                        else if (field.Position.Y == 10)
                        {
                            // Extension fields
                            if (field.Position.X == 0) // Boost P1
                            {
                                // Line Boost field clicked
                                // Check if a card already has a line boost
                                var boostedCard = Game.Board.OnlineCards.FirstOrDefault(card => card.HasBoost && card.Owner.PlayerNumber == _game.CurrentPlayer);
                                if (boostedCard != null)
                                {
                                    // TODO: Signal Main Window to show popup that asks player to remove boost
                                    if (_game.ExecuteCommand(
                                    _game.CreateSetBoostCommand(boostedCard.Location.Position, false)))
                                    {
                                        _game.CurrentPlayer = 2;
                                        _isLineBoostP1Selected = false;
                                    }
                                    return;
                                }
                                // No boosted card
                                if (_isLineBoostP1Selected)
                                {
                                    ResetBlink();
                                    _isLineBoostP1Selected = false;
                                    return;
                                }
                                else
                                {
                                    SetBlink(field.Position, true);
                                    _isLineBoostP1Selected = true;
                                    // Let all Player cards on the field blink
                                    var allPlayerCardsOnField =
                                        Game.Board.OnlineCards.FindAll(card =>
                                        card.Owner.PlayerNumber == _game.CurrentPlayer &&
                                        card.Location.Position.Y < 8);
                                    foreach (var card in allPlayerCardsOnField)
                                    {
                                        SetBlink(card.Location.Position, true);
                                    }
                                    return;
                                }
                            }
                            else if (field.Position.X == 1) // Firewall P1
                            {
                                if (_isFirewallP1Selected)
                                {
                                    ResetBlink();
                                    _isFirewallP1Selected = false;
                                    return;
                                }
                                else
                                {
                                    // Check if firewall was already placed, if yes, remove it
                                    var fw = _game.Board.GetFirewall(_game.CurrentPlayer);
                                    if (fw.Location != null)
                                    {
                                        fw.Location.Card = null;
                                        fw.Location = null;
                                        _game.CurrentPlayer = 2;
                                        return;
                                    }
                                    SetBlink(field.Position, true);
                                    _isFirewallP1Selected = true;
                                    // Let all empty fields on main area blink
                                    for (int x = 0; x <= 7; ++x)
                                    {
                                        for (int y = 0; y <= 7; ++y)
                                        {
                                            if (_game.Board.Fields[x, y].Card == null && _game.Board.Fields[x, y].Type == BoardFieldType.Main)
                                                SetBlink(_game.Board.Fields[x, y].Position, true);
                                        }
                                    }
                                }
                            }
                            else if (field.Position.X == 2) // Virus Check P1
                            {
                                if (_isVirusCheckP1Selected)
                                {
                                    ResetBlink();
                                    _isVirusCheckP1Selected = false;
                                    return;
                                }
                                else
                                {
                                    // Card can only be used once
                                    if (_VirusCheckP1Count > 0)
                                        return;
                                    // Blink all opponents cards
                                    SetBlink(field.Position, true);
                                    _isVirusCheckP1Selected = true;
                                    for (int x = 0; x <= 7; ++x)
                                    {
                                        for (int y = 0; y <= 7; ++y)
                                        {
                                            if (_game.Board.Fields[x, y].Card != null && _game.Board.Fields[x, y].Card.Owner.PlayerNumber == 2)
                                                SetBlink(_game.Board.Fields[x, y].Position, true);
                                        }
                                    }
                                }
                                return;
                            }
                            else if (field.Position.X == 3) // Error 404 P1
                            {
                                if (_isError404P1Selected)
                                {                                    
                                    ResetBlink();
                                    _isError404P1Selected = false;
                                    return;
                                }
                                else
                                {
                                    // Card can only be used once
                                    if (_Error404P1Count > 0)
                                        return;
                                    // Blink all own cards
                                    SetBlink(field.Position, true);
                                    _isError404P1Selected = true;
                                    for (int x = 0; x <= 7; ++x)
                                    {
                                        for (int y = 0; y <= 7; ++y)
                                        {
                                            if (_game.Board.Fields[x, y].Card != null && _game.Board.Fields[x, y].Card.Owner.PlayerNumber == 1)
                                                SetBlink(_game.Board.Fields[x, y].Position, true);
                                        }
                                    }
                                }
                                return;
                            }
                            else if (field.Position.X == 4) // P1 Server Area
                            {
                                return; // UI is only used by P1 and P1 cannot enter his own server area
                            }
                            else if (field.Position.X == 5) // P2 Server Area
                            {
                                return; // This is already handled when a card was selected.
                            }
                        } // field.Position.Y == 10
                    } // _currentlySelectedField == null
                    // Currently selected field is not null:
                    else if (_currentlySelectedField == field)
                    {
                        _currentlySelectedField = null;
                        if (_isError404P1Selected && field.IsHighlighted)
                        {
                            field.IsHighlighted = false;
                            SetBlink(field.Position, true);
                        }
                        else 
                            ResetBlink();
                    }
                    else
                    {
                        if (_isError404P1Selected)
                        {
                            if (field.Position.Y == 10 && field.Position.X == 3)
                            {
                                if (_currentlySelectedField != null)
                                {
                                    _currentlySelectedField.IsHighlighted = false;
                                    _currentlySelectedField = null;
                                    ResetBlink();
                                    _isError404P1Selected = false;
                                    return;
                                }
                            }
                            else if (field.Card != null && field.Card.Owner.PlayerNumber == 1 /*_game.CurrentPlayer*/)
                            {
                                // Second card was chosen, do the switch
                                _currentlySelectedField.IsHighlighted = false;
                                ResetBlink();
                                _isError404P1Selected = false;
                                _currentlySelectedField = null;
                                // Only game is allowed to set cards
                                if (_game.ExecuteCommand(_game.CreateUseError404Command(field.Card.Location.Position, _currentlySelectedField.Card.Location.Position, true))) // TODO
                                {
                                    ++_Error404P1Count;
                                    OnPropertyChanged("IsError404P1Visible");
                                    _game.CurrentPlayer = 2;
                                }
                                return;
                            }
                            // Ignore all other fields
                            return;
                        }

                        // Move currently selected card if possible
                        var success = _game.ExecuteCommand(_game.CreateMoveCommand(
                            _currentlySelectedField.Position, field.Position));
                        ResetBlink();
                        _currentlySelectedField = null;
                        if (success)
                            _game.CurrentPlayer = 2;
                    }
                }
                #endregion
            }
            finally
            {
                OnPropertyChanged("IsDeployingLinkCard");
                OnPropertyChanged("IsDeployingVirusCard");
            }
        }

        public string PlayerWinMessage
        {
            get
            {
                if (_game.WinningPlayer < 1 || _game.WinningPlayer > 2)
                    return "???";

                var playerName = _game.Players[_game.WinningPlayer - 1].Name;
                if (playerName.Length > 160) playerName = playerName.Substring(0, 160);
                return playerName + " WIN";
            }
        }

        public bool IsGameOverPopupVisible
        {
            get { return _game.Phase == GamePhase.GameOver; }
        }

        public bool IsWaitingForPlayer2 // TODO: Animate Opacity via storyboard and double animation
        {
            get { return _game.CurrentPlayer == 2 && _game.Phase == GamePhase.Deployment; } // TODO: Also show in Turn phase when playing online
        }

        public bool IsNewGamePopupVisible
        {
            get { return !_designerMode && _game.Phase == GamePhase.Init; }
        }

        public bool IsDeploymentPopupVisible
        {
            get { return !_designerMode && _game.Phase == GamePhase.Deployment && _game.CurrentPlayer == 1; }
        }

        public RelayCommand NewGamePopupAccessBattleCommand
        {
            get { return new RelayCommand(
                NewGamePopupAccessBattleCommandExecute,
                CanNewGamePopupAccessBattleCommandExecute); }
        }

        public void NewGamePopupAccessBattleCommandExecute(object obj)
        {
            _game.Phase = GamePhase.Deployment;
        }

        public bool CanNewGamePopupAccessBattleCommandExecute(object obj)
        {
            return !string.IsNullOrEmpty(_game.Players[0].Name);
        }

    }
}
