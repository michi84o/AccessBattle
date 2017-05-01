using AccessBattle;
using AccessBattle.Networking;
using AccessBattle.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

                Console.WriteLine("Connecting client 1...");
                var client1 = new GameClient();
                if (!client1.Connect("127.0.0.1", 3221).GetAwaiter().GetResult())
                {
                    Console.WriteLine("Client 1 connect failed!");
                    return;
                }

                Console.WriteLine("Connecting client 2...");
                var client2 = new GameClient();
                if (!client2.Connect("127.0.0.1", 3221).GetAwaiter().GetResult())
                {
                    Console.WriteLine("Client 2 connect failed!");
                    return;
                }

                Console.WriteLine("Logging in client 1...");
                if (!client1.Login("P1", "").GetAwaiter().GetResult())
                {
                    Console.WriteLine("Client 1 login failed!");
                    return;
                }

                Console.WriteLine("Logging in client 2...");
                if (!client2.Login("P2", "").GetAwaiter().GetResult())
                {
                    Console.WriteLine("Client 2 login failed!");
                    return;
                }

                GameInfo info;
                Console.WriteLine("Client 1 creating game...");
                if ((info = client1.CreateGame("P1_Game").GetAwaiter().GetResult()) == null)
                {
                    Console.WriteLine("Client 1 create game failed!");
                    return;
                }

                List<GameInfo> games;
                Console.WriteLine("Client 2 requesting game list...");
                if ((games = client2.RequestGameList().GetAwaiter().GetResult()) == null)
                {
                    Console.WriteLine("Client 2 could not receive game list!");
                    return;
                }

                if (games.Count != 1)
                {
                    Console.WriteLine("Game list is empty!");
                    return;
                }

                Console.WriteLine("Client 2 joining game ...");
                if (!client2.JoinGame(games[0].UID).GetAwaiter().GetResult())
                {
                    Console.WriteLine("Client 2 could not join game!");
                    return;
                }


                Console.WriteLine("Success!");
            }
            finally
            {
                Log.WriteLine("Stopping Server");
                _server.Stop();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
