using AccessBattle;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattleTests
{
    [TestClass]
    public class EloTest
    {
        [TestMethod]
        public void TestElo()
        {
            int elop1 = 2400;
            int elop2 = 2000;

            int elop1new;
            int elop2new;

            EloRating.Calculate(elop1, elop2, 1, out elop1new, out elop2new);

            Assert.AreEqual(2403, elop1new);
            Assert.AreEqual(1997, elop2new);

            EloRating.Calculate(elop1, elop2, 2, out elop1new, out elop2new);

            Assert.AreEqual(2371, elop1new);
            Assert.AreEqual(2029, elop2new);

            EloRating.Calculate(elop1, elop2, 0, out elop1new, out elop2new);

            Assert.AreEqual(2387, elop1new);
            Assert.AreEqual(2013, elop2new);

        }
    }
}
