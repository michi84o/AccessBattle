using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Networking.Packets
{
    public class GameInfo
    {
        public uint UID { get; set; }
        public string Name { get; set; }
        public string Player1 { get; set; }
    }
}
