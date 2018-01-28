using AccessBattle.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Plugins
{
    public interface IPlugin
    {
        IPluginMetadata Metadata { get; set; }
    }

    public interface IPluginMetadata
    {
        string Name { get;  }
        string Description { get;  }
        string Version { get;  }
    }

    public class PluginMetadata : IPluginMetadata
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
    }

    public interface IAiPlugin : IPlugin
    {
        string PlayTurn();
        void Synchronize(GameSync sync);
    }
}
