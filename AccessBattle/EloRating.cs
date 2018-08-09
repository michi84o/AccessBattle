using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle
{
    /// <summary>
    /// Class for calculating the ELO rating for two players.
    /// </summary>
    public class EloRating
    {
        /// <summary>
        /// Calculate the new ELO values for two players.
        /// </summary>
        /// <param name="eloP1">ELO rating of player 1.</param>
        /// <param name="eloP2">ELO rating of player 2.</param>
        /// <param name="winner">Winner of the match: 0: Draw, 1: Player 1, 2: Player 2</param>
        /// <param name="eloP1New">New ELO value for player 1.</param>
        /// <param name="eloP2New">New ELO value for player 2.</param>
        /// <param name="k">K value. Default value is 32.</param>
        public static void Calculate(int eloP1, int eloP2, int winner, out int eloP1New, out int eloP2New, int k = 32)
        {
            if (winner < 0 || winner > 2) throw new ArgumentException("Invalid value for winner", nameof(winner));

            double e1 = 1.0 / (1 + Math.Pow(10, (eloP2 - eloP1)/400.0));
            double e2 = 1.0 / (1 + Math.Pow(10, (eloP1 - eloP2)/400.0));

            double s1, s2;
            switch (winner)
            {
                case 1: s1 = 1; s2 = 0; break;
                case 2: s1 = 0; s2 = 1; break;
                default: s1 = s2 = .5; break;
            }

            eloP1New = (int)((eloP1 + k * (s1 - e1)) + .5);
            eloP2New = (int)((eloP2 + k * (s2 - e2)) + .5);
        }
    }
}
