namespace AccessBattle
{
    /// <summary>
    /// Interface for local, network or AI players.
    /// </summary>
    public interface IPlayer
    {
        /// <summary>ID of the player (mainly used for server).</summary>
        uint UID { get; }
        /// <summary>Name of the player.</summary>
        string Name { get; set; }

        /// <summary>
        /// Notify the player to make his move.
        /// </summary>
        void PlayTurn();
    }
}
