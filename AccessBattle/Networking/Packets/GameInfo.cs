namespace AccessBattle.Networking.Packets
{
    /// <summary>
    /// Game info packet.
    /// </summary>
    public class GameInfo
    {
        /// <summary>
        /// Unique id of the game.
        /// </summary>
        public uint UID { get; set; }

        /// <summary>
        /// Name of the game.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Name of the player that created the game.
        /// </summary>
        public string Player1 { get; set; }
    }
}
