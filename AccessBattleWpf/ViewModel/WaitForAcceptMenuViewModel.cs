using AccessBattle.Wpf.Interfaces;
using System.Windows.Input;

namespace AccessBattle.Wpf.ViewModel
{
    public class WaitForAcceptMenuViewModel : MenuViewModelBase
    {
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="parent">Parent viewmodel.</param>
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
