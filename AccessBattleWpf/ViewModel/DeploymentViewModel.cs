using AccessBattle.Wpf.Interfaces;
using System;
using System.Text;
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
                        var card = ParentViewModel.Game.BoardFieldVm[x, y].Field.Card as OnlineCard;
                        if (card == null)
                        {
                            Log.WriteLine(LogPriority.Warning, "A card is missing at field " + x + "," + y);
                            return;
                        }
                        cmd.Append(card.Type == OnlineCardType.Link ? "L" : "V");
                    }

                    // Race condition: SendGameCommandAsync might cause a game update before current menu can be changed
                    //                 Set Current Menu first and restore it if sending command failed.
                    var currentMenu = ParentViewModel.CurrentMenu;
                    ParentViewModel.CurrentMenu = MenuType.OpponentTurn;
                    var result = await ParentViewModel.Game.SendGameCommandAsync(cmd.ToString());
                    if (!result)
                    {
                        ParentViewModel.CurrentMenu = currentMenu;
                        Log.WriteLine(LogPriority.Error, "DeploymentViewModel: Sending game command failed!");
                    }
                    
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
