namespace AccessBattle
{
    public interface IBoardGame
    {
        PlayerState[] Players { get; }
        BoardField[,] Board { get; }
        GamePhase Phase { get;}
    }
}
