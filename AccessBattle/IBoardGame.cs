namespace AccessBattle
{
    /// <summary>Interface for board games.</summary>
    public interface IBoardGame
    {
        /// <summary>Player list. Should always have a length of 2.</summary>
        PlayerState[] Players { get; }
        /// <summary>Board fields. Board is 8x8 + additional fields in y dimension.</summary>
        BoardField[,] Board { get; }
        /// <summary>Current game phase.</summary>
        GamePhase Phase { get;}
    }
}
