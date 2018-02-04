using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccessBattle.Networking.Packets;

namespace AccessBattle.Plugins
{
    public abstract class AiBase : IAiPlugin
    {
        GameSync _lastSync;

        public IPluginMetadata Metadata { get; set; }

        public uint UID => 0;

        protected abstract string _name { get; }
        public string Name { get => _name; set { } }

        public abstract string PlayTurn();
        
        public void Synchronize(GameSync sync)
        {
            _lastSync = sync;
        }


    }
}
