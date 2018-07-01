using System;
using System.Globalization;
using System.Text;

namespace AccessBattle.Networking
{
    /// <summary>
    /// Warning this password hashing function is not safe
    /// and should not be used in security critical applications.
    /// For the moment its good enough for this simple game.
    /// </summary>
    public static class PasswordHasher
    {
        /// <summary>
        /// Generates hash and salt for a new password entry.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="hash"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        public static bool GetNewHash(string password, out string hash, out string salt)
        {
            // TODO: Use PBKDF2 instead of sha256
            // TODO: The restriction with the ASCII characters in the game makes logins unsafe
            hash = null;
            salt = null;

            try
            {
                var saltBytes = new byte[32];
                using (var provider = new System.Security.Cryptography.RNGCryptoServiceProvider())
                {
                    provider.GetBytes(saltBytes);
                }

                var pwBytes = Encoding.UTF8.GetBytes(password);
                var allBytes = new byte[pwBytes.Length + saltBytes.Length];
                Array.Copy(pwBytes, allBytes, pwBytes.Length);
                Array.Copy(saltBytes, 0, allBytes, pwBytes.Length, saltBytes.Length);

                byte[] sHash;
                using (var sha = new System.Security.Cryptography.SHA256Managed())
                {
                    sHash = sha.ComputeHash(allBytes);
                }

                salt = BitConverter.ToString(saltBytes).Replace("-","");
                hash = BitConverter.ToString(sHash).Replace("-", "");

                if (salt.Length != 64 || hash.Length != 64) return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Verify a password using a password entry.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="hash"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        public static bool VerifyHash(string password, string hash, string salt)
        {
            if (salt?.Length != 64 || hash?.Length != 64) return false;
            try
            {
                var hashBytes = new byte[32];
                var saltBytes = new byte[32];
                for (int i = 0; i < 64; i += 2)
                {
                    hashBytes[i / 2] = byte.Parse(hash.Substring(i, 2), NumberStyles.HexNumber);
                    saltBytes[i / 2] = byte.Parse(salt.Substring(i, 2), NumberStyles.HexNumber);
                }

                var pwBytes = Encoding.UTF8.GetBytes(password);
                var allBytes = new byte[pwBytes.Length + saltBytes.Length];
                Array.Copy(pwBytes, allBytes, pwBytes.Length);
                Array.Copy(saltBytes, 0, allBytes, pwBytes.Length, saltBytes.Length);

                byte[] calcHash;
                using (var sha = new System.Security.Cryptography.SHA256Managed())
                {
                    calcHash = sha.ComputeHash(allBytes);
                }

                for (int i = 0; i < 32; ++i)
                    if (calcHash[i] != hashBytes[i]) return false;

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

    }
}
