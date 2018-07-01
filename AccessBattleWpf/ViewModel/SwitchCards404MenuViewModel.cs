using AccessBattle.Wpf.Interfaces;
using System.Windows.Input;

namespace AccessBattle.Wpf.ViewModel
{
    public class SwitchCards404MenuViewModel : MenuViewModelBase
    {
        public ICommand YesCommand => new RelayCommand(o => { ParentViewModel.Game.PlayError404(true); });
        public ICommand NoCommand => new RelayCommand(o => { ParentViewModel.Game.PlayError404(false); });

        public SwitchCards404MenuViewModel(IMenuHolder parent) : base(parent) {  }

        public override void Activate()
        {

        }

        public override void Suspend()
        {

        }
    }
}
