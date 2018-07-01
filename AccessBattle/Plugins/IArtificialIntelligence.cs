using AccessBattle.Networking.Packets;

namespace AccessBattle.Plugins
{
    public interface IArtificialIntelligenceFactory : IPlugin
    {
        IArtificialIntelligence CreateInstance();
    }

    public interface IArtificialIntelligence : IPlayer
    {
        /// <summary>
        /// Determines the side this AI is playing on
        /// (can later be used to let AIs play against each other).
        /// When true, AI is player 1.
        /// </summary>
        bool IsAiHost { get; set; }

        string PlayTurn();
        void Synchronize(GameSync sync);
    }
}
