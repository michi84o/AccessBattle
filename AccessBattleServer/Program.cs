using AccessBattle;
using AccessBattle.Networking;
using AccessBattle.Plugins;
using System;
using System.Linq;

// Commands:
// -usrdb=".\db\db.txt"
// -acceptany

namespace AccessBattleServer
{
    class Program
    {
        static GameServer _server;

        // TODO: Execute main code in a async task.
        static void Main(string[] args)
        {
            bool acceptAny = false;
            ushort port = 3221;

            #region Read command line params
            for (int i = 0; i<args.Length; ++i)
            {
                var arg = args[i];
                if (!arg.StartsWith("-", StringComparison.Ordinal) && !arg.StartsWith("/", StringComparison.Ordinal))
                {
                    Console.WriteLine("Parameters must start with '-' or '/'. Use '-?' to show help.");
                    return;
                }
                if (arg.Length == 1) continue;
                arg = arg.Substring(1, arg.Length - 1);

                if (arg == "acceptany")
                {
                    acceptAny = true;
                    continue;
                }
                else if (arg.StartsWith("port="))
                {
                    var spl = arg.Split('=');
                    if (spl.Length != 2 || !ushort.TryParse(spl[1], out port))
                    {
                        Console.WriteLine("Error in parameter 'port'");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine(
                        "Access Battle Server\r\n\r\n" +
                        "Usage: AccessBattleServer [-acceptany] [-port=3221]\r\n" +
                        "\r\nOptions:\r\n" +
                        "\t-port=3221    Define the port to use. Default: 3221\r\n" +
                        "\t-acceptany    Accept any client. Disables user database.\r\n" +
                        "\t              Clients will not require a password." +
                        "\r\nUsing '/' instead of '-' is allowed.\r\n"
                        );
                    return;
                }
            }
            #endregion

            Log.SetMode(LogMode.Console);
            Log.Priority = LogPriority.Information;

            // Create userdb folder if not existing
            IUserDatabaseProvider db = null;
            if (!acceptAny)
            {
                Console.WriteLine("==============");
                Console.WriteLine("Database Setup");
                Console.WriteLine("==============");
                var plugins = PluginHandler.Instance.GetPlugins<IUserDatabaseProviderFactory>();
                if (plugins.Count == 0)
                {
                    Console.WriteLine("No database plugin found.");
                }
                Console.WriteLine("Please select an option:\r\n");
                Console.WriteLine("\t1: Accept any client (no database)");
                Console.WriteLine("\t2: Text based database");
                for (int i = 0; i < plugins.Count; ++i)
                {
                    Console.WriteLine("\t"+(i+3)+": " + plugins[i].Metadata.Description);
                }
                Console.WriteLine("\r\nEnter the number of the option.");
                if (int.TryParse(Console.ReadLine(), out int ichoice) && ichoice > 0 && ichoice <= plugins.Count + 2)
                {
                    if (ichoice == 1)
                    {
                        acceptAny = true;
                    }
                    else if (ichoice == 2)
                    {
                        db = new TextFileUserDatabaseProvider();
                    }
                    else
                    {
                        db = plugins[ichoice - 3].CreateInstance();
                    }
                }
                else
                {
                    Console.WriteLine("Invalid choice! Server will exit.");
                    return;
                }
            }
            try
            {
                if (!acceptAny) // Have to check again
                {
                    try
                    {
                        Console.WriteLine("\r\n" + db.ConnectStringHint);
                        var connectString = Console.ReadLine();
                        if (!db.Connect(connectString).GetAwaiter().GetResult())
                        {
                            Log.WriteLine(LogPriority.Error, "Connecting to database failed. Server will exit.");
                            return;
                        }
                        Console.WriteLine();
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine(LogPriority.Error, "Error setting up database: " + e.Message);
                        Console.WriteLine("Server will exit.");
                        return;
                    }
                }


                _server = new GameServer(port, db)
                {
                    AcceptAnyClient = acceptAny
                };
                _server.Start();

                Console.WriteLine("Game Server started");
                if (acceptAny)
                    Console.WriteLine("! Any client is accepted");

                Console.WriteLine("Type 'help' to show available commands");

                string line;
                while ((line = Console.ReadLine()) != "exit")
                {
                    bool ok = false;
                    line = line.Trim();
                    if (line == ("help"))
                    {
                        Console.WriteLine(
                            "\texit                  Close this program.\r\n" +
                            "\tlist                  List all games\r\n" +
                            "\tadd user n p elo      Adds user 'u' with password 'p'. elo is optional ELO rating (default:1000) \r\n" +
                            "\tmcpw user             Check if user must change password\r\n" +
                            "\tdebug                 Debug command. Requires additional parameters:\r\n" +
                            "\t  win key=1234   1    Let player 1 win. Uses game key.\r\n" +
                            "\t  win name=gameX 2    Let player 2 win. Uses game name.");
                        ok = true;
                    }
                    else if (line == ("list"))
                    {
                        var games = _server.Games.ToList();
                        if (games.Count == 0)
                        {
                            Console.WriteLine("There are no games"); ;
                        }
                        else
                            foreach (var game in games)
                                Console.WriteLine(game.Key + " - " + game.Value.Name);
                        ok = true;
                    }
                    else if (line.StartsWith("mcpw"))
                    {
                        if (db == null) continue;
                        var sp = line.Split(' ');
                        if (sp.Length == 2)
                        {
                            bool? res = db.MustChangePasswordAsync(sp[1]).GetAwaiter().GetResult();

                            if (res == true)
                            {
                                Console.WriteLine("User must change password");
                                ok = true;
                            }
                            else if (res == false)
                            {
                                Console.WriteLine("User must not change password");
                                ok = true;
                            }
                        }
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
                        if (db == null) continue;
                        var spl = line.Split(' ');
                        if (spl.Length == 4 || spl.Length == 5)
                        {
                            int elo = 1000;
                            if (spl.Length == 5 && !int.TryParse(spl[4], out elo))
                                elo = 1000;

                            if (elo < 0) elo = 0;
                            if (elo > 10000) elo = 10000;

                            var pw = new System.Security.SecureString();
                            foreach (var c in spl[3]) pw.AppendChar(c);
                            if (db.AddUserAsync(spl[2], pw, elo).GetAwaiter().GetResult())
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
                _server?.Stop();

                db?.Disconnect();
#if DEBUG
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
#endif
                Log.WriteLine(LogPriority.Information, "Game Server stopped");
            }
        }
    }
}
