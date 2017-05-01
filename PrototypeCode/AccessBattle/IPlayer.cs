using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle
{
    public interface IPlayer
    {
        uint UID { get; }
        string Name { get; set; }
        void PlayTurn();
    }
}
