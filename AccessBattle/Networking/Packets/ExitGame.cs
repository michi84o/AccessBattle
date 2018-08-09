namespace AccessBattle.Networking.Packets
{
    /// <summary>Reason for exiting a game.</summary>
    public enum ExitGameReason
    {
        /// <summary>Player quit the game ('exit' during match or 'leave' after the match).</summary>
        PlayerQuit,
        /// <summary>Timeout due to inactivity.</summary>
        Inactivity,
        /// <summary>Player that created the game cancelled before another player could join.</summary>
        Cancelled,
    }

    /// <summary>
    /// Class for ExitGame events.
    /// </summary>
    public class ExitGameEventArgs
    {
        /// <summary>
        /// Reason for exit.
        /// </summary>
        public ExitGameReason Reason { get; }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="reason"></param>
        public ExitGameEventArgs(ExitGameReason reason)
        {
            Reason = reason;
        }
    }

    /// <summary>
    /// Packet sent when a game is about to be deleted from the server.
    /// Also used by players to remove a game they just created.
    /// </summary>
    public class ExitGame
    {
        /// <summary>
        /// Unique id of the game.
        /// </summary>
        public uint UID { get; set; }

        /// <summary>
        /// Reason for exit.
        /// </summary>
        public ExitGameReason Reason { get; set; }
    }
}
