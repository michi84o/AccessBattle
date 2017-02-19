using AccessBattle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// TODO

namespace AccessBattleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new GameServer();
            server.Start();

            Thread.Sleep(3000);

            server.Stop();

            Console.ReadKey();
        }
    }
}
