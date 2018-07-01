using AccessBattle.Networking;
using AccessBattle.Networking.Packets;
using AccessBattle.Wpf.Interfaces;
using System.Collections.Generic;
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
               parent.Game.Client, nameof(parent.Game.Client.GameJoinRequested), JoinRequestedHandler);
        }

        public override void Activate()
        {

        }

        public override void Suspend()
        {

        }

        List<JoinMessage> _joinMessages = new List<JoinMessage>();

        public string CurrentJoiningPlayer
        {
            get
            {
                if (_joinMessages.Count == 0) return "";
                string msg;
                lock (_joinMessages)
                    msg = _joinMessages.Count > 0 ? _joinMessages[0]?.JoiningUser : "";
                return msg;
            }
        }
        public JoinMessage CurrentJoinMessage
        {
            get
            {
                if (_joinMessages.Count == 0) return null;
                JoinMessage msg;
                lock (_joinMessages)
                    msg = _joinMessages.Count > 0 ? _joinMessages[0] : null;
                return msg;
            }
        }

        void JoinRequestedHandler(object sender, GameJoinRequestedEventArgs args)
        {
            // Special case: Accepted connection but a decline is incoming!
            if (ParentViewModel.CurrentMenu == MenuType.Deployment && ParentViewModel.Game.Phase == GamePhase.PlayerJoining)
            {
                lock (_joinMessages)
                {
                    if (CurrentJoinMessage?.JoiningUser == args.Message.JoiningUser && args.Message.Request == JoinRequestType.Decline)
                    {
                        _joinMessages.Clear();
                        ParentViewModel.CurrentMenu = MenuType.WaitForJoin;
                    }
                }

            }

            // Might be in network menu and creating a new game
            if (ParentViewModel.CurrentMenu != MenuType.WaitForJoin &&
                ParentViewModel.CurrentMenu != MenuType.AcceptJoin) return;

            // Wrong game (TODO: test if this can happen)
            if (args.Message.UID != ParentViewModel.Game.UID)
            {
                ParentViewModel.Game.Client.ConfirmJoin(args.Message.UID, false);
                return;
            }

            // Handle decline
            lock (_joinMessages)
            {
                if (args.Message.Request == JoinRequestType.Decline)
                {
                    _joinMessages.RemoveAll(o => o.JoiningUser == args.Message.JoiningUser);
                }
                else
                {
                    _joinMessages.Add(args.Message);
                }
            }
            OnPropertyChanged(nameof(CurrentJoiningPlayer));
            OnPropertyChanged(nameof(CurrentJoinMessage));
            if (_joinMessages.Count == 0)
                ParentViewModel.CurrentMenu = MenuType.WaitForJoin;
        }

        public ICommand AcceptCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    ParentViewModel.Game.Client.ConfirmJoin(ParentViewModel.Game.UID, true);
                    ParentViewModel.CurrentMenu = MenuType.Deployment;
                    //MessageBox.Show("TODO: Init Game (p1)");
                    // At this stage it is still possible that we get a decline join packet from p2.
                    // In that case we must revert to the waiting menu.
                    // This is done above.
                    // Clear all join requests
                    lock (_joinMessages)
                    {
                        while (_joinMessages.Count > 1)
                            _joinMessages.RemoveAt(1);
                    }
                }, o => true);
            }
        }

        public ICommand DeclineCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    ParentViewModel.Game.Client.ConfirmJoin(ParentViewModel.Game.UID, false);
                    lock (_joinMessages)
                    {
                        if (_joinMessages.Count > 0)
                            _joinMessages.RemoveAt(0);
                    }
                    OnPropertyChanged(nameof(CurrentJoiningPlayer));
                    OnPropertyChanged(nameof(CurrentJoinMessage));
                    if (_joinMessages.Count == 0)
                        ParentViewModel.CurrentMenu = MenuType.WaitForJoin;
                }, o => true);
            }
        }
    }
}
