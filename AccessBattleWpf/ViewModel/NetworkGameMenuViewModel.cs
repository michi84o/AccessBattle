using AccessBattle.Networking;
using AccessBattle.Networking.Packets;
using AccessBattle.Wpf.Extensions;
using AccessBattle.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace AccessBattle.Wpf.ViewModel
{
    public class NetworkGameMenuViewModel : MenuViewModelBase
    {
        public NetworkGameMenuViewModel(
            IMenuHolder parent) : base(parent)
        {
            Games = new ObservableCollection<GameInfo>
            {
                new GameInfo { UID= 2147483647, Name="MyGame", Player1="Player1" },
                new GameInfo { UID= 123, Name="Awesome Game", Player1="cvsgsagf 12131fs" }
            };

            _gamesView = new CollectionViewSource
            {
                Source = Games
            };
            _gamesView.Filter += GamesView_Filter;

            WeakEventManager<NetworkGameClient, ServerInfoEventArgs>.AddHandler(
                parent.NetworkClient, nameof(parent.NetworkClient.ServerInfoReceived), ServerInfoReceivedHandler);

            WeakEventManager<NetworkGameClient, GameJoinRequestedEventArgs>.AddHandler(
                parent.NetworkClient, nameof(parent.NetworkClient.GameJoinRequested), JoinRequestedHandler);

            _gameListUpdateTimer = new System.Threading.Timer(new System.Threading.TimerCallback(UpdateGameList));
        }

        public override void Activate()
        {
            var client = ParentViewModel.NetworkClient;
            if (client.IsConnected == true && client.IsLoggedIn == true
                && !(client.IsJoined == true))
            {
                _gameListUpdateTimer.Change(0, System.Threading.Timeout.Infinite);
            }
        }

        public override void Suspend()
        {
            _gameListUpdateTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }

        void JoinRequestedHandler(object sender, GameJoinRequestedEventArgs args)
        {
            //args.Message.
        }

        void UpdateGameList(object state)
        {
            var t = (System.Threading.Timer)state;

            // disable timer
            t.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                (Action)(async () =>
                {
                    var client = ParentViewModel.NetworkClient;
                    if (client.IsConnected == true && client.IsLoggedIn == true
                        && !(client.IsJoined==true))
                    {
                        var list = await ParentViewModel.NetworkClient.RequestGameList();

                        // Remove games that don't exist anymore
                        var games = Games.ToList();
                        foreach (var game in games)
                        {
                            if (!list.Exists(o => o.UID == game.UID))
                                Games.Remove(game);
                        }

                        games = Games.ToList();
                        // Remove games from list that are already in Games
                        list.RemoveAll(o => games.Exists(oo => oo.UID == o.UID));

                        // Add the remaining games
                        foreach (var game in list)
                            Games.Add(game);

                        _gamesView.View.Refresh();

                        // re-enable timer
                        t.Change(5000, System.Threading.Timeout.Infinite);
                    }
                }));
        }

        void ServerInfoReceivedHandler(object sender, ServerInfoEventArgs args)
        {
            RequiresPassword = args.Info.RequiresLogin;
        }

        private void GamesView_Filter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrEmpty(FilterText))
            {
                e.Accepted = true;
                return;
            }

            var info = e.Item as GameInfo;
            if (info.Name.ToUpper().Contains(FilterText.ToUpper()))
                e.Accepted = true;
            else
                e.Accepted = false;
        }

        System.Threading.Timer _gameListUpdateTimer;

        CollectionViewSource _gamesView;
        public ICollectionView GamesView
        {
            get
            {
                return _gamesView.View;
            }
        }

        public ObservableCollection<GameInfo> Games { get; private set; }

        GameInfo _selectedGame;
        public GameInfo SelectedGame
        {
            get { return _selectedGame; }
            set { SetProp(ref _selectedGame, value); }
        }

        public bool CanChangeConnection
        {
            get { return !IsLoggingIn && !IsConnecting; }
        }

        bool _isConnecting;
        public bool IsConnecting
        {
            get { return _isConnecting; }
            set
            {
                if (SetProp(ref _isConnecting, value))
                {
                    OnPropertyChanged(nameof(CanConnect));
                    OnPropertyChanged(nameof(CanChangeConnection));
                }
            }
        }

        bool _isLoggingIn;
        public bool IsLoggingIn
        {
            get { return _isLoggingIn; }
            set
            {
                if (SetProp(ref _isLoggingIn, value))
                    OnPropertyChanged(nameof(CanChangeConnection));
            }
        }

        bool _requiresPassword;
        public bool RequiresPassword
        {
            get { return _requiresPassword; }
            set { SetProp(ref _requiresPassword, value); }
        }

        string _loginName;
        public string LoginName
        {
            get { return _loginName; }
            set { SetProp(ref _loginName, value); }
        }

        bool _isCreatingGame;
        public bool IsCreatingGame
        {
            get { return _isCreatingGame; }
            set { SetProp(ref _isCreatingGame, value); }
        }

        bool _isJoiningGame;
        public bool IsJoiningGame
        {
            get { return _isJoiningGame; }
            set { SetProp(ref _isJoiningGame, value); }
        }

        SecureString _loginPassword;
        public SecureString LoginPassword
        {
            get { return _loginPassword; }
            set { SetProp(ref _loginPassword, value); }
        }

        public bool SettingsValid
        {
            get
            {
                System.Net.IPAddress a;
                return System.Net.IPAddress.TryParse(IpAddress, out a) && Port > 1023;
            }
        }

        public bool CanConnect
        {
            get { return SettingsValid && ParentViewModel.NetworkClient.IsConnected == false && !IsConnecting && !IsLoggingIn; }
        }

        string _ipAddress = "127.0.0.1";
        public string IpAddress
        {
            get { return _ipAddress; }
            set
            {
                if (SetProp(ref _ipAddress, value))
                {
                    ParentViewModel.NetworkClient.Disconnect();
                    Games.Clear();
                    OnPropertyChanged(nameof(SettingsValid));
                    OnPropertyChanged(nameof(CanConnect));
                }
            }
        }

        ushort _port = 3221;
        public ushort Port
        {
            get { return _port; }
            set
            {
                if (SetProp(ref _port, value))
                {
                    ParentViewModel.NetworkClient.Disconnect();
                    Games.Clear();
                    OnPropertyChanged(nameof(SettingsValid));
                    OnPropertyChanged(nameof(CanConnect));
                }
            }
        }

        string _filterText;
        public string FilterText
        {
            get { return _filterText; }
            set
            {
                if (SetProp(ref _filterText, value))
                    _gamesView.View.Refresh();
            }
        }

        string _newGameText;
        public string NewGameText
        {
            get { return _newGameText; }
            set { SetProp(ref _newGameText, value); }
        }

        #region Commands

#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
        public ICommand ConnectToServerCommand =>
            new RelayCommand(async o =>
            {
                IsConnecting = true;
                var result = await ParentViewModel.NetworkClient.Connect(IpAddress, Port);
                IsConnecting = false;
                IsLoggingIn = true;
                CommandManager.InvalidateRequerySuggested();
            }, o => { return CanConnect; });
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void

        public ICommand CreateNetworkGameCommand
        {
            get
            {
                return new RelayCommand(async o =>
                {
                    IsCreatingGame = true;
                    await Task.Delay(1000);
                    var result = await ParentViewModel.NetworkClient.CreateGame(NewGameText);
                    if (result != null && result.Name == NewGameText)
                    {
                        ParentViewModel.CurrentMenu = MenuType.WaitForOpponent;
                        // TODO: Should be cancelled after 60s if no one joines
                        // TODO: The timer for polling the game list is still running
                    }
                    IsCreatingGame = false;
                }, o =>
                {
                    return
                    !string.IsNullOrEmpty(NewGameText) &&
                    ParentViewModel.NetworkClient.IsLoggedIn == true &&
                    !IsConnecting && !IsLoggingIn && !IsCreatingGame && !IsJoiningGame;
                });
            }
        }

        public ICommand JoinNetworkGameCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    var uid = SelectedGame?.UID;
                    if (uid == null) return;
                    ParentViewModel.NetworkClient.RequestJoinGame(uid.Value);
                }, o =>
                {
                    return
                        ParentViewModel.NetworkClient.IsLoggedIn == true &&
                        ParentViewModel.NetworkClient.IsJoined == false &&
                        !IsConnecting && !IsLoggingIn && SelectedGame != null && !IsCreatingGame && !IsJoiningGame;
                });
            }
        }

        public ICommand ShowWelcomeMenuCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    ParentViewModel.NetworkClient.Disconnect();
                    ParentViewModel.CurrentMenu = MenuType.Welcome;
                }, o => !IsConnecting && !IsLoggingIn && !IsCreatingGame && !IsJoiningGame);
            }
        }

        bool _sendingLogin;
        public ICommand LoginCommand
        {
            get
            {
                return new RelayCommand(async o =>
                {
                    _sendingLogin = true;
                    CommandManager.InvalidateRequerySuggested();
                    var result = await ParentViewModel.NetworkClient.Login(LoginName, LoginPassword.ConvertToUnsecureString());
                    _sendingLogin = false;
                    if (result)
                    {
                        IsLoggingIn = false;
                        _gameListUpdateTimer.Change(0, System.Threading.Timeout.Infinite);
                    }
                    CommandManager.InvalidateRequerySuggested();
                }, o => !string.IsNullOrEmpty(LoginName) && IsLoggingIn && ParentViewModel.NetworkClient.IsConnected == true && !_sendingLogin);
            }
        }

        public ICommand CancelLoginCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    IsLoggingIn = false;
                    ParentViewModel.NetworkClient.Disconnect();
                    OnPropertyChanged(nameof(CanConnect));
                    CommandManager.InvalidateRequerySuggested();
                }, o => !_sendingLogin);
            }
        }

        #endregion
    }
}
