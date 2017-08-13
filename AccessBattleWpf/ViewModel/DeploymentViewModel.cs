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

        public override void Activate()
        {
        }

        public override void Suspend()
        {
        }

        public ICommand ConfirmCommand =>
#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
            new RelayCommand(async o =>
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
            {
                if (ParentViewModel.IsBusy) return;
                ParentViewModel.IsBusy = true;
                try
                {
                    // Build command
                    var cmd = new StringBuilder("dp ");
                    for (int x = 0; x <= 7; ++x)
                    {
                        int y = 0;
                        if (x == 3 || x == 4) y = 1;
                        var card = ParentViewModel.Game.BoardFields[x, y].Field.Card as OnlineCard;
                        if (card == null)
                        {
                            Log.WriteLine("A card is missing at field " + x + "," + y);
                            return;
                        }
                        cmd.Append(card.Type == OnlineCardType.Link ? "L" : "V");
                    }
                    ParentViewModel.IsBusy = true;
                    var result = await ParentViewModel.Game.Client.SendGameCommand(ParentViewModel.Game.UID, cmd.ToString());
                    if (!result) Log.WriteLine("DeploymentViewModel: Sending game command failed!");
                    else ParentViewModel.CurrentMenu = MenuType.None;
                }
                catch (Exception)
                {
                    return;
                }
                finally
                {
                    ParentViewModel.IsBusy = false;
                    CommandManager.InvalidateRequerySuggested();
                }
            }, o => { return ParentViewModel.Game.CanConfirmDeploy && !ParentViewModel.IsBusy; });
    }
}
