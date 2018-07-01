using Microsoft.VisualStudio.TestTools.UnitTesting;
using AccessBattle.MySqlProvider;
using System.Threading.Tasks;
using AccessBattle.Networking;
using System.Security;
using AccessBattle.Plugins;

namespace AccessBattleMySqlTest
{
    [TestClass]
    public class ConnectTest
    {
        // NEVER CHECK IN REAL PASSWORD OR USER INTO GITHUB !!!
        static string ConnectString = "Server=192.168.178.254;Port=3306;Database=test;Uid=XXXXXXX;Pwd=XXXXXXX;";

        [TestMethod]
        public async Task TestMySql() // Warning!!! This test creates a database on your database server.
        {
            IUserDatabaseProvider plugin = new MySqlDatabaseProvider();

            Assert.IsTrue(await plugin.Connect(ConnectString));

            //await plugin.DeleteUserAsync("testuser");

            var secStr = new SecureString();
            secStr.AppendChar('t');
            secStr.AppendChar('e');
            secStr.AppendChar('s');
            secStr.AppendChar('t');

            Assert.AreEqual(LoginCheckResult.InvalidUser, await plugin.CheckLoginAsync("test", secStr));

            Assert.IsTrue(await plugin.AddUserAsync("testuser1", secStr));
            Assert.IsTrue(await plugin.AddUserAsync("testuser2", new SecureString()));

            Assert.AreEqual(LoginCheckResult.InvalidPassword, await plugin.CheckLoginAsync("testuser1", new SecureString()));
            Assert.AreEqual(LoginCheckResult.LoginOK, await plugin.CheckLoginAsync("testuser1", secStr));

            Assert.IsTrue(await plugin.DeleteUserAsync("testuser1"));
            Assert.IsTrue(await plugin.DeleteUserAsync("testuser2"));

            plugin.Disconnect();
        }
    }
}
