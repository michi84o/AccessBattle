using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Board is 8x8 using Chess notation
// as base for position indexes
//   a b c d e f g h
// 8                 8  7
// 7                 7  6
// ...               ...
// 2                 2  1
// 1                 1  0
//   a b c d e f g h
//   0 1 2 3 4 5 6 7
//
// X is horizontal, Y is vertical
// (0,0) is a1, (7,7) is h8
//
// Stack:
// For simplification,
// the first 4 fields are always links.
// Board orientation is ignored
// Player1: Fields (0,8) - (7,8)
// Player2: Fields (0,9) - (7,9)
//
// Additional fields:
// Player1 Server Area : Field (4,10)
// Player2 Server Area : Field (5,10)

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

        int _winningPlayer;
        /// <summary>
        /// If game phase is GameOver then this value shows which player won
        /// </summary>
        public int WinningPlayer
        {
            get { return _winningPlayer; }
            set
            {
                SetProp(ref _winningPlayer, value);
            }
        }

        PlayerState[] _players;
        /// <summary>
        /// Player related data.
        /// </summary>
        public PlayerState[] Players { get { return _players; } }

        /// <summary>
        /// Represents all fields of the game board.
        /// </summary>
        public BoardField[,] Board { get; private set; }

        /// <summary>
        /// Online cards of players. First index is player, second index is card.
        /// Each player has 8 online cards.
        /// </summary>
        public OnlineCard[,] PlayerOnlineCards { get; private set; }

        /// <summary>
        /// Firewall cards of players. Index is player index.
        /// </summary>
        public FirewallCard[] PlayerFirewallCards { get; private set; }

        /// <summary>
        /// Assumes that both players have properly joined the game.
        /// </summary>
        protected void InitGame()
        {
            Players[0].Did404NotFound = false;
            Players[0].DidVirusCheck = false;
            Players[1].Did404NotFound = false;
            Players[1].DidVirusCheck = false;

            for (int p = 0; p < 2; ++p)
                for (int c = 0; c < 8; ++c)
                {
                    PlayerOnlineCards[p, c].Type = OnlineCardType.Unknown;
                }

            // Reset Board
            for (int x = 0; x < 8; ++x)
                for (int y = 0; y < 10; ++y)
                    Board[x, y].Card = null;

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

            Board = new BoardField[8, 11];
            for (ushort y = 0; y < 11; ++y)
                for (ushort x = 0; x < 8; ++x)
                {
                    Board[x, y] = new BoardField(x, y);
                }

            PlayerOnlineCards = new OnlineCard[2, 8];
            for (int p = 0; p < 2; ++p)
                for (int c = 0; c < 8; ++c)
                    PlayerOnlineCards[p, c] = new OnlineCard { Owner = _players[p] };

            PlayerFirewallCards = new FirewallCard[2]
            {
                new FirewallCard { Owner = _players[0] },
                new FirewallCard { Owner = _players[1] },
            };
        }
    }
}
