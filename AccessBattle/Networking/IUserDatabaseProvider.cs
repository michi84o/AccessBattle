using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AccessBattle.Networking
{
    public interface IUserDatabaseProvider
    {
        /// <summary>
        /// Must return false if user already exists in database.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        Task<bool> AddUserAsync(string user, SecureString password);
        /// <summary>
        /// Delete user.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<bool> DeleteUserAsync(string user);
        /// <summary>
        /// Checks if password is correct.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns>0: Login OK. 1: Invalid user name. 2: Invalid Password. 3: Database Error.</returns>
        Task<byte> CheckLoginAsync(string user, SecureString password);

        /// <summary>
        /// Checks if a user must change his password on next login.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<bool> MustChangePasswordAsync(string user);
    }

    /// <summary>
    /// Simple text file based user database.
    /// Format: Username Password Hash Salt MustChangePassword(0/1)
    /// Usernames can not have space in it.
    /// </summary>
    /// <remarks>
    /// This class is not optimized for a large amount of users since
    /// it completely reads and rewrites the file.
    /// </remarks>
    public class TextFileUserDatabaseProvider : IUserDatabaseProvider
    {
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        readonly string _databaseFile;
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="databaseFile"></param>
        public TextFileUserDatabaseProvider(string databaseFile)
        {
            _databaseFile = databaseFile;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<bool> AddUserAsync(string user, SecureString password)
        {
            user = user.Trim();
            if (user.Any(c => c == ' ')) return false;
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

        const byte LoginOK = 0;
        const byte InvalidUser = 1;
        const byte InvalidPassword = 2;
        const byte DatabaseError = 3;

        /// <summary>
        ///
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns>0: Login OK. 1: Invalid user name. 2: Invalid Password. 3: Database Error.</returns>
        public async Task<byte> CheckLoginAsync(string user, SecureString password)
        {
            user = user.Trim();
            if (user.Any(c => c == ' ')) return InvalidUser;
            if (!File.Exists(_databaseFile))
            {
                Log.WriteLine(LogPriority.Error, "Error in text file user database: File does not exist!");
                return DatabaseError;
            }
            await semaphoreSlim.WaitAsync();
            try
            {
                string allText = null;
                await Task.Run(() =>
                {
                    allText = File.ReadAllText(_databaseFile);
                });
                if (allText == null) return DatabaseError;

                // Check if there is a line that starts with username
                allText = allText.Replace("\r", "").Replace("\t", "");
                var lines = allText.Split('\n');
                var line = lines.FirstOrDefault(l => l.StartsWith(user + " ", StringComparison.Ordinal));
                if (string.IsNullOrEmpty(line)) return InvalidUser;
                var linespl = line.Split(' ');
                if (linespl.Length != 4) return DatabaseError;
                if (linespl[0] != user) return DatabaseError;
                if (PasswordHasher.VerifyHash(password.ConvertToUnsecureString(), linespl[1], linespl[2])) return LoginOK;
                return InvalidPassword;
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Error, "Error in text file user database: " + e);
                return DatabaseError;
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
            user = user.Trim();
            if (user.Any(c => c == ' ')) return false;
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
            if (user.Any(c => c == ' ')) return false;
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
    }
}
