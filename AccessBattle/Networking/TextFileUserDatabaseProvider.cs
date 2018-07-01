using AccessBattle.Plugins;
using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AccessBattle.Networking
{
    /// <summary>
    /// Simple text file based user database.
    /// Format: "Username" "Password Hash" "Salt" "MustChangePassword(0/1)"
    /// Usernames can not have space in it.
    /// </summary>
    /// <remarks>
    /// This class is not optimized for a large amount of users since
    /// it completely reads and rewrites the file.
    /// </remarks>
    public class TextFileUserDatabaseProvider : IUserDatabaseProvider
    {
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        string _databaseFile;

        public string ConnectStringHint => "Enter a file name for a text file";

        /// <summary>
        ///
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<bool> AddUserAsync(string user, SecureString password)
        {
            if (_databaseFile == null) return false;
            user = user.Trim();
            if (!LoginHelper.CheckUserName(user))
            {
                return false;
            }
            await semaphoreSlim.WaitAsync();
            try
            {
                string allText = null;
                if (File.Exists(_databaseFile))
                {
                    await Task.Run(() =>
                    {
                        allText = File.ReadAllText(_databaseFile);
                    });
                    if (allText == null) return false;
                }
                else allText = "";

                // Check if there is a line that starts with username
                allText = allText.Replace("\r", "").Replace("\t", "");
                var lines = allText.Split('\n');
                if (lines.Any(line => line.StartsWith(user + " ", StringComparison.Ordinal)))
                {
                    return false;
                }
                string hash, salt;
                if (!PasswordHasher.GetNewHash(password.ConvertToUnsecureString(), out hash, out salt)) return false;

                allText += user + " " + hash + " " + salt + " 1\n";

                allText.Replace("\n", "\r\n");
                await Task.Run(() => { File.WriteAllText(_databaseFile, allText); });

                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally { semaphoreSlim.Release(); }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns>0: Login OK. 1: Invalid user name. 2: Invalid Password. 3: Database Error.</returns>
        public async Task<LoginCheckResult> CheckLoginAsync(string user, SecureString password)
        {
            if (_databaseFile == null) return LoginCheckResult.DatabaseError;
            user = user.Trim();
            if (!LoginHelper.CheckUserName(user))
            {
                return LoginCheckResult.InvalidUser;
            }

            if (!File.Exists(_databaseFile))
            {
                Log.WriteLine(LogPriority.Error, "Error in text file user database: File does not exist!");
                return LoginCheckResult.DatabaseError;
            }
            await semaphoreSlim.WaitAsync();
            try
            {
                string allText = null;
                await Task.Run(() =>
                {
                    allText = File.ReadAllText(_databaseFile);
                });
                if (allText == null) return LoginCheckResult.DatabaseError;

                // Check if there is a line that starts with username
                allText = allText.Replace("\r", "").Replace("\t", "");
                var lines = allText.Split('\n');
                var line = lines.FirstOrDefault(l => l.StartsWith(user + " ", StringComparison.Ordinal));
                if (string.IsNullOrEmpty(line)) return LoginCheckResult.InvalidUser;
                var linespl = line.Split(' ');
                if (linespl.Length != 4) return LoginCheckResult.DatabaseError;
                if (linespl[0] != user) return LoginCheckResult.DatabaseError;
                if (PasswordHasher.VerifyHash(password.ConvertToUnsecureString(), linespl[1], linespl[2]))
                    return LoginCheckResult.LoginOK;
                return LoginCheckResult.InvalidPassword;
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "Error in text file user database: " + e);
                return LoginCheckResult.DatabaseError;
            }
            finally { semaphoreSlim.Release(); }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<bool> DeleteUserAsync(string user)
        {
            if (_databaseFile == null) return false;
            user = user.Trim();
            if (!LoginHelper.CheckUserName(user))
            {
                return false;
            }
            if (!File.Exists(_databaseFile)) return false;
            await semaphoreSlim.WaitAsync();
            try
            {
                string allText = null;
                await Task.Run(() =>
                {
                    allText = File.ReadAllText(_databaseFile);
                });
                if (allText == null) return false;

                // Check if there is a line that starts with username
                allText = allText.Replace("\r", "").Replace("\t", "");
                var lines = allText.Split('\n');

                var sb = new StringBuilder();
                foreach (var str in lines)
                {
                    if (!str.StartsWith(user + " ", StringComparison.Ordinal))
                        sb.Append(str + "\n");
                }

                allText.Replace("\n", "\r\n");

                await Task.Run(() => { File.WriteAllText(_databaseFile, sb.ToString()); });

                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally { semaphoreSlim.Release(); }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<bool> MustChangePasswordAsync(string user)
        {
            user = user.Trim();
            if (!LoginHelper.CheckUserName(user))
            {
                return false;
            }
            if (!File.Exists(_databaseFile)) return false;
            await semaphoreSlim.WaitAsync();
            try
            {
                string allText = null;
                await Task.Run(() =>
                {
                    allText = File.ReadAllText(_databaseFile);
                });
                if (allText == null) return false;

                // Check if there is a line that starts with username
                allText = allText.Replace("\r", "").Replace("\t", "");
                var lines = allText.Split('\n');
                var line = lines.FirstOrDefault(l => l.StartsWith(user + " ", StringComparison.Ordinal));
                if (string.IsNullOrEmpty(line)) return false;
                var linespl = line.Split(' ');
                if (linespl.Length != 4) return false;
                if (linespl[0] != user) return false;
                return linespl[3].Trim() == "1";
            }
            catch (Exception)
            {
                return false;
            }
            finally { semaphoreSlim.Release(); }
        }

        public async Task<bool> Connect(string connectstring)
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(_databaseFile);
                if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "Could not create target directory");
                return false;
            }
            _databaseFile = connectstring;
            return true;
        }

        public void Disconnect()
        {

        }

        public void Dispose()
        {

        }
    }
}
