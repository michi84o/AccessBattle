using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Networking.Packets
{
    /// <summary>
    /// Message for joining a game.
    /// </summary>
    public class JoinMessage
    {
        /// <summary>
        /// Unique id of the game.
        /// </summary>
        public uint UID { get; set; }

        /// <summary>
        /// Request type:
        /// 0 - Request to join game (P2 -> Server)
        /// 1 - Error, join not possible (Server -> P2)
        /// 2 - Request to accept join (Server -> P1)
        /// 3 - Accept join (P1 -> Server -> P2) (P2 -> Server)
        /// 4 - Decline join (P1 -> Server -> P2)
        /// </summary>
        public int Request { get; set; }

        /// <summary>
        /// User that is joining. Only used when Request is 2.
        /// </summary>
        public string JoiningUser { get; set; }

    }
}
