using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AccessBattle.Networking;
using AccessBattle.Networking.Packets;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace AccessBattleTests
{
    [TestClass]
    public class ServerTests
    {
        GameServer _server;

        #region Init and Cleanup
        [TestInitialize]
        public void Init()
        {
            _server = new GameServer();
            _server.Start();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _server.Stop();
        }
        #endregion

        [TestMethod]
        public void CreateAndJoinGame()
        {
            var client1 = new NetworkGameClient();
            var client2 = new NetworkGameClient();
            // Connect
            Assert.IsTrue(client1.Connect("127.0.0.1", 3221).GetAwaiter().GetResult());
            Assert.IsTrue(client2.Connect("127.0.0.1", 3221).GetAwaiter().GetResult());
            // Login
            Assert.IsTrue(client1.Login("P1", "").GetAwaiter().GetResult());
            Assert.IsTrue(client2.Login("P2", "").GetAwaiter().GetResult());
            // Create Gamne
            GameInfo info;
            Assert.IsNotNull((info = client1.CreateGame("P1_Game").GetAwaiter().GetResult()));
            Assert.AreEqual("P1_Game", info.Name);
            // List games
            List<GameInfo> games;
            Assert.IsNotNull((games = client2.RequestGameList().GetAwaiter().GetResult()));
            Assert.AreEqual(1, games.Count);

            // ==================================================================
            // Join game
            // ==================================================================
            // Join is event based due to the long waiting time for confirmation
            var client1Task = new TaskCompletionSource<bool>();
            var client2Task = new TaskCompletionSource<bool>();
            // Register events
            client1.GameJoinRequested += (s, a) =>
            {
                Assert.AreEqual(info.UID, a.Message.UID);
                // Accept request
                Assert.IsTrue(client1.ConfirmJoin(info.UID, true));
                client1Task.TrySetResult(true);
            };
            client2.GameJoinRequested += (s, a) =>
            {
                Assert.AreEqual(info.UID, a.Message.UID);
                // Accept request
                Assert.IsTrue(client2.ConfirmJoin(info.UID, true));
                client2Task.TrySetResult(true);
            };

            Assert.IsTrue(client2.RequestJoinGame(games[0].UID));

            // Abort after 3 seconds
            using (var ct = new CancellationTokenSource(3000))
            {
                ct.Token.Register(() =>
                {
                    client1Task.TrySetResult(false);
                    client2Task.TrySetResult(false);
                });
                Assert.IsTrue(client1Task.Task.GetAwaiter().GetResult());
                Assert.IsTrue(client2Task.Task.GetAwaiter().GetResult());
            }


        }
    }
}
