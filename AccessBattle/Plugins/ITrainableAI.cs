using AccessBattle.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Plugins
{
    /// <summary>
    /// Interface for trainable AIs.
    /// </summary>
    public interface ITrainableAI : IArtificialIntelligence
    {
        /// <summary>
        /// Train the AI.
        /// </summary>
        /// <param name="sync">Current state of board.</param>
        /// <param name="command">Command that should be played. AI is player 1.</param>
        void Train(GameSync sync, string command);
    }
}
