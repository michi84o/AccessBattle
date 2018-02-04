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

    public interface IArtificialIntelligenceFactory : IPlugin
    {
        IArtificialIntelligence CreateInstance();
    }

    public interface IArtificialIntelligence : IPlayer
    {
        /// <summary>
        /// Determines the side this AI is playing on
        /// (can later be used to let AIs play against each other).
        /// When true, AI is player 1.
        /// </summary>
        bool IsAiHost { get; set; }

        string PlayTurn();
        void Synchronize(GameSync sync);
    }
}
