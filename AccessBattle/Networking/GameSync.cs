using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Networking
{
    /// <summary>
    /// Game synchronization class.
    /// </summary>
    public class GameSync
    {
        /// <summary>Game ID.</summary>
        public int UID { get; set; }
        /// <summary>Current game phase.</summary>
        public GamePhase Phase { get; set; }

        /// <summary>State of player that created the game.</summary>
        public PlayerSync Player1 { get; set; }
        /// <summary>State of player that joined the game.</summary>
        public PlayerSync Player2 { get; set; }
        /// <summary>State of the board.</summary>
        public BoardSync Board { get; set; }
    }

    /// <summary>
    /// Player synchronization class.
    /// </summary>
    /// <remarks>
    /// User names and ID have not to be synced because they don't change after join.
    /// </remarks>
    public class PlayerSync
    {
        /// <summary>Points of the player (to track score if played multiple times).</summary>
        public int Points { get; set; }
        /// <summary>Player already did his virus check.</summary>
        public bool DidVirusCheck { get; set; }
        /// <summary>Player already used the 404 card.</summary>
        public bool Did404NotFound { get; set; }
    }

    /// <summary>
    /// Board synchronization class.
    /// Contains locations of all deployed cards.
    /// </summary>
    public class BoardSync
    {
        // TODO: Make sure players can not cheat. Do not send data that the opponent cannot see. No IDs
    }

}
