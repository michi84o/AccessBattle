using AccessBattle.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security;
using MySql.Data.MySqlClient;
using System.Net;
using System.Text.RegularExpressions;
using AccessBattle.Plugins;
using System.ComponentModel.Composition;
using System.Threading;

namespace AccessBattle.MySqlProvider
{
    // MySQL / MariaDB Database plugin
    //
    // MySQL / MariaDB default port is 3306.
    //
    // Database structure:
    // One Table is required.
    // +----------------------------+
    // |           users            |
    // +----------------------------+
    // | INT idUser                 |
    // | VARCHAR(32) userName       |
    // | INT elo                    |
    // | CHAR(64) passwordHash      |
    // | CHAR(64) passwordSalt      |
    // | TINYINT mustChangePassword |
    // +----------------------------+

    [Export(typeof(IPlugin))]
    [ExportMetadata("Name", "AccessBattle.DatabaseProviders.MySql")]
    [ExportMetadata("Description", "Database provider for MySql and MariaDB")]
    [ExportMetadata("Version", "0.1")]
    public class MySqlDatabaseProviderFactory : IUserDatabaseProviderFactory
    {
        public IPluginMetadata Metadata { get; set; }

        public IUserDatabaseProvider CreateInstance()
        {
            return new MySqlDatabaseProvider();
        }
    }

    public class MySqlDatabaseProvider : IUserDatabaseProvider
    {
        MySqlConnection _connection;
        SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public string ConnectStringHint => "Enter a connection string or nothing for interactive mode.\r\nConnection strings look like this:\r\nServer=myServerAddress;Port=3306;Database=myDataBase;Uid=myUsername;Pwd=myPassword";

        public async Task<bool> AddUserAsync(string user, SecureString password, int elo = 1000)
        {
            if (!IsConnected) return false;
            if (!LoginHelper.CheckUserName(user))
            {
                return false;
            }

            await _semaphoreSlim.WaitAsync();
            try
            {
                // Try to get user first. If user exists, only update password.
                var usr = new User();

                using (var cmd = new MySqlCommand(
                    "SELECT idUser,userName,elo,passwordHash,passwordSalt,mustChangePassword FROM users WHERE userName=@param1;", _connection))
                {
                    cmd.Parameters.Add(new MySqlParameter("@param1", user));
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader == null || reader.VisibleFieldCount != 6)
                        {
                            Log.WriteLine(LogPriority.Error, "Error: Reading user entry from database failed.");
                        }
                        if (reader.HasRows && await reader.ReadAsync())
                        {
                            usr.IdUser = Convert.ToInt32(reader.GetValue(0));
                            usr.UserName = Convert.ToString(reader.GetValue(1));
                            usr.ELO = Convert.ToInt32(reader.GetValue(2));
                            usr.PasswordHash = Convert.ToString(reader.GetValue(3));
                            usr.PasswordSalt = Convert.ToString(reader.GetValue(4));
                            usr.MustChangePassword = Convert.ToBoolean(reader.GetValue(5));
                        }
                    }
                }

                // Build password string
                string hash;
                string salt;
                PasswordHasher.GetNewHash(password.ConvertToUnsecureString(), out hash, out salt);

                // Add or update user
                if (usr.IdUser == 0)
                {
                    usr.UserName = user;
                }
                else
                {
                    if (usr.UserName != user)
                    {
                        Log.WriteLine(LogPriority.Error, "Error: Read user name from database does not match.");
                        return false;
                    }
                }
                usr.PasswordHash = hash;
                usr.PasswordSalt = salt;
                usr.MustChangePassword = false;
                usr.ELO = elo;

                // Insert
                if (usr.IdUser == 0)
                {
                    using (var cmd = new MySqlCommand(
                        "INSERT INTO users (userName,elo,passwordHash,passwordSalt,mustChangePassword) VALUES (@p1,@p2,@p3,@p4,@p5);", _connection))
                    {
                        cmd.Parameters.Add(new MySqlParameter("@p1", usr.UserName));
                        cmd.Parameters.Add(new MySqlParameter("@p2", usr.ELO));
                        cmd.Parameters.Add(new MySqlParameter("@p3", usr.PasswordHash));
                        cmd.Parameters.Add(new MySqlParameter("@p4", usr.PasswordSalt));
                        cmd.Parameters.Add(new MySqlParameter("@p5", usr.MustChangePassword));
                        if (await cmd.ExecuteNonQueryAsync() != 1)
                        {
                            Log.WriteLine(LogPriority.Error, "Error: Inserting user to database table failed.");
                            return false;
                        }
                        return true;
                    }
                }
                // Update
                else
                {
                    using (var cmd = new MySqlCommand(
                        "UPDATE users SET userName=@p2, elo=@p3, passwordHash=@p4, passwordSalt=@p5, mustChangePassword=@p6 " +
                        "WHERE idUser=@p1", _connection))
                    {
                        cmd.Parameters.Add(new MySqlParameter("@p1", usr.IdUser));
                        cmd.Parameters.Add(new MySqlParameter("@p2", usr.UserName));
                        cmd.Parameters.Add(new MySqlParameter("@p3", usr.ELO));
                        cmd.Parameters.Add(new MySqlParameter("@p4", usr.PasswordHash));
                        cmd.Parameters.Add(new MySqlParameter("@p5", usr.PasswordSalt));
                        cmd.Parameters.Add(new MySqlParameter("@p6", usr.MustChangePassword));
                        if (await cmd.ExecuteNonQueryAsync() != 1)
                        {
                            Log.WriteLine(LogPriority.Error, "Error: Updating user in database table failed.");
                            return false;
                        }
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "Error: " + e.Message);
                return false;
            }
            finally { _semaphoreSlim.Release(); }
        }

        public async Task<LoginCheckResult> CheckLoginAsync(string user, SecureString password)
        {
            if (!IsConnected) return LoginCheckResult.DatabaseError;
            if (!LoginHelper.CheckUserName(user))
            {
                return LoginCheckResult.InvalidUser;
            }

            await _semaphoreSlim.WaitAsync();
            try
            {
                // Try to get user first. If user exists, only update password.
                var usr = new User();

                using (var cmd = new MySqlCommand(
                    "SELECT idUser,userName,passwordHash,passwordSalt,mustChangePassword FROM users WHERE userName=@param1;", _connection))
                {
                    cmd.Parameters.Add(new MySqlParameter("@param1", user));
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader == null || reader.VisibleFieldCount != 5)
                        {
                            Log.WriteLine(LogPriority.Error, "Error: Reading user entry from database failed.");
                            return LoginCheckResult.DatabaseError;
                        }
                        if (reader.HasRows && await reader.ReadAsync())
                        {
                            usr.IdUser = Convert.ToInt32(reader.GetValue(0));
                            usr.UserName = Convert.ToString(reader.GetValue(1));
                            usr.PasswordHash = Convert.ToString(reader.GetValue(2));
                            usr.PasswordSalt = Convert.ToString(reader.GetValue(3));
                            usr.MustChangePassword = Convert.ToBoolean(reader.GetValue(4));
                        }
                        else
                        {
                            Log.WriteLine(LogPriority.Error, "Error: Reading user entry from database failed.");
                            return LoginCheckResult.InvalidUser;
                        }
                    }
                }

                if (usr.UserName != user)
                {
                    Log.WriteLine(LogPriority.Error, "Error: Reading user entry from database failed.");
                    return LoginCheckResult.DatabaseError;
                }

                // Build password string

                if (PasswordHasher.VerifyHash(password.ConvertToUnsecureString(), usr.PasswordHash, usr.PasswordSalt))
                {
                    return LoginCheckResult.LoginOK;
                }
                return LoginCheckResult.InvalidPassword;
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "Error: " + e.Message);
                return LoginCheckResult.Unknown;
            }
            finally { _semaphoreSlim.Release(); }
        }

        public async Task<bool> DeleteUserAsync(string user)
        {
            if (!IsConnected) return false;
            await _semaphoreSlim.WaitAsync();
            try
            {
                // Try to get user first. If user exists, only update password.
                using (var cmd = new MySqlCommand(
                    "DELETE FROM users WHERE userName=@param1;", _connection))
                {
                    cmd.Parameters.Add(new MySqlParameter("@param1", user));
                    if (await cmd.ExecuteNonQueryAsync() != 1)
                        return false;
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "Error: " + e.Message);
                return false;
            }
            finally { _semaphoreSlim.Release(); }
            return true;
        }

        public async Task<bool?> MustChangePasswordAsync(string user)
        {
            if (!IsConnected) return null;
            if (!LoginHelper.CheckUserName(user))
            {
                return null;
            }
            await _semaphoreSlim.WaitAsync();
            try
            {
                // Try to get user first. If user exists, only update password.
                using (var cmd = new MySqlCommand(
                    "SELECT mustChangePassword FROM users WHERE userName=@param1;", _connection))
                {
                    cmd.Parameters.Add(new MySqlParameter("@param1", user));
                    var x = await cmd.ExecuteScalarAsync();
                    if (x == null) return null;
                    return (Convert.ToInt32(x) == 1);
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "Error: " + e.Message);
                return null;
            }
            finally { _semaphoreSlim.Release(); }
        }

        /// <summary>
        /// Connects to MySql server using MySQL connection strings.
        /// If the specified database does not exist, it will be created!
        /// </summary>
        /// <returns>true if successful.</returns>
        // Connect String: Server=myServerAddress;Port=3306;Database=myDataBase;Uid=myUsername;Pwd=myPassword;
        public async Task<bool> Connect(string connectstring)
        {
            if (_disposed) return false;
            Disconnect();

            string server;
            ushort port;
            string database;
            string uid;
            string password;

            // If parameter not set, enter interactive mode
            if (string.IsNullOrEmpty(connectstring))
            {
                Console.WriteLine("MySQL Database Plugin - Interactive Mode");
                Console.WriteLine("Enter address of database server:");
                server = Console.ReadLine();
                if (!IPAddress.TryParse(server, out IPAddress adr))
                {
                    Console.WriteLine("Invalid server address! Connect aborted");
                    return false;
                }
                Console.WriteLine("Enter port for server (default is 3306):");
                string portStr = Console.ReadLine();
                if (!ushort.TryParse(portStr, out port) || port <= 1024)
                {
                    Console.WriteLine("Invalid port! Connect aborted");
                    return false;
                }
                Console.WriteLine("Enter database name:");
                database = Console.ReadLine();
                if (string.IsNullOrEmpty(database) || !Regex.IsMatch(database, "^[0-9a-zA-Z$_]+$"))
                {
                    Console.WriteLine("Invalid database name! Connect aborted");
                    return false;
                }
                Console.WriteLine("Enter user name:");
                uid = Console.ReadLine();
                if (string.IsNullOrEmpty(uid))
                {
                    Console.WriteLine("Invalid user name! Connect aborted");
                    return false;
                }
                Console.WriteLine("Enter user password:");
                password = EnterPassword();
                if (password == null) // allow empty password
                {
                    Console.WriteLine("Invalid user password! Connect aborted");
                    return false;
                }

            }
            else
            {
                // Check if all the parameters are there
                var spl = connectstring.Split(';').ToList();

                var param = spl.FirstOrDefault(s => s.StartsWith("Server="));
                if (param == null)
                {
                    Log.WriteLine(LogPriority.Error, "Server parameter is missing!");
                    return false;
                }
                var spl1 = param.Split('=');
                if (spl1.Length != 2)
                {
                    Log.WriteLine(LogPriority.Error, "Invalid Server parameter!");
                    return false;
                }
                var value = spl1[1].Trim();
                if (!IPAddress.TryParse(value, out IPAddress adr))
                {
                    Log.WriteLine(LogPriority.Error, "Invalid server address! Connect aborted");
                    return false;
                }
                server = value;

                param = spl.FirstOrDefault(s => s.StartsWith("Port="));
                if (param == null)
                {
                    Log.WriteLine(LogPriority.Error, "Port parameter is missing!");
                    return false;
                }
                spl1 = param.Split('=');
                if (spl1.Length != 2)
                {
                    Log.WriteLine(LogPriority.Error, "Invalid Port parameter!");
                    return false;
                }
                value = spl1[1].Trim();
                if (!ushort.TryParse(value, out port) || port <= 1024)
                {
                    Log.WriteLine(LogPriority.Error, "Invalid port! Connect aborted");
                    return false;
                }

                param = spl.FirstOrDefault(s => s.StartsWith("Database="));
                if (param == null)
                {
                    Log.WriteLine(LogPriority.Error, "Database parameter is missing!");
                    return false;
                }
                spl1 = param.Split('=');
                if (spl1.Length != 2)
                {
                    Log.WriteLine(LogPriority.Error, "Invalid Database parameter!");
                    return false;
                }
                value = spl1[1].Trim();
                if (string.IsNullOrEmpty(value) || !Regex.IsMatch(value, "^[0-9a-zA-Z$_]+$"))
                {
                    Log.WriteLine(LogPriority.Error, "Invalid database name! Connect aborted");
                    return false;
                }
                database = value;

                param = spl.FirstOrDefault(s => s.StartsWith("Uid="));
                if (param == null)
                {
                    Log.WriteLine(LogPriority.Error, "Uid parameter is missing!");
                    return false;
                }
                spl1 = param.Split('=');
                if (spl1.Length != 2)
                {
                    Log.WriteLine(LogPriority.Error, "Invalid Uid parameter!");
                    return false;
                }
                value = spl1[1].Trim();
                if (string.IsNullOrEmpty(value))
                {
                    Log.WriteLine(LogPriority.Error, "Invalid user name! Connect aborted");
                    return false;
                }
                uid = value;

                param = spl.FirstOrDefault(s => s.StartsWith("Pwd="));
                if (param == null)
                {
                    Log.WriteLine(LogPriority.Error, "Pwd parameter is missing!");
                    return false;
                }
                spl1 = param.Split('=');
                if (spl1.Length != 2)
                {
                    Log.WriteLine(LogPriority.Error, "Invalid Pwd parameter!");
                    return false;
                }
                value = spl1[1].Trim();
                if (value == null) // allow empty password
                {
                    Log.WriteLine(LogPriority.Error, "Invalid password! Connect aborted");
                    return false;
                }
                password = value;
            }

            // All data was entered
            // Now check if database exists
            try
            {
                _connection = new MySqlConnection(
                    string.Format("Server={0};Port={1};Uid={2};Pwd={3};",
                        server, port, uid, password));
                await _connection.OpenAsync();
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "Error: " + e.Message);
                return false;
            }

            try
            {
                object result;
                using (var cmd = new MySqlCommand("SHOW DATABASES LIKE @param1;", _connection))
                {
                    cmd.Parameters.Add(new MySqlParameter("@param1", database));
                     result = await cmd.ExecuteScalarAsync();
                }
                if (result == null)
                {
                    // Cannot use parameter here. But we already checked the database string for validity.
                    using (var cmd = new MySqlCommand("CREATE DATABASE "+database+" DEFAULT CHARACTER SET utf8 DEFAULT COLLATE utf8_general_ci ;", _connection))
                    {
                        if (await cmd.ExecuteNonQueryAsync() == -1)
                        {
                            Log.WriteLine(LogPriority.Error, "Error: Database does not exist and could not be created");
                            Disconnect();
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "Error: " + e.Message);
                Disconnect();
                return false;
            }

            // Now that we made sure the database exits, we can connect directly to that database
            // First reconnect
            Disconnect();
            try
            {
                _connection = new MySqlConnection(
                    string.Format("Server={0};Port={1};Database={2};Uid={3};Pwd={4};",
                server, port, database, uid, password));
                await _connection.OpenAsync();
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "Error: " + e.Message);
                return false;
            }
            // We are connected. Now check if the user table exists:
            try
            {
                object result;
                using (var cmd = new MySqlCommand("SHOW TABLES LIKE 'users';", _connection))
                {
                    result = await cmd.ExecuteScalarAsync();
                }
                if (result == null)
                {
                    using (var cmd = new MySqlCommand(
                        "CREATE TABLE IF NOT EXISTS users (" +
                        "idUser int(11) NOT NULL AUTO_INCREMENT, " +
                        "userName VARCHAR(32) NOT NULL, " +
                        "elo int(11) NOT NULL DEFAULT 1000, " +
                        "passwordHash CHAR(64) NOT NULL, " +
                        "passwordSalt CHAR(64) NOT NULL, " +
                        "mustChangePassword TINYINT NOT NULL, " +
                        "PRIMARY KEY (idUser), " +
                        "UNIQUE KEY userName (userName(32)) " +
                        ") ENGINE=InnoDB DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci AUTO_INCREMENT=1; ", _connection))
                    {
                        if (await cmd.ExecuteNonQueryAsync() == -1)
                        {
                            Log.WriteLine(LogPriority.Error, "Error: Table 'users' dies not exist and could not be created");
                            Disconnect();
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "Error: " + e.Message);
                Disconnect();
                return false;
            }

            return IsConnected;
        }

        bool IsConnected => _connection?.State == System.Data.ConnectionState.Open;

        string EnterPassword()
        {
            List<char> pwChars = new List<char>();
            while (true)
            {
                var info = Console.ReadKey(true);
                if (info.Key == ConsoleKey.Enter)
                    break;
                if (info.Key == ConsoleKey.Backspace)
                {
                    if (pwChars.Count > 0)
                    {
                        pwChars.RemoveAt(pwChars.Count - 1);
                        Console.Write("\b \b");
                    }
                    continue;
                }
                pwChars.Add(info.KeyChar);
                Console.Write("*");
            }
            return new string(pwChars.ToArray());
        }

        public void Disconnect()
        {
            if (_disposed) return;
            _connection?.Close();
            _connection?.Dispose();
            _connection = null;
        }

        #region IDisposeable

        bool _disposed;

        void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here.
                _connection?.Dispose();
                _connection = null;
            }

            // Free any unmanaged objects here.
            //

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<int?> GetELO(string user)
        {
            if (!IsConnected) return null;
            if (!LoginHelper.CheckUserName(user))
            {
                return null;
            }
            await _semaphoreSlim.WaitAsync();
            try
            {
                // Try to get user first. If user exists, only update password.
                using (var cmd = new MySqlCommand(
                    "SELECT elo FROM users WHERE userName=@param1;", _connection))
                {
                    cmd.Parameters.Add(new MySqlParameter("@param1", user));
                    var x = await cmd.ExecuteScalarAsync();
                    if (x == null) return null;
                    return Convert.ToInt32(x);
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "Error: " + e.Message);
                return null;
            }
            finally { _semaphoreSlim.Release(); }
        }

        public async Task<bool> SetELO(string user, int elo)
        {
            if (!IsConnected) return false;
            if (!LoginHelper.CheckUserName(user))
            {
                return false;
            }
            await _semaphoreSlim.WaitAsync();
            try
            {
                // Try to get user first. If user exists, only update password.
                using (var cmd = new MySqlCommand(
                    "UPDATE users SET elo=@param1 WHERE userName=@param2;", _connection))
                {
                    cmd.Parameters.Add(new MySqlParameter("@param1", elo));
                    cmd.Parameters.Add(new MySqlParameter("@param2", user));
                    return await cmd.ExecuteNonQueryAsync() == 1;
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "Error: " + e.Message);
                return false;
            }
            finally { _semaphoreSlim.Release(); }
        }

        ~MySqlDatabaseProvider()
        {
            Dispose(false);
        }

        #endregion
    }
}
