using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Networking.Packets
{
    /// <summary>
    /// Packet sent when a game is about to be deleted from the server.
    /// Also used by players to remove a game they just created.
    /// </summary>
    public class ExitGame
    {
        /// <summary>
        /// Unique id of the game.
        /// </summary>
        public uint UID { get; set; }
    }
}
