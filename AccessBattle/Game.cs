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
            private set
            {
                SetProp(_phase, value, () =>
                {
                     _phase = value;
                     OnPhaseChanged(); // Should be done before prop change event fires
                });
            }
        }

        PlayerState[] _players;
        /// <summary>
        /// Player related data.
        /// </summary>
        public PlayerState[] Players { get { return _players; } }

        private object _locker = new object();

        public bool JoinPlayer(IPlayer player)
        {
            var result = false;
            lock (_locker)
            {
                if (Phase == GamePhase.WaitingForPlayers)
                {
                    Phase = GamePhase.PlayerJoining;
                    result = true;
                }
            }
            return result;
        }

        uint _uid;
        /// <summary>
        /// This game's ID on the server.
        /// </summary>
        public uint UID { get { return _uid; } }

        string _name;
        /// <summary>
        /// This game's name.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { SetProp(ref _name, value); }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="uid">ID of this game (mainly used for server).</param>
        public Game(uint uid = 0)
        {
            _uid = uid;
            _players = new PlayerState[]
            {
                new PlayerState(1) { Name = "Player 1"  },
                new PlayerState(2) { Name = "Player 2"  }
            };
            _phase = GamePhase.WaitingForPlayers;
            OnPhaseChanged();
        }

        void OnPhaseChanged()
        {

        }
    }
}
