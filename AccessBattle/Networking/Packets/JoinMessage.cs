using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Networking.Packets
{
    /// <summary>Types of join requests.</summary>
    public enum JoinRequestType
    {
        /// <summary>Request to join game (P2 -> Server)</summary>
        Join            = 0,
        /// <summary> Error, join not possible (Server -> P2)</summary>
        Error           = 1,
        /// <summary>Request to accept join (Server -> P1)</summary>
        RequestAccept   = 2,
        /// <summary>Accept join (P1 -> Server -> P2) (P2 -> Server)</summary>
        Accept          = 3,
        /// <summary>Decline join (P1 -> Server -> P2)</summary>
        Decline         = 4,
    }

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
        /// Join Request
        /// </summary>
        public JoinRequestType Request { get; set; }

        /// <summary>
        /// User that is joining. Only used when Request is 2.
        /// </summary>
        public string JoiningUser { get; set; }

    }
}
