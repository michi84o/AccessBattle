using AccessBattle.Networking.Packets;
using AccessBattle.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AccessBattle.Wpf.ViewModel
{
    public class DeploymentViewModel : MenuViewModelBase
    {
        public DeploymentViewModel(
            IMenuHolder parent) : base(parent)
        {
        }

        public int LinkCardsLeft
        {
            get { return 0; } // TODO
        }

        public int VirusCardsLeft
        {
            get { return 0; } // TODO
        }

        public override void Activate()
        {

        }

        public override void Suspend()
        {

        }

        public ICommand ConnectToServerCommand =>
            new RelayCommand( o =>
            {
                // TODO
                ParentViewModel.Game.Client.SendGameCommand(ParentViewModel.Game.UID, "dp LLVVVVLL");

                CommandManager.InvalidateRequerySuggested();
            }, o => { return true; }); // TODO
    }
}
