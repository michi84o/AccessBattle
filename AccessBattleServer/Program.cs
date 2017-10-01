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
                var userDb = new TextFileUserDatabaseProvider("userdb.txt");

                _server = new GameServer(userDatabase: userDb);
                _server.Start();

                Console.WriteLine("Game Server started");

                string line;
                while ((line = Console.ReadLine()) != "exit")
                {
                    bool ok = false;
                    line = line.Trim();
                    if (line == ("list"))
                    {
                        var games = _server.Games.ToList();
                        foreach (var game in games)
                            Console.WriteLine(game.Key + " - " + game.Value.Name);
                        ok = true;
                    }
                    else if (line.StartsWith("debug ", StringComparison.Ordinal))
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
                                ok = true;
                            }
                        }
                    }
                    else if (line.StartsWith("add user ", StringComparison.Ordinal))
                    {
                        var spl = line.Split(' ');
                        if (spl.Length == 4)
                        {
                            var pw = new System.Security.SecureString();
                            foreach (var c in spl[3]) pw.AppendChar(c);
                            if (userDb.AddUserAsync(spl[2], pw).GetAwaiter().GetResult())
                                Console.WriteLine("user added");
                            else Console.WriteLine("add failed");
                            ok = true;
                        }
                    }

                    if (!ok)
                    {
                        Console.WriteLine("error");
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
