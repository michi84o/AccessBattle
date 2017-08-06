﻿using System;
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
        /// <summary>Main game phase. Player 1 turn.</summary>
        Player1Turn,
        /// <summary>Main game phase. Player 2 turn.</summary>
        Player2Turn,
        /// <summary>Game is over. Player 1 won.</summary>
        Player1Win,
        /// <summary>Game is over. Player 2 won.</summary>
        Player2Win,
        /// <summary>Game was aborted. No winner.</summary>
        Aborted
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
        public void InitGame()
        {
            Players[0].Did404NotFound = false;
            Players[0].DidVirusCheck = false;
            Players[1].Did404NotFound = false;
            Players[1].DidVirusCheck = false;

            // Reset Board
            for (int x = 0; x < 8; ++x)
                for (int y = 0; y < 10; ++y)
                    Board[x, y].Card = null;

            // Place cards on stack for deployment
            // Half of the cards are link, other half are virus
            for (int p = 0; p < 2; ++p)
            {
                for (int c = 0; c < 4; ++c)
                {
                    PlayerOnlineCards[p, c].Type = OnlineCardType.Link;
                    PlayerOnlineCards[p, c].IsFaceUp = false;
                    Board[c, 8 + p].Card = PlayerOnlineCards[p, c];
                }
                for (int c = 4; c < 8; ++c)
                {
                    PlayerOnlineCards[p, c].Type = OnlineCardType.Virus;
                    PlayerOnlineCards[p, c].IsFaceUp = false;
                    Board[c, 8 + p].Card = PlayerOnlineCards[p, c];
                }
            }

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
