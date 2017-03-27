using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Networking.Packets
{
    /// <summary>
    /// Packet to be exchanged when synchronizing game state
    /// </summary>
    public class GameSync
    {
        public uint UID { get; set; }
        public string Name { get; set; }

        public string P1Name { get; set; }
        public string P2Name { get; set; }
        public bool P1DidVirusCheck { get; set; }
        public bool P2DidVirusCheck { get; set; }
        public bool P1Did404NotFound { get; set; }
        public bool P2Did404NotFound { get; set; }
        public int CurrentPlayer { get; set; }
        public int WinningPlayer { get; set; }

        public GamePhase Phase { get; set; }

        public List<BoardFieldSyncInfo> Board { get; set; }
    }

    public class BoardFieldSyncInfo
    {
        public int X { get; set; }
        public int Y { get; set; }
        public SyncCardType Type { get; set; }
        public int Owner { get; set; }
        public bool Boost { get; set; }
    }

    public enum SyncCardType
    {
        Unknown, // Link or Virus
        Virus,
        Link,
        Firewall,

    }

}
