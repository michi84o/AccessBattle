namespace AccessBattle.Networking.Packets
{
    /// <summary>
    /// Login packet.
    /// </summary>
    public class Login
    {
        /// <summary>Login name.</summary>
        public string Name { get; set; }
        /// <summary>Login password.</summary>
        public string Password { get; set; }
        /// <summary>Request creating a new account.</summary>
        public bool CreateAccount { get; set; }
    }
}