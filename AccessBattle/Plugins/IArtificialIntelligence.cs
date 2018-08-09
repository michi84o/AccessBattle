using AccessBattle.Networking.Packets;

namespace AccessBattle.Plugins
{
    /// <summary>Factory for AI instances.</summary>
    public interface IArtificialIntelligenceFactory : IPlugin
    {
        /// <summary>Creates an AI instance.</summary>
        IArtificialIntelligence CreateInstance();
    }

    /// <summary>Interface for AI players.</summary>
    public interface IArtificialIntelligence : IPlayer
    {
        /// <summary>
        /// Determines the side this AI is playing on
        /// (can later be used to let AIs play against each other).
        /// When true, AI is player 1.
        /// </summary>
        bool IsAiHost { get; set; }

        /// <summary>Tells AI to create a command for its next move.</summary>
        string PlayTurn();

        /// <summary>Tells the AI the current state of the game.</summary>
        void Synchronize(GameSync sync);
    }
}
