using AccessBattle.Networking;
using System;
using System.Security;
using System.Threading.Tasks;

namespace AccessBattle.Plugins
{
    public interface IUserDatabaseProviderFactory : IPlugin
    {
        IUserDatabaseProvider CreateInstance();
    }

    public interface IUserDatabaseProvider : IDisposable
    {
        /// <summary>
        /// Must return false if user already exists in database.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="elo">ELO rating. Default: 1000, Min: 0, Max: 10000</param>
        /// <returns></returns>
        Task<bool> AddUserAsync(string user, SecureString password, int elo);
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
        Task<LoginCheckResult> CheckLoginAsync(string user, SecureString password);
        /// <summary>
        /// Checks if a user must change his password on next login.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<bool> MustChangePasswordAsync(string user);
        /// <summary>
        /// Establishes the connection to the database if required.
        /// This might cause a promt for login information.
        /// </summary>
        /// <returns></returns>
        Task<bool> Connect(string connectstring);
        /// <summary>
        /// Disconnects from the database.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Gets the ELO rating.
        /// </summary>
        /// <returns></returns>
        Task<int> GetELO(string user);

        /// <summary>
        /// A humand readable text that lets the user know what to use as connection string.
        /// </summary>
        string ConnectStringHint { get; }
    }
}
