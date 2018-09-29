namespace AccessBattle.Networking.Packets
{
    /// <summary>
    /// Information about the server.
    /// This packet is always sent unencrypted.
    /// </summary>
    public class ServerInfo
    {
        /// <summary>
        /// True if a login with username and password is required.
        /// If false any user name can be used and password is not required.
        /// </summary>
        public bool RequiresLogin { get; private set; }

        /// <summary>
        /// True if clients are allowed to create an account.
        /// </summary>
        public bool AllowsRegistration { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="requiresLogin"></param>
        public ServerInfo(bool requiresLogin, bool allowsRegistration)
        {
            RequiresLogin = requiresLogin;
            AllowsRegistration = allowsRegistration;
        }
    }
}
