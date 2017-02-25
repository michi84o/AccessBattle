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
        static GameServer _server;

        static void Main(string[] args)
        {
            _server = new GameServer();
            _server.Start();

            var client = new GameClient();
            if (client.Connect("127.0.0.1", 3221))
            {
                client.SendMessage("Hello");
            }
            
            string input;
            while ((input = Console.ReadLine()) != "exit")
            {

            }
            client.Disconnect();
            _server.Stop();
            Thread.Sleep(5000);        
        }

        

    }
}
