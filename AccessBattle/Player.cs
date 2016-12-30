using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RaiNet
{
    public class Player
    {
        public string Name { get; set; }
        public int Points { get; set; }

        public bool DidVirusCheck { get; set; }
        public bool Did404NotFound { get; set; }

        public Player()
        {
        }
    }
}
