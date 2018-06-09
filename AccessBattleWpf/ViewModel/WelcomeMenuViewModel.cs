using AccessBattle.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AccessBattle.Wpf.ViewModel
{
    public class WelcomeMenuViewModel : MenuViewModelBase
    {
        public WelcomeMenuViewModel(IMenuHolder parent) : base(parent) { }

        public override void Activate()
        {
        }

        public override void Suspend()
        {
        }

        public ICommand StartLocalGameCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    ParentViewModel.CurrentMenu = MenuType.AISelect;
                    ParentViewModel.Game.IsInSinglePlayerMode = true;
                }, o =>
                {
                    return true;
                });
            }
        }

        public ICommand StartNetworkGameCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    ParentViewModel.CurrentMenu = MenuType.NetworkGame;
                    ParentViewModel.Game.IsInSinglePlayerMode = false;
                });
            }
        }


    }
}
