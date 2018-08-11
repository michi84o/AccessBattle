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
    /// Format: "Username" "ELO" "Password Hash" "Salt" "MustChangePassword(0/1)"
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

        /// <summary>Hint for the connection string.</summary>
        public string ConnectStringHint => "Enter a file name for a text file";

        /// <summary>
        ///
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="elo">ELO rating. Default: 1000</param>
        /// <returns></returns>
        public async Task<bool> AddUserAsync(string user, SecureString password, int elo = 1000)
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

                allText = allText.Replace("\r", "").Replace("\t", "");
                var lines = allText.Split('\n').ToList();

                // Check if there is a line that starts with username
                if (lines.Count > 0)
                {
                    int i = 0;
                    string check = user + " ";
                    while (true)
                    {
                        if (lines.Count >= i) break;
                        if (lines[i].StartsWith(check))
                            lines.RemoveAt(i);
                        else ++i;
                    }
                }

                StringBuilder sb = new StringBuilder();
                foreach (var line in lines)
                {
                    if (line == "") continue;
                    sb.Append(line + "\r\n");
                }

                string hash, salt;
                if (!PasswordHasher.GetNewHash(password.ConvertToUnsecureString(), out hash, out salt)) return false;

                if (elo < 0) elo = 0;
                if (elo > 10000) elo = 10000;

                var newLine = user + " "+ elo +" " + hash + " " + salt + " 1";
                sb.Append(newLine + "\r\n");

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
                await Task.Run(() => // TODO: StreamReader async interface
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
                if (linespl.Length != 5) return LoginCheckResult.DatabaseError;
                if (linespl[0] != user) return LoginCheckResult.DatabaseError;

                if (PasswordHasher.VerifyHash(password.ConvertToUnsecureString(), linespl[2], linespl[3]))
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
        public async Task<bool?> MustChangePasswordAsync(string user)
        {
            user = user.Trim();
            if (!LoginHelper.CheckUserName(user))
            {
                return null;
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
                if (allText == null) return null;

                // Check if there is a line that starts with username
                allText = allText.Replace("\r", "").Replace("\t", "");
                var lines = allText.Split('\n');
                var line = lines.FirstOrDefault(l => l.StartsWith(user + " ", StringComparison.Ordinal));
                if (string.IsNullOrEmpty(line)) return null;
                var linespl = line.Split(' ');
                if (linespl.Length != 4) return null;
                if (linespl[0] != user) return null;
                return linespl[3].Trim() == "1";
            }
            catch (Exception)
            {
                return null;
            }
            finally { semaphoreSlim.Release(); }
        }

        /// <summary>
        /// Opens a database file.
        /// </summary>
        /// <param name="connectstring">Database file to use.</param>
        /// <returns></returns>
        public async Task<bool> Connect(string connectstring)
        {
            try
            {
                await Task.Yield(); // Force async
                var dir = System.IO.Path.GetDirectoryName(_databaseFile);
                if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "Could not create target directory (" + e.Message + ")");
                return false;
            }
            _databaseFile = connectstring;
            return true;
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public void Disconnect()
        {

        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public void Dispose()
        {

        }

        /// <summary>
        /// Gets the ELO value for this player.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<int?> GetELO(string user)
        {
            if (_databaseFile == null) return null;
            if (!File.Exists(_databaseFile))
            {
                Log.WriteLine(LogPriority.Error, "Error in text file user database: File does not exist!");
                return null;
            }
            await semaphoreSlim.WaitAsync();
            try
            {
                string allText = null;
                await Task.Run(() => // TODO: StreamReader async interface
                {
                    allText = File.ReadAllText(_databaseFile);
                });
                if (allText == null) return null;

                allText = allText.Replace("\r", "").Replace("\t", "");
                var lines = allText.Split('\n');
                var line = lines.FirstOrDefault(l => l.StartsWith(user + " ", StringComparison.Ordinal));

                if (string.IsNullOrEmpty(line)) return null;
                var linespl = line.Split(' ');

                int elo;
                if (!int.TryParse(linespl[1], out elo))return null;

                return elo;
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "Error in text file user database: " + e);
                return -1;
            }
            finally { semaphoreSlim.Release(); }
        }

        public async Task<bool> SetELO(string user, int elo)
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
                    if (str.StartsWith(user + " ", StringComparison.Ordinal))
                    {
                        // Found the line. Update ELO value
                        // ELO is second column
                        var spl = str.Split(' ');
                        for (int i = 0; i < spl.Length; ++i)
                        {
                            if (i == 1)
                                sb.Append(elo);
                            else
                                sb.Append(spl[i]);

                            if (i < spl.Length - 1)
                                sb.Append(" ");
                        }
                        sb.Append("\n");
                    }
                    else
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


    }
}
