using AccessBattle.Wpf.Interfaces;

namespace AccessBattle.Wpf.ViewModel
{
    public abstract class MenuViewModelBase : PropChangeNotifier, IMenuViewModel
    {
        public IMenuHolder ParentViewModel { get; private set; }

        protected MenuViewModelBase(IMenuHolder parent)
        {
            ParentViewModel = parent;
        }

        public abstract void Activate();
        public abstract void Suspend();


    }
}
