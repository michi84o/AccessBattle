using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle
{
    public struct Vector
    {
        public readonly ushort X;
        public readonly ushort Y;
        public Vector(ushort x, ushort y)
        {
            X = x;
            Y = y;
        }
    }
}
