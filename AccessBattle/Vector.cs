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

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is Vector))
                return false;            
            return (X == ((Vector)obj).X) && (Y == ((Vector)obj).Y);
        }

        public bool Equals(Vector v)
        {
            return (X == v.X) && (Y == v.Y);
        }

        public override int GetHashCode()
        {
            return X ^ Y;
        }
    }
}
