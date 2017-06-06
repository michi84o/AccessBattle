using AccessBattle.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Wpf.ViewModel
{
    public abstract class MenuViewModelBase : PropChangeNotifier, IMenuViewModel
    {
        public IMenuHolder ParentViewModel { get; private set; }

        protected MenuViewModelBase(IMenuHolder parent)
        {
            ParentViewModel = parent;
        }
    }
}
