using AccessBattle.Wpf.Interfaces;
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

// TODO: Add optional Textchat

namespace AccessBattle.Wpf.ViewModel
{
    public class MainWindowViewModel : PropChangeNotifier, IMenuHolder
    {
        GameViewModel _game;
        public GameViewModel Game => _game;

        public bool IsPlayerHost
        {
            get { return _game.IsPlayerHost; }
            set { _game.IsPlayerHost = value; } // Prop change triggered by model and forwarded below
        }

        /// <summary>
        /// Used to lock the UI while waiting from answer of the server.
        /// Should only be accessed from UI thread.
        /// </summary>
        public bool IsBusy { get; set; }

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
                        _game.Phase = GamePhase.PlayerJoining;
                    else if (
                        _currentMenu == MenuType.NetworkGame ||
                        _currentMenu == MenuType.Welcome)
                        _game.Phase = GamePhase.WaitingForPlayers;
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
        DeploymentViewModel _deploymentVm;

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
                    case MenuType.Deployment: return _deploymentVm;
                    case MenuType.Welcome:
                    default: return _welcomeVm;
                }
            }
        }

        #region Board Field Visual States

        public BoardFieldViewModel[,] BoardFields => _game.BoardFields;

        /// <summary>
        /// This is a one-dimensional list that can be used in XAML code.
        /// It maps the internal two-dimensinal list and contains the items row-wise,
        /// starting from row (y=)0.
        /// item 0 is field [0,0], item 1 is field [1,0], item 8 is field [1,0].
        /// The length of this list is 88.
        /// </summary>
        public List<BoardFieldViewModel> BoardFieldList => _game.BoardFieldList;

        #endregion

        public MainWindowViewModel()
        {
            _game = new GameViewModel(this);
            _game.PropertyChanged += _model_PropertyChanged;

            // Menu view models
            _welcomeVm = new WelcomeMenuViewModel(this);
            _networkGameVm = new NetworkGameMenuViewModel(this);
            _waitForJoinVm = new WaitForJoinMenuViewModel(this);
            _acceptJoinVm = new AcceptJoinMenuViewModel(this);
            _waitForAccept = new WaitForAcceptMenuViewModel(this);
            _deploymentVm = new DeploymentViewModel(this);

            CurrentMenu = MenuType.Welcome;

            for (int y = 0; y < 11; ++y)
                for (int x = 0; x < 8; ++x)
                {
                    BoardFields[x, y] = new BoardFieldViewModel();
                    BoardFieldList.Add(BoardFields[x, y]);
                }

            // Server area p1 is at index 83, p2 at 84

            #region Set default visual states

            BoardFields[3, 0].DefaultVisualState = BoardFieldVisualState.Exit;
            BoardFields[4, 0].DefaultVisualState = BoardFieldVisualState.Exit;

            BoardFields[3, 7].DefaultVisualState = BoardFieldVisualState.Exit;
            BoardFields[4, 7].DefaultVisualState = BoardFieldVisualState.Exit;

            // Stack
            BoardFields[0, 8].DefaultVisualState = BoardFieldVisualState.Link; // 64
            BoardFields[1, 8].DefaultVisualState = BoardFieldVisualState.Link;
            BoardFields[2, 8].DefaultVisualState = BoardFieldVisualState.Link;
            BoardFields[3, 8].DefaultVisualState = BoardFieldVisualState.Link;
            BoardFields[4, 8].DefaultVisualState = BoardFieldVisualState.Virus; // 68
            BoardFields[5, 8].DefaultVisualState = BoardFieldVisualState.Virus;
            BoardFields[6, 8].DefaultVisualState = BoardFieldVisualState.Virus;
            BoardFields[7, 8].DefaultVisualState = BoardFieldVisualState.Virus;
            BoardFields[0, 9].DefaultVisualState = BoardFieldVisualState.Link; // 72
            BoardFields[1, 9].DefaultVisualState = BoardFieldVisualState.Link;
            BoardFields[2, 9].DefaultVisualState = BoardFieldVisualState.Link;
            BoardFields[3, 9].DefaultVisualState = BoardFieldVisualState.Link;
            BoardFields[4, 9].DefaultVisualState = BoardFieldVisualState.Virus; // 76
            BoardFields[5, 9].DefaultVisualState = BoardFieldVisualState.Virus;
            BoardFields[6, 9].DefaultVisualState = BoardFieldVisualState.Virus;
            BoardFields[7, 9].DefaultVisualState = BoardFieldVisualState.Virus; // 79

            #endregion

            for (int y = 0; y < 11; ++y)
                for (int x = 0; x < 8; ++x)
                    BoardFields[x, y].RegisterBoardField(_game.Board[x, y]);

            // Animates two double values for flashing and alternating card states (Boost)
            UiGlobals.Instance.StartFlashing();
            UiGlobals.Instance.StartMultiOverlayFlashing();
        }

        void _model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_game.IsPlayerHost))
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
                       (IsPlayerHost && _game.Phase == GamePhase.Player1Turn) ||
                      (!IsPlayerHost && _game.Phase == GamePhase.Player2Turn);
                });
            }
        }

        public ICommand BoardFieldClickedCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    var indexStr = o as string;
                    if (string.IsNullOrEmpty(indexStr)) return;
                    int index;
                    if (!Int32.TryParse(indexStr, out index)) return;
                    _game.HandleFieldSelection(index);
                }, o =>
                {
                    return true; // Check is done in execution for performance reasons.

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
