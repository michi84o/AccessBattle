using System.Text.RegularExpressions;

namespace AccessBattle.Networking
{
    /// <summary>
    /// Login result.
    /// </summary>
    public enum LoginCheckResult
    {
        /// <summary>Default value. Not used.</summary>
        Unknown = 0,
        /// <summary>Login successful.</summary>
        LoginOK,
        /// <summary>Invalid user name.</summary>
        InvalidUser,
        /// <summary>Invalid password.</summary>
        InvalidPassword,
        /// <summary>Database error.</summary>
        DatabaseError,
    }

    /// <summary>Helper for login names.</summary>
    public static class LoginHelper
    {
        /// <summary>Checks if a user name is valid.</summary>
        public static bool CheckUserName(string user)
        {
            if (user.Length > 32)
            {
                Log.WriteLine(LogPriority.Error, "Error: User names are limited to 32 characters!");
                return false;
            }
            if (!Regex.IsMatch(user, @"^[\x21-\x7E]+$"))
            {
                Log.WriteLine(LogPriority.Error, "Error: Invalid character in user name! (space is not allowed)");
                return false;
            }
            return true;
        }
    }




}
