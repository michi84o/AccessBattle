using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle
{
    public interface IBoardGame
    {
        PlayerState[] Players { get; }
        BoardField[,] Board { get; }
        GamePhase Phase { get;}
    }
}
