using AccessBattle;
using AccessBattle.Networking;
using AccessBattle.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AccessBattleServer
{
    class Program
    {
        static GameServer _server;

        static void Main(string[] args)
        {
            // Testcode
            //string hash, salt;
            //PasswordHasher.GetNewHash("passw0rd", out hash, out salt);
            //Console.WriteLine(hash);
            //Console.WriteLine(salt);
            //Console.WriteLine(PasswordHasher.VerifyHash("passw0rd", hash, salt));
            // ----------------

            Log.SetMode(LogMode.Console);
            Log.Priority = LogPriority.Information;

            try
            {
                _server = new GameServer();
                _server.Start();

                Console.WriteLine("Game Server started");

                string line;
                while ((line = Console.ReadLine()) != "exit")
                {
                    line = line.Trim();
                    if (line == ("list"))
                    {
                        var games = _server.Games.ToList();
                        foreach (var game in games)
                            Console.WriteLine(game.Key + " - " + game.Value.Name);
                    }
                    if (line.StartsWith("debug ", StringComparison.Ordinal))
                    {
                        var sp = line.Split(' ');
                        if (sp.Length == 4)
                        {
                            if (sp[1] == "win")
                            {
                                uint k = 0;
                                var spx = sp[2].Split('=');
                                if (spx.Length != 2) continue;
                                if (spx[0] == "key")
                                {

                                    if (!uint.TryParse(spx[1], out k)) continue;
                                }
                                else if (spx[0] == "name")
                                {
                                    k = _server.Games.FirstOrDefault(o => o.Value.Name == spx[1]).Key;
                                }
                                if (k == 0) continue;
                                int p;
                                if (!int.TryParse(sp[3], out p)) continue;
                                _server.Win(p, k);
                            }
                        }
                    }
                }
            }
            finally
            {
                Log.WriteLine(LogPriority.Information, "Stopping Server...");
                _server.Stop();
#if DEBUG
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
#endif
                Log.WriteLine(LogPriority.Information, "Game Server stopped");
            }
        }
    }
}
