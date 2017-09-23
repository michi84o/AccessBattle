using AccessBattle.Networking;
using AccessBattle.Networking.Packets;
using AccessBattle.Wpf.Extensions;
using AccessBattle.Wpf.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace AccessBattle.Wpf.ViewModel
{
    public class NetworkGameMenuViewModel : MenuViewModelBase, INotifyDataErrorInfo
    {
        public NetworkGameMenuViewModel(
            IMenuHolder parent) : base(parent)
        {
            Games = new ObservableCollection<GameInfo>();

            _gamesView = new CollectionViewSource
            {
                Source = Games
            };
            _gamesView.Filter += GamesView_Filter;

            WeakEventManager<NetworkGameClient, ServerInfoEventArgs>.AddHandler(
                parent.Game.Client, nameof(parent.Game.Client.ServerInfoReceived), ServerInfoReceivedHandler);

            WeakEventManager<NetworkGameClient, GameJoinRequestedEventArgs>.AddHandler(
                parent.Game.Client, nameof(parent.Game.Client.GameJoinRequested), JoinRequestedHandler);

            WeakEventManager<NetworkGameClient, GameJoinRequestedEventArgs>.AddHandler(
                parent.Game.Client, nameof(parent.Game.Client.ConfirmJoinCalled), ConfirmJoinCalledHandler);


            _gameListUpdateTimer = new System.Threading.Timer(new System.Threading.TimerCallback(UpdateGameList));
        }

        public override void Activate()
        {
            var client = ParentViewModel.Game.Client;
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

        void ConfirmJoinCalledHandler(object sender, GameJoinRequestedEventArgs args)
        {
            if (args.Message.UID != ParentViewModel.JoiningGame?.UID) return;

            // Player pressed the cancel button while waiting for accept
            if (args.Message.Request == JoinRequestType.Decline
                && ParentViewModel.CurrentMenu == MenuType.WaitForAccept)
            {
                ParentViewModel.JoiningGame = null;
                ParentViewModel.CurrentMenu = MenuType.NetworkGame;
            }
        }

        // TODO: part of this could be moved to NetworkGameClient. Setting UID for example
        void JoinRequestedHandler(object sender, GameJoinRequestedEventArgs args)
        {
            if (args.Message.UID != ParentViewModel.JoiningGame?.UID) return;

            ParentViewModel.JoiningGame = null;

            if (args.Message.Request == JoinRequestType.Accept)
            {
                ParentViewModel.Game.Client.ConfirmJoin(args.Message.UID, true);

                // TODO: Work with SynchronizationContext
                Application.Current.Dispatcher.Invoke(() => { ParentViewModel.Game.IsPlayerHost = false; });

                ParentViewModel.Game.UID = args.Message.UID;
                ParentViewModel.CurrentMenu = MenuType.Deployment;
                //MessageBox.Show("TODO: INIT GAME (p2)");
            }
            else if (args.Message.Request == JoinRequestType.Decline) // Declined
            {
                ParentViewModel.JoiningGame = null;
                ParentViewModel.CurrentMenu = MenuType.NetworkGame;
            }
            else
            {
                // TODO
                MessageBox.Show("TODO: UNHANDLED JOIN REQUEST");
            }
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
                    var client = ParentViewModel.Game.Client;
                    if (client.IsConnected == true && client.IsLoggedIn == true
                        && !(client.IsJoined == true))
                    {
                        var list = await ParentViewModel.Game.Client.RequestGameList();

                        // If server does not answer, list will be null!
                        if (list != null)
                        {
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
                        }

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
        /// <summary>Selected game in game list.</summary>
        /// <remarks>Game gets removed from list if player tries to join!</remarks>
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
            set
            {
                if (SetProp(ref _requiresPassword, value))
                    Validate();
            }
        }

        string _loginName;
        public string LoginName
        {
            get { return _loginName; }
            set
            {
                if (SetProp(ref _loginName, value))
                    Validate();
            }
        }

        bool _isCreatingGame;
        public bool IsCreatingGame
        {
            get { return _isCreatingGame; }
            set { SetProp(ref _isCreatingGame, value); }
        }

        bool IsJoiningGame
        {
            get { return ParentViewModel.JoiningGame != null; }
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
            get { return SettingsValid && ParentViewModel.Game.Client.IsConnected == false && !IsConnecting && !IsLoggingIn; }
        }

        string _ipAddress = "127.0.0.1";
        public string IpAddress
        {
            get { return _ipAddress; }
            set
            {
                if (SetProp(ref _ipAddress, value))
                {
                    ParentViewModel.Game.Client.Disconnect();
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
                    ParentViewModel.Game.Client.Disconnect();
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
                var result = await ParentViewModel.Game.Client.Connect(IpAddress, Port);
                IsConnecting = false;
                IsLoggingIn = result;
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
                    var result = await ParentViewModel.Game.Client.CreateGame(NewGameText);
                    if (result != null && result.Name == NewGameText)
                    {
                        //ParentViewModel.Model.Client.RegisterGame(ParentViewModel.Game, result);
                        ParentViewModel.CurrentMenu = MenuType.WaitForJoin;
                        ParentViewModel.Game.IsPlayerHost = true;
                        ParentViewModel.Game.UID = result.UID;
                        // TODO: Should be cancelled after 60s if no one joines
                        // TODO: The timer for polling the game list is still running
                    }
                    IsCreatingGame = false;
                }, o =>
                {
                    return
                    !string.IsNullOrEmpty(NewGameText) &&
                    ParentViewModel.Game.Client.IsLoggedIn == true &&
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
                    ParentViewModel.JoiningGame = SelectedGame;
                    var uid = ParentViewModel.JoiningGame?.UID;
                    if (uid == null)
                    {
                        ParentViewModel.JoiningGame = null;
                        return;
                    }
                    ParentViewModel.CurrentMenu = MenuType.WaitForAccept;
                    ParentViewModel.Game.Client.RequestJoinGame(uid.Value);
                }, o =>
                {
                    return
                        ParentViewModel.Game.Client.IsLoggedIn == true &&
                        ParentViewModel.Game.Client.IsJoined == false &&
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
                    ParentViewModel.Game.Client.Disconnect();
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
                    var result = await ParentViewModel.Game.Client.Login(LoginName, LoginPassword.ConvertToUnsecureString());
                    _sendingLogin = false;
                    if (result)
                    {
                        IsLoggingIn = false;
                        _gameListUpdateTimer.Change(0, System.Threading.Timeout.Infinite);
                    }
                    CommandManager.InvalidateRequerySuggested();
                }, o => !string.IsNullOrEmpty(LoginName) && IsLoggingIn && ParentViewModel.Game.Client.IsConnected == true && !_sendingLogin && !HasPropError(nameof(LoginName)) && !HasPropError(nameof(LoginPassword)));
            }
        }

        public ICommand CancelLoginCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    IsLoggingIn = false;
                    ParentViewModel.Game.Client.Disconnect();
                    OnPropertyChanged(nameof(CanConnect));
                    CommandManager.InvalidateRequerySuggested();
                }, o => !_sendingLogin);
            }
        }


        #endregion

        #region Error Validation

        void Validate([CallerMemberName] string propertyName = null)
        {
            List<string> errors;
            if (!_errors.TryGetValue(propertyName, out errors))
            {
                errors = new List<string>();
                _errors[propertyName] = errors;
            }

            StringBuilder builder;

            switch (propertyName)
            {
                case nameof(LoginName):
                    errors.Clear();
                    if (LoginName?.Length > 0 && !Regex.IsMatch(LoginName, @"^[\x20-\x7E]+$"))
                        errors.Add("Invalid symbols");
                    if (LoginName?.Length > 32)
                        errors.Add("Max length is 32");
                    ErrorText = "";
                    builder = new StringBuilder();
                    builder.Append(ErrorText);
                    foreach (var str in errors)
                        builder.Append(str + "\r\n");
                    ErrorText = builder.ToString().TrimEnd(new[] { '\r', '\n' });
                    break;
                case nameof(LoginPassword):
                    errors.Clear();
                    if (LoginPassword?.ConvertToUnsecureString()?.Length > 0 && !Regex.IsMatch(LoginPassword?.ConvertToUnsecureString() ?? "", @"^[\x20-\x7E]+$"))
                        errors.Add("Invalid symbols");
                    if (LoginPassword?.ConvertToUnsecureString()?.Length > 32)
                        errors.Add("Max length is 32");
                    ErrorText = "";
                    builder = new StringBuilder();
                    builder.Append(ErrorText);
                    foreach (var str in errors)
                        builder.Append(str + "\r\n");
                    ErrorText = builder.ToString().TrimEnd(new[] { '\r', '\n' });
                    break;
            }

            HasErrors = _errors.Any(o => o.Value?.Count > 0);
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        string _errorText;
        public string ErrorText
        {
            get => _errorText;
            set { SetProp(ref _errorText, value); }
        }

        bool _hasErrors;
        public bool HasErrors
        {
            get => _hasErrors;
            set { SetProp(ref _hasErrors, value); }
        }
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        Dictionary<string, List<string>> _errors = new Dictionary<string, List<string>>();
        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return new List<string>();
            List<string> errors;
            if (!_errors.TryGetValue(propertyName, out errors))
                return new List<string>();
            return errors;
        }

        bool HasPropError(string propertyName) => _errors.TryGetValue(propertyName, out List<string> errors) && errors?.Count > 0;

        #endregion

    }
}
