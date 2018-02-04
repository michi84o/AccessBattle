using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle
{
    public static class Helpers
    {
        /// <summary>
        /// Converts the coordinates for player 2. Does nothing if isPlayerHost=false.
        /// This function is used to flip the board when player 2 is viewing it.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="isPlayerHost">Must be true to perform the conversion.</param>
        public static void ConvertCoordinates(ref int x, ref int y, bool isPlayerHost = false)
        {
            if (isPlayerHost) return; // No rotation

            // If player is not host, the board is rotated by 180°
            // This only affects the board itself

            // Board fields:
            if (y >= 0 && y <= 7)
            {
                x = 7 - x;
                y = 7 - y;
            }
            // Stack P1
            else if (y == 8)
            {
                y = 9;
                //x = 7 - x;
            }
            // Stack P2
            else if (y == 9)
            {
                y = 8;
                //x = 7 - x;
            }
            // Server area
            else if (y == 10)
            {
                if (x == 4) x = 5;
                else if (x == 5) x = 4;
            }
        }
    }
}
