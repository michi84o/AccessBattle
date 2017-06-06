using AccessBattle.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AccessBattle.Wpf.ViewModel
{
    public class NetworkGameMenuViewModel : MenuViewModelBase
    {
        NetworkSettingsMenuViewModel _settings;

        public NetworkGameMenuViewModel(
            IMenuHolder parent, NetworkSettingsMenuViewModel settings) : base(parent)
        {
            _settings = settings;
        }

        public ICommand CreateNetworkGameCommand
        {
            get
            {
                return new RelayCommand(o =>
                {

                }, o => { return _settings.SettingsValid; });
            }
        }

        public ICommand JoinNetworkGameCommand
        {
            get
            {
                return new RelayCommand(o =>
                {

                }, o => { return _settings.SettingsValid; });
            }
        }

        public ICommand NetworkSettingsCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    ParentViewModel.CurrentMenu = MenuType.NetworkSettings;
                }, o => { return true; });
            }
        }

        public ICommand ShowWelcomeMenuCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    ParentViewModel.CurrentMenu = MenuType.Welcome;
                });
            }
        }
    }
}
