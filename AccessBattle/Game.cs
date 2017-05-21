using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle
{
    /// <summary>
    /// Indicator for the current game phase.
    /// </summary>
    public enum GamePhase
    {
        /// <summary>Game was just created and the second player has yet to join.</summary>
        WaitingForPlayers,
        /// <summary>A player is joining the game.</summary>
        PlayerJoining,
        /// <summary>In Init phase, the game state is reset and it is decided which player makes the first move.</summary>
        Init,
        /// <summary>Players deploy their cards in this phase.</summary>
        Deployment,
        /// <summary>Main game phase.</summary>
        PlayerTurns,
        /// <summary>Game is over. One of the players won.</summary>
        GameOver
    }

    /// <summary>
    /// Contains the complete state of a game.
    /// </summary>
    public class Game : PropChangeNotifier
    {
        GamePhase _phase;
        /// <summary>
        /// Current game phase.
        /// </summary>
        public GamePhase Phase
        {
            get { return _phase; }
            protected set { SetProp(ref _phase, value); }
        }

        PlayerState[] _players;
        /// <summary>
        /// Player related data.
        /// </summary>
        public PlayerState[] Players { get { return _players; } }


        /// <summary>
        /// Assumes that both players have properly joined the game.
        /// </summary>
        protected void InitGame()
        {
            Players[0].Did404NotFound = false;
            Players[0].DidVirusCheck = false;
            Players[1].Did404NotFound = false;
            Players[1].DidVirusCheck = false;

            // TODO: Reset Board and Cards

            Phase = GamePhase.Init;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public Game()
        {
            _players = new PlayerState[]
            {
                new PlayerState(1) { Name = "Player 1"  },
                new PlayerState(2) { Name = "Player 2"  }
            };
            _phase = GamePhase.WaitingForPlayers;
        }
    }
}
