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
               parent.NetworkClient, nameof(parent.NetworkClient.GameJoinRequested), JoinRequestedHandler);
        }

        public override void Activate()
        {

        }

        public override void Suspend()
        {

        }

        // TODO: Add handling for multiple received join messages
        JoinMessage _currentJoinMessage;

        public string CurrentJoiningPlayer
        {
            get { return _currentJoinMessage?.JoiningUser; }
        }

        void JoinRequestedHandler(object sender, GameJoinRequestedEventArgs args)
        {
            // Might be in network menu and creating a new game
            if (ParentViewModel.CurrentMenu != MenuType.WaitForOpponent) return;

            if (_currentJoinMessage != null) return; // ignore new message for now
            _currentJoinMessage = args.Message;
            OnPropertyChanged(nameof(CurrentJoiningPlayer));
        }

        string _joiningPlayer;
        string JoiningPlayer
        {
            get { return _joiningPlayer; }
            set { SetProp(ref _joiningPlayer, value); }
        }

        public ICommand AcceptCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    // Depends what we are doing
                    // TODO
                }, o => true);
            }
        }

        public ICommand DeclineCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    // Depends what we are doing
                    // TODO
                }, o => true);
            }
        }
    }
}
