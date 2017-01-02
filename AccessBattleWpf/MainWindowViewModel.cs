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
    public class MainWindowViewModel : PropChangeNotifier
    {
        Game _game;
        public Game Game { get {return _game;}}

        bool _designerMode;

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
                switch (_game.Phase)
                {
                    case GamePhase.Init:
                        break;
                    case GamePhase.Deployment:
                        break;
                }
                OnPropertyChanged("IsNewGamePopupVisible");
                OnPropertyChanged("IsDeploymentPopupVisible");
            }
        }

        List<BoardField> Player1DeploymentFields;
        public void FieldClicked(BoardField field)
        {
            if (_game.Phase == GamePhase.Deployment)
            {
                if (field.IsHighlighted)
                {
                    if (field.Card != null)
                    {
                        // TODO: turn back card
                        return;
                    }
                    var nextDepField = _game.Board.Player1StackFields.FirstOrDefault(o => o.Card != null);
                    if (nextDepField == null)
                    {
                        Trace.WriteLine("MainWindowViewModel FieldClicked nextDepField is null !!!!!!!!!!");
                        _game.Phase = GamePhase.Player1Turn; // TODO: Random
                        return;
                    }
                    field.Card = nextDepField.Card;
                    nextDepField.Card = null;
                }
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
            return !string.IsNullOrEmpty(_game.Player1.Name);
        }

    }
}
