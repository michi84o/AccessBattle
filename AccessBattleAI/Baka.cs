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
    [ExportMetadata("Name", "AccessBattle.AI.BAKA")]
    [ExportMetadata("Description", "BAKA (jap. fool) is a simple stupid AI that just does nearly random moves.")]
    [ExportMetadata("Version", "0.1")]
    public class Baka : AiBase 
    {
        protected override string _name => "BAKA";

        public override string PlayTurn()
        {
            // TODO
            return "";
        }
    }
}
