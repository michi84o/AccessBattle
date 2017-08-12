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
    public class WaitForJoinMenuViewModel : MenuViewModelBase
    {
        public WaitForJoinMenuViewModel(
            IMenuHolder parent) : base(parent)
        {
            WeakEventManager<NetworkGameClient, GameJoinRequestedEventArgs>.AddHandler(
                parent.Game.Client, nameof(parent.Game.Client.GameJoinRequested), JoinRequestedHandler);

            _canCancel = true;
        }

        public override void Activate()
        {

        }

        public override void Suspend()
        {

        }

        void JoinRequestedHandler(object sender, GameJoinRequestedEventArgs args)
        {
            // Might be in network menu and creating a new game
            if (ParentViewModel.CurrentMenu != MenuType.WaitForJoin) return;

            ParentViewModel.CurrentMenu = MenuType.AcceptJoin;
        }

        bool _canCancel;
        public bool CanCancel
        {
            get { return _canCancel; }
            set { SetProp(ref _canCancel, value); }
        }

        public ICommand CancelCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    ParentViewModel.Game.Client.ExitGame(ParentViewModel.Game.UID);
                    ParentViewModel.CurrentMenu = MenuType.NetworkGame;
                }, o => CanCancel);
            }
        }
    }
}