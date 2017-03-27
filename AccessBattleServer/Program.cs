using AccessBattle;
using AccessBattle.Networking;
using AccessBattle.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var pubKey = decrypter.GetPublicKey();
            var encrypter = new CryptoHelper(pubKey);

            var a = new byte[] { 0, 1, 2, 3 };

            var a1 = encrypter.Encrypt(a);
            var a2 = decrypter.Decrypt(a1);
            var error = false;
            for (int i = 0; i < a.Length; ++i)
            {
                error |= (a2[i] != a[i]);
            }
            if (error) Log.WriteLine("TestCrypto FAIL");
            else Log.WriteLine("TestCrypto OK");
        }

        static void TestNetworkPacket()
        {
            var bytes = new byte[256];
            for (int i = 0; i <= 255; ++i) // don't use byte here !!!
            {
                bytes[i] = (byte)i;
            }
            var packet = new NetworkPacket(bytes, 8);
            var packetRaw = packet.ToByteArray();
            // Expecting 3 escapes
            if (packetRaw.Length != bytes.Length + 3 + 6)
                Log.WriteLine("Network packet length fail 1");

            // pack it into new array
            var packetRawPadded = new byte[packetRaw.Length + 3];
            Array.Copy(packetRaw, 0, packetRawPadded, 1, packetRaw.Length);

            int stxIndex, etxIndex;
            var packet2 = NetworkPacket.FromByteArray(packetRawPadded, out stxIndex, out etxIndex);
            if (packet2 == null)
            {
                Log.WriteLine("Network packet fail");
                return;
            }

            if (stxIndex != 1)
                Log.WriteLine("Network packet STX fail");
            if (etxIndex != packetRaw.Length)
                Log.WriteLine("Network packet ETX fail");
            if (packet2.PacketType != 8)
                Log.WriteLine("Network packet type fail");
            if (packet2.Data.Length != 256)
            {
                Log.WriteLine("Network packet length fail 2");
                return;
            }
            // Compare
            var fail = false;
            for (int i = 0; i <= 255; ++i) // do not use byte here !!!
            {
                fail |= bytes[i] != packet2.Data[i];
            }
            if (fail)
                Log.WriteLine("Network packet data fail");
        }

        static void TestByteBuffer()
        {
            var buf = new ByteBuffer(8);
            var bufIn = new byte[3];
            var j = 0;
            for (int i = 0; i < 5000; ++i)
            {
                if (buf.Capacity - buf.Length < 3)
                {
                    byte[] bufOut;
                    Debug.Assert(buf.Take(3, out bufOut));
                    Debug.Assert(bufOut[0] == (j++ % 256));
                    Debug.Assert(bufOut[1] == (j++ % 256));
                    Debug.Assert(bufOut[2] == (j++ % 256));
                }
                bufIn[0] = (byte)(i % 256);
                bufIn[1] = (byte)(++i % 256);
                bufIn[2] = (byte)(++i % 256);
                Debug.Assert(buf.Add(bufIn));
            }
            //buf.Add(new byte[8]);
        }

        static async Task<bool> TestConnectAndLoginClient(GameClient client)
        {
            var result = await client.Connect("127.0.0.1", 3221);
            if (result) result = await client.Login("foo", "bar");
            return result;
        }

        static void PrintGameList(List<GameInfo> gameList)
        {
            Console.WriteLine("Game List:");
            if (gameList != null && gameList.Count > 0)
                foreach (var info in gameList)
                {
                    Console.WriteLine("  [UID:" +  info.UID  + ";Name:" + info.Name + ";Host:" + info.Player1 + "]");
                }
            else
                Console.WriteLine("  There are currently no games");
        }

        static void Main()
        {
            Log.SetMode(LogMode.Debug);

            _server = new GameServer();
            _server.Start();

            var client = new GameClient();

            var t = TestConnectAndLoginClient(client); t.Wait();
            if (t.Result)
            {
                var t2 = client.RequestGameList(); t2.Wait();
                PrintGameList(t2.Result);
            }

            var t3 = client.CreateGame("Game1");
            t.Wait();
            if (t3.Result != null)
                Console.WriteLine("Game successfully created. Uid: " + t3.Result.UID);

            var t4 = client.RequestGameList(); t4.Wait();
            PrintGameList(t4.Result);

            Console.WriteLine("Creating Player 2...");
            var client2 = new GameClient();
            Console.WriteLine("...connect...");
            client.Connect("127.0.0.1", 3221).Wait();
            Console.WriteLine("...login...");
            client.Login("p2", "1234").Wait();
            Console.WriteLine("...join...");
            var t5 = client.JoinGame(t4.Result[0].UID);
            t5.Wait();
            Console.WriteLine("Player 2 login status: " + t5.Result);

            string input;
            while ((input = Console.ReadLine()) != "exit")
            {

            }
            client.Disconnect();
            _server.Stop();
            Console.WriteLine("Server stopped. Program will exit in 5 seconds.");
            Thread.Sleep(5000);
        }


    }
}
