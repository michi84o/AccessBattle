using AccessBattle;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        static void TestCrypto()
        {
            var decrypter = new CryptoHelper();
            var encrypter = new CryptoHelper(decrypter.GetPublicKey());

            byte[] a = new byte[] { 0, 1, 2, 3 };

            var a1 = encrypter.Encrypt(a);
            var a2 = decrypter.Decrypt(a1);

            for (int i = 0; i < a1.Length; ++i)
            {
                Console.Write(a1[i] + " ");
            }
            Console.WriteLine();

        }

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
