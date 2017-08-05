using AccessBattle.Wpf.Interfaces;
using AccessBattle.Wpf.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AccessBattle.Networking;
using AccessBattle.Networking.Packets;

namespace AccessBattle.Wpf.ViewModel
{
    public class MainWindowViewModel : PropChangeNotifier, IMenuHolder
    {
        GameModel _model = new GameModel();

        public GameModel Model => _model;

        //public NetworkGameClient NetworkClient => _model.Client;
        //public Game Game => _model.Game;

        public bool IsPlayerHost
        {
            get { return _model.IsPlayerHost; }
            set { _model.IsPlayerHost = value; } // Prop change triggered by model
        }

        MenuType _currentMenu;
        public MenuType CurrentMenu
        {
            get { return _currentMenu; }
            set
            {
                var lastVm = CurrentMenuViewModel;
                if (SetProp(ref _currentMenu, value))
                {
                    OnPropertyChanged(nameof(CurrentMenuViewModel));

                    if (_currentMenu == MenuType.WaitForAccept ||
                        _currentMenu == MenuType.WaitForJoin ||
                        _currentMenu == MenuType.AcceptJoin)
                        Model.Game.Phase = GamePhase.PlayerJoining;
                    else if (
                        _currentMenu == MenuType.NetworkGame ||
                        _currentMenu == MenuType.Welcome)
                        Model.Game.Phase = GamePhase.WaitingForPlayers;
                    lastVm?.Suspend();
                    CurrentMenuViewModel?.Activate();
                }
            }
        }

        // View models for menus:
        WelcomeMenuViewModel _welcomeVm;
        NetworkGameMenuViewModel _networkGameVm;
        WaitForJoinMenuViewModel _waitForJoinVm;
        AcceptJoinMenuViewModel _acceptJoinVm;
        WaitForAcceptMenuViewModel _waitForAccept;

        public MenuViewModelBase CurrentMenuViewModel
        {
            get
            {
                switch (_currentMenu)
                {
                    case MenuType.None: return null;
                    case MenuType.NetworkGame: return _networkGameVm;
                    case MenuType.WaitForJoin: return _waitForJoinVm;
                    case MenuType.AcceptJoin: return _acceptJoinVm;
                    case MenuType.WaitForAccept: return _waitForAccept;
                    case MenuType.Welcome:
                    default: return _welcomeVm;
                }
            }
        }

        #region Board Field Visual States

        BoardFieldViewModel[,] _boardFields = new BoardFieldViewModel[8, 11];

        /// <summary>
        /// This is a one-dimensional list that can be used in XAML code.
        /// It maps the internal two-dimensinal list and contains the items row-wise,
        /// starting from row (y=)0.
        /// item 0 is field [0,0], item 1 is field [1,0], item 8 is field [1,0].
        /// The length of this list is 88.
        /// </summary>
        public List<BoardFieldViewModel> BoardFieldList { get; private set; }

        #endregion

        public MainWindowViewModel()
        {
            _model.PropertyChanged += _model_PropertyChanged;

            // Menu view models
            _welcomeVm = new WelcomeMenuViewModel(this);
            _networkGameVm = new NetworkGameMenuViewModel(this);
            _waitForJoinVm = new WaitForJoinMenuViewModel(this);
            _acceptJoinVm = new AcceptJoinMenuViewModel(this);
            _waitForAccept = new WaitForAcceptMenuViewModel(this);

            CurrentMenu = MenuType.Welcome;

            BoardFieldList = new List<BoardFieldViewModel>();
            for (int y = 0; y < 11; ++y)
                for (int x = 0; x < 8; ++x)
                {
                    _boardFields[x, y] = new BoardFieldViewModel();
                    BoardFieldList.Add(_boardFields[x, y]);
                }

            _boardFields[3, 0].DefaultVisualState = BoardFieldVisualState.Exit;
            _boardFields[4, 0].DefaultVisualState = BoardFieldVisualState.Exit;

            _boardFields[3, 7].DefaultVisualState = BoardFieldVisualState.Exit;
            _boardFields[4, 7].DefaultVisualState = BoardFieldVisualState.Exit;


            for (int y = 0; y < 11; ++y)
                for (int x = 0; x < 8; ++x)
                    _boardFields[x, y].RegisterBoardField(_model.Game.Board[x, y]);
        }

        void _model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_model.IsPlayerHost))
            {
                OnPropertyChanged(nameof(IsPlayerHost));
            }
        }

        public ICommand ActionsCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                }, o =>
                {
                    return
                       (IsPlayerHost && _model.Game.Phase == GamePhase.Player1Turn) ||
                      (!IsPlayerHost && _model.Game.Phase == GamePhase.Player2Turn);
                });
            }
        }

        GameInfo _joiningGame = null;
        public GameInfo JoiningGame
        {
            get { return _joiningGame; }
            set { SetProp(ref _joiningGame, value); }
        }
    }
}
