using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Networking.Packets
{
    /// <summary>
    /// Game synchronization packet.
    /// </summary>
    public class GameSync
    {
        /// <summary>Game UID.</summary>
        public uint UID { get; set; }
    }
}
