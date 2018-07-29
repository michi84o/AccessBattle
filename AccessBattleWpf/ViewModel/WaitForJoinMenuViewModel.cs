using AccessBattle.Networking;
using AccessBattle.Wpf.Interfaces;
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

#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
        public ICommand CancelCommand => new RelayCommand(async o =>
        {
            CanCancel = false;
            await ParentViewModel.Game.Client.ExitGame(ParentViewModel.Game.UID, Networking.Packets.ExitGameReason.Cancelled);
            ParentViewModel.CurrentMenu = MenuType.NetworkGame;
            CanCancel = true;
        }, o => CanCancel);
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
    }
}