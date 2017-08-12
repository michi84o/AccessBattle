using AccessBattle.Networking;
using AccessBattle.Networking.Packets;
using AccessBattle.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AccessBattle.Wpf.ViewModel
{
    public class WaitForAcceptMenuViewModel : MenuViewModelBase
    {
        NetworkGameMenuViewModel _networkGameMenu;

        public WaitForAcceptMenuViewModel(
            IMenuHolder parent) : base(parent)
        {
        }

        public override void Activate()
        {

        }

        public override void Suspend()
        {

        }

        public ICommand CancelCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    var jg = ParentViewModel.JoiningGame?.UID;
                    if (jg != null)
                        ParentViewModel.Game.Client.ConfirmJoin(jg.Value, false);
                    else
                        ParentViewModel.CurrentMenu = MenuType.NetworkGame;
                }, o => true);
            }
        }

    }
}
