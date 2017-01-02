using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccessBattle
{
    public abstract class OnlineCard
    {
        public Player Owner { get; set; }
        public bool IsFaceUp { get; set; }
        public bool HasBoost { get; set; }
        public BoardField Location { get; set; }
    }

    public class VirusCard : OnlineCard
    {

    }

    public class LinkCard : OnlineCard
    {

    }

}
