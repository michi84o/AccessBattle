using AccessBattle.Networking.Packets;
using AccessBattle.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AccessBattle.Wpf.ViewModel
{
    // TODO: Filter for Game list

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
        }

        public ObservableCollection<GameInfo> Games { get; private set; }

        GameInfo _selectedGame;
        public GameInfo SelectedGame
        {
            get { return _selectedGame; }
            set { SetProp(ref _selectedGame, value); }
        }

        bool _isConnecting;
        public bool IsConnecting
        {
            get { return _isConnecting; }
            set
            {
                if (SetProp(ref _isConnecting, value))
                    OnPropertyChanged(nameof(CanConnect));
            }
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
            get { return SettingsValid && ParentViewModel.NetworkClient.IsConnected == false && !IsConnecting; }
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

        #region Commands

        public ICommand ConnectToServerCommand
        {
            get
            {
                return new RelayCommand(async o =>
                {
                    IsConnecting = true;
                    bool result = await ParentViewModel.NetworkClient.Connect(IpAddress, Port);
                    IsConnecting = false;
                    CommandManager.InvalidateRequerySuggested();
                }, o => { return CanConnect; });
            }
        }

        public ICommand CreateNetworkGameCommand
        {
            get
            {
                return new RelayCommand(o =>
                {

                }, o =>
                {
                    return
                    ParentViewModel.NetworkClient.IsLoggedIn == true &&
                    !IsConnecting;
                });
            }
        }

        public ICommand JoinNetworkGameCommand
        {
            get
            {
                return new RelayCommand(o =>
                {

                }, o =>
                {
                    return
                        ParentViewModel.NetworkClient.IsLoggedIn == true &&
                        ParentViewModel.NetworkClient.IsJoined == false &&
                        !IsConnecting;
                });
            }
        }

        public ICommand ShowWelcomeMenuCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    ParentViewModel.CurrentMenu = MenuType.Welcome;
                }, o => !IsConnecting);
            }
        }

        #endregion
    }
}
