using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle
{
    /// <summary>
    /// Represents a field on the board
    /// </summary>
    public class BoardField : PropChangeNotifier
    {
        /// <summary>X Location of this field.</summary>
        public ushort X { get; private set; }
        /// <summary>Y Location of this field.</summary>
        public ushort Y { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="x">X Location of this field.</param>
        /// <param name="y">Y Location of this field.</param>
        public BoardField(ushort x, ushort y)
        {
            X = x;
            Y = y;
        }

    }
}
