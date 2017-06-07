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

namespace AccessBattle.Wpf.ViewModel
{
    public class MainWindowViewModel : PropChangeNotifier, IMenuHolder
    {
        GameModel _model;

        NetworkGameClient _networkClient = new NetworkGameClient();
        public NetworkGameClient NetworkClient
        {
            get { return _networkClient; }
        }

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
                if (SetProp(ref _currentMenu, value))
                    OnPropertyChanged(nameof(CurrentMenuViewModel));
            }
        }

        // View models for menus:
        WelcomeMenuViewModel _welcomeVm;
        NetworkGameMenuViewModel _networkGameVm;

        public PropChangeNotifier CurrentMenuViewModel
        {
            get
            {
                switch (_currentMenu)
                {
                    case MenuType.None:return null;
                    case MenuType.NetworkGame: return _networkGameVm;
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
            _model = new GameModel();
            _model.PropertyChanged += _model_PropertyChanged;

            // Menu view models
            _welcomeVm = new WelcomeMenuViewModel(this);
            _networkGameVm = new NetworkGameMenuViewModel(this);

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
                    return _model.Game.Phase == GamePhase.PlayerTurns;
                    // TODO: Also only if its current players turn
                });
            }
        }
    }
}
