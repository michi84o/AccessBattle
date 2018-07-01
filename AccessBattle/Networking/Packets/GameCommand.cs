namespace AccessBattle.Networking.Packets
{
    public class GameCommand
    {
        /// <summary>
        /// Unique id of the game.
        /// </summary>
        public uint UID { get; set; }

        /// <summary>
        /// If received by server: Command to play.
        /// If received by client: "OK" or "FAIL"
        /// </summary>
        public string Command { get; set; }
    }
}
