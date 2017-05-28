using AccessBattle;
using AccessBattle.Networking;
using AccessBattle.Networking.Packets;
using System;
using System.Collections.Generic;

namespace AccessBattleServer
{
    class Program
    {
        static GameServer _server;

        static void Main(string[] args)
        {
            Log.SetMode(LogMode.Debug);

            try
            {
                _server = new GameServer();
                _server.Start();

                Console.WriteLine("Game Server started");

                string line;
                while ((line = Console.ReadLine()) != "exit")
                {

                }
            }
            finally
            {
                Log.WriteLine("Stopping Server...");
                _server.Stop();
#if DEBUG
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
#endif
                Log.WriteLine("Game Server stopped");
            }
        }
    }
}
