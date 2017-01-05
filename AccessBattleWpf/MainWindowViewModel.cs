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

        bool _designerMode;

        public event EventHandler<BlinkChangedEventArgs> BlinkStateChanged;

        public MainWindowViewModel()
        {
            _game = new Game();
            _game.PropertyChanged += Game_PropertyChanged;
            _designerMode = WpfHelper.IsInDesignerMode;
        }

        void Game_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Phase")
            {
                CurrentDeploymentType = OnlineCardType.Unknown;
                ResetBlink();
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

                        break;
                }
                OnPropertyChanged("IsNewGamePopupVisible");
                OnPropertyChanged("IsDeploymentPopupVisible");
            }
        }

        public bool GetBlink(Vector position)
        {
            if (position.X > 7 || position.Y > 9) return false;

            var num = (byte)(1 << position.X);
            return (_blinkMap[position.Y] & num) > 0;
        }

        void ResetBlink()
        {
            for (int i = 0; i < 10; ++i)
            {
                _blinkMap[i] = 0;
            }
            var handler = BlinkStateChanged;
            if (handler != null) handler(this, new BlinkChangedEventArgs(new Vector(0, 0),true));
        }

        bool SetBlink(Vector position, bool isBlinking)
        {
            // horizontal: always 8 fields -> 1 byte
            // vertical: 10 fields: -> array index
            if (position.X > 7 || position.Y > 9) return false;
            if (GetBlink(position) == isBlinking) return true; // Nothing to do

            // Some bit magic. From good old atmega8 µC programming
                var num = (byte)(1 << position.X);
            if (isBlinking) _blinkMap[position.Y] |= num;
            else _blinkMap[position.Y] &= (byte)~num;

            var handler = BlinkStateChanged;
            if (handler != null) handler(this, new BlinkChangedEventArgs(position));
            return true;
        }

        byte[] _blinkMap = new byte[10];
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

        public void FieldClicked(BoardField field)
        {
            try
            {
                Trace.WriteLine("Field clicked: " + field.Position.X + "," + field.Position.Y);
                if (_game.Phase == GamePhase.Deployment)
                {
                    if (_game.Board.GetPlayerDeploymentFields(1).Contains(field))
                    {
                        var cardsOnStack = _game.Board.GetPlayerStackFields(1).FindAll(
                                o => o.Card != null && o.Card is OnlineCard).ToList();
                        BoardField fieldToMove = null;
                        OnlineCardType type = OnlineCardType.Unknown;
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
                        if (LinkCardsToDeploy == 0 && VirusCardsToDeploy == 0) _game.Phase = GamePhase.PlayerTurns;
                    }
                }
            }
            finally
            {
                OnPropertyChanged("IsDeployingLinkCard");
                OnPropertyChanged("IsDeployingVirusCard");
            }
        }

        public bool IsNewGamePopupVisible
        {
            get { return !_designerMode && _game.Phase == GamePhase.Init; }
        }

        public bool IsDeploymentPopupVisible
        {
            get { return !_designerMode && _game.Phase == GamePhase.Deployment; }
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
