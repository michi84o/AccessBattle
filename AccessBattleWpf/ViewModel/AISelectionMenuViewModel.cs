using AccessBattle.Plugins;
using AccessBattle.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AccessBattle.Wpf.ViewModel
{
    public class AiPluginContainer
    {
        public string Name { get; set; }
        public IAiPlugin Plugin { get; set; }
    }

    public class AISelectionMenuViewModel : MenuViewModelBase
    {
        ObservableCollection<AiPluginContainer> _plugins;
        public ObservableCollection<AiPluginContainer> Plugins => _plugins;

        AiPluginContainer _selectedItem;
        public AiPluginContainer SelectedItem
        {
            get => _selectedItem;
            set { SetProp(ref _selectedItem, value); }
        }


        public AISelectionMenuViewModel(
            IMenuHolder parent) : base(parent)
        {
            _plugins = new ObservableCollection<AiPluginContainer>();
            var plugins = PluginHandler.Instance.GetPlugins<IAiPlugin>();
            foreach (var plugin in plugins)
            {
                var container = new AiPluginContainer { Name=plugin.Name ?? "???", Plugin = plugin };
                _plugins.Add(container);
            }
            if (_plugins.Count > 0) SelectedItem = _plugins[0];
        }
        public ICommand BackCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    ParentViewModel.CurrentMenu = MenuType.Welcome;
                }, o => true);
            }
        }

        public ICommand PlayCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    var sel = _selectedItem;
                    if (sel == null) return;
                    ParentViewModel.Game.StartLocalGame(sel.Plugin);
                    ParentViewModel.CurrentMenu = MenuType.Deployment;
                }, o => _selectedItem != null);
            }
        }

        public override void Activate()
        {
        }

        public override void Suspend()
        {
        }
    }
}
