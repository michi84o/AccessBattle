using System;
using System.Runtime.InteropServices;
using System.Security;

namespace AccessBattle
{
    /// <summary>Extensions for using strings and secure strings.</summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Converts secure string to unsecure string. Use with caution!
        /// </summary>
        /// <param name="securePassword"></param>
        /// <returns></returns>
        public static string ConvertToUnsecureString(this SecureString securePassword)
        {
            if (securePassword == null)
                return null;

            var unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        /// <summary>
        /// Converts string to secure string. Use with caution!
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static SecureString ConvertToSecureString(this string password)
        {
            var secStr = new SecureString();
            foreach (char c in password)
            {
                secStr.AppendChar(c);
            }
            return secStr;
        }
    }
}
