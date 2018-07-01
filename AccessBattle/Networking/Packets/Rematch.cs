namespace AccessBattle.Networking.Packets
{
    /// <summary>
    /// Rematch packet. Can be sent in GameOver phase when both players are still connected.
    /// </summary>
    public class Rematch
    {
        /// <summary>
        /// Unique id of the game.
        /// </summary>
        public uint UID { get; set; }
    }
}
