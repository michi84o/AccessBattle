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
    }
}