using AccessBattle.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccessBattle.Networking.Packets;
using AccessBattle;

namespace AccessBattleAI
{
    [Export(typeof(IPlugin))]
    [ExportMetadata("Name", "AI.Bob")]
    [ExportMetadata("Description", "Bob is a simple stupid AI that just does random moves.")]
    [ExportMetadata("Version", "0.1")]
    public class Bob : IAiPlugin
    {
        public IPluginMetadata Metadata { get; set; }

        public string PlayTurn()
        {
            // TODO
            return "";
        }

        public void Synchronize(GameSync sync)
        {
            
        }
    }
}
