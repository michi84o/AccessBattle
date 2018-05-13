using AccessBattle;
using AccessBattle.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// The configuration of the neural network was created uding a genetic algorithm. (TODO)

namespace AccessBattleAI
{
    //[Export(typeof(IPlugin))]
    //[ExportMetadata("Name", "AccessBattle.AI.Nou")]
    //[ExportMetadata("Description", "Nou (Brain). A neural network.")]
    //[ExportMetadata("Version", "0.1")]
    public class NouFactory : IArtificialIntelligenceFactory
    {
        public IPluginMetadata Metadata { get; set; }

        public IArtificialIntelligence CreateInstance() => new Nou();
    }

    /// <summary>
    /// Implementation of Nou.
    /// </summary>
    public class Nou : AiBase
    {
        protected override string _name => "Nou";

        Random _rnd = new Random();

        public override string PlayTurn()
        {
            // Strategy: There is no strategy. The neurons just do their thing.

            // Deployment is random.
            if (Phase == GamePhase.Deployment)
                return Deploy();            



            throw new NotImplementedException();
        }

        string Deploy()
        {
            // Randomize cards:
            var list = new List<char> { 'V', 'V', 'V', 'V', 'L', 'L', 'L', 'L', };
            var n = list.Count;
            while (n > 1)
            {
                --n;
                int i = _rnd.Next(n + 1);
                char c = list[i];
                list[i] = list[n];
                list[n] = c;
            }
            string ret = "dp ";
            foreach (char c in list)
            {
                ret += c;
            }
            return ret;
        }
    }
}
