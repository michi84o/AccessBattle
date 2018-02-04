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
    public class ArtificialIntelligenceContainer
    {
        public string Name { get; set; }
        public IArtificialIntelligence AI { get; set; }
    }

    public class AISelectionMenuViewModel : MenuViewModelBase
    {
        ObservableCollection<ArtificialIntelligenceContainer> _plugins;
        public ObservableCollection<ArtificialIntelligenceContainer> Plugins => _plugins;

        ArtificialIntelligenceContainer _selectedItem;
        public ArtificialIntelligenceContainer SelectedItem
        {
            get => _selectedItem;
            set { SetProp(ref _selectedItem, value); }
        }


        public AISelectionMenuViewModel(
            IMenuHolder parent) : base(parent)
        {
            _plugins = new ObservableCollection<ArtificialIntelligenceContainer>();
            var plugins = PluginHandler.Instance.GetPlugins<IArtificialIntelligenceFactory>();
            foreach (var plugin in plugins)
            {
                var ai = plugin.CreateInstance();
                var container = new ArtificialIntelligenceContainer { Name= ai.Name ?? "???", AI = ai };
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
                    ParentViewModel.Game.StartLocalGame(sel.AI);
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
