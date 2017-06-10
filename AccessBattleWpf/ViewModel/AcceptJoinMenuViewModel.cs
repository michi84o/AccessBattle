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
    class AcceptJoinMenuViewModel : MenuViewModelBase
    {
        public AcceptJoinMenuViewModel(
            IMenuHolder parent) : base(parent)
        {
            WeakEventManager<NetworkGameClient, GameJoinRequestedEventArgs>.AddHandler(
               parent.Model.Client, nameof(parent.Model.Client.GameJoinRequested), JoinRequestedHandler);
        }

        public override void Activate()
        {

        }

        public override void Suspend()
        {

        }

        List<JoinMessage> _joinMessages = new List<JoinMessage>();

        public string CurrentJoiningPlayer
            => _joinMessages.Count > 0 ? _joinMessages[0]?.JoiningUser : "";
        public JoinMessage CurrentJoinMessage => _joinMessages.Count > 0 ? _joinMessages[0] : null;

        void JoinRequestedHandler(object sender, GameJoinRequestedEventArgs args)
        {
            // Might be in network menu and creating a new game
            if (ParentViewModel.CurrentMenu != MenuType.WaitForJoin &&
                ParentViewModel.CurrentMenu != MenuType.AcceptJoin) return;

            // Wrong game (TODO: test if this can happen)
            if (args.Message.UID != ParentViewModel.Model.UID)
            {
                ParentViewModel.Model.Client.ConfirmJoin(args.Message.UID, false);
                return;
            }

            _joinMessages.Add(args.Message);
            OnPropertyChanged(nameof(CurrentJoiningPlayer));
            OnPropertyChanged(nameof(CurrentJoinMessage));
        }

        public ICommand AcceptCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    ParentViewModel.Model.Client.ConfirmJoin(ParentViewModel.Model.UID, true);
                    // TODO: Init game
                }, o => true);
            }
        }

        public ICommand DeclineCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    ParentViewModel.Model.Client.ConfirmJoin(ParentViewModel.Model.UID, false);
                }, o => true);
            }
        }
    }
}
