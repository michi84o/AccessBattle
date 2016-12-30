using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RaiNet
{
    public abstract class OnlineCard
    {
        Player Owner { get; set; }
        public bool IsFaceUp { get; set; }
        bool HasBoost { get; set; }
        BoardField Location { get; set; }
    }

    public class Virus : OnlineCard
    {

    }

    public class Link : OnlineCard
    {

    }

}
