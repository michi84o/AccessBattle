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
    public class Game : PropChangeNotifier, IBoardGame
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
            Phase = GamePhase.Deployment;
        }

        /// <summary>
        /// Determines which player moves first and starts the player turn phase.
        /// </summary>
        public void BeginTurns()
        {
            // Determine which player starts
            var rnd = new Random();
            var num = rnd.Next(1, 3);
            if (num == 1)
                Phase = GamePhase.Player1Turn;
            else
                Phase = GamePhase.Player2Turn;
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

        public bool ExecuteCommand(string command, int player)
        {
            if (player != 1 && player != 2) return false;
            if (string.IsNullOrEmpty(command)) return false;

            var cmd = command.Trim();

            #region Deploy Command "dp"
            // Deployment command is a command that contains 4 'L' and 4 'V' characters.
            // It corresponds to the 8 cards that will be deployed. The card positions are
            // counted from left to right on the deployment line.
            // The command is "dp". Example:
            // "dp LLVLVVVL"
            if (cmd.StartsWith("dp ", StringComparison.InvariantCultureIgnoreCase))
            {
                if (Phase != GamePhase.Deployment) return false;
                if (cmd.Length < 11) return false;
                cmd = cmd.Substring(3).Trim();
                if (cmd.Length != 8) return false;
                // Verify string
                int linkCount = 0;
                int virusCount = 0;
                var layout = new OnlineCardType[8];
                for (int i=0; i<8; ++i)
                {
                    if (cmd[i] == 'L' || cmd[i] == 'l')
                    {
                        ++linkCount;
                        layout[i] = OnlineCardType.Link;
                    }
                    else if (cmd[i] == 'V' || cmd[i] == 'v')
                    {
                        ++virusCount;
                        layout[i] = OnlineCardType.Virus;
                    }
                }
                if (linkCount != 4 || virusCount != 4) return false;
                linkCount = 0;
                virusCount = 0;
                var y1 = player == 1 ? 0 : 7;
                var y2 = player == 1 ? 1 : 6;
                for (int x = 0; x < 8; ++x)
                {
                    // Clear stack.
                    Board[x, 8 + player].Card = null;
                    // Place card
                    int y = (x == 3 || x == 4) ? y2 : y1;
                    int offset = layout[x] == OnlineCardType.Link ? linkCount++ : (4 + virusCount++);
                    Board[x, y].Card = PlayerOnlineCards[player, offset];
                }
                return true;
            }
            #endregion


            return false;
        }

        #region Static Helper Methods

        /// <summary>
        /// Gets all fields of the given board that the card on the given board field can be moved to.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static List<BoardField> GetMoveTargetFields(IBoardGame game, BoardField field)
        {
            var fields = new List<BoardField>();
            var card = field?.Card as OnlineCard; // Firewall card cannot be moved directly to another field
            if (card == null || card.Owner == null) return fields;
            // No move possible if it is the turn of the other player
            if (card.Owner.PlayerNumber == 1 && game.Phase == GamePhase.Player2Turn ||
                card.Owner.PlayerNumber == 2 && game.Phase == GamePhase.Player1Turn)
                return fields;

            var x = field.X;
            var y = field.Y;

            if (game.Phase == GamePhase.Deployment)
            {
                // In this phase you can only move cards between stack and deployment fields.
                var owner = card.Owner.PlayerNumber;
                var depFields = new BoardField[16];
                var y1 = owner == 1 ? 0 : 7;
                var y2 = owner == 1 ? 1 : 6;
                var y3 = owner == 1 ? 8 : 9;
                for (int ix = 0; ix<=7; ++ix)
                {
                    // Set deployment fields
                    if (ix == 3 || ix == 4)
                        depFields[ix] = game.Board[ix, y2];
                    else
                        depFields[ix] = game.Board[ix, y1];
                    // Set stack fields
                    depFields[ix+8] = game.Board[ix, y3];
                }
                // Select all fields that have no card
                fields.AddRange(depFields.Where(o => o.Card == null));
            }
            else if (game.Phase == GamePhase.Player1Turn || game.Phase == GamePhase.Player2Turn)
            {
                var currentPlayer = game.Phase == GamePhase.Player1Turn ? 1 : 2;

                // without boost there are 4 possible locations
                var fs = new BoardField[4];
                if (y > 0 && y <= 7) // Down
                    fs[0] = game.Board[x, y - 1];
                else
                {
                    // Special case for exit fields:
                    if (y == 0 && field.IsExit && currentPlayer == 2)
                        fs[0] = game.Board[4, 10];
                }

                if (y >= 0 && y < 7) // Up
                    fs[1] = game.Board[x, y + 1];
                else
                {
                    // Special case for exit fields:
                    if (y == 7 && field.IsExit && currentPlayer == 1)
                        fs[1] = game.Board[5, 10];
                }

                if (x > 0 && x <= 7 && y <= 7) // Left;  Mask out stack fields
                    fs[2] = game.Board[x - 1, y];
                if (x >= 0 && x < 7 && y <= 7) // Right
                    fs[3] = game.Board[x + 1, y];

                // Moves on opponents cards are allowed, move on own card not
                for (int i = 0; i < 4; ++i)
                {
                    if (fs[i] == null) continue;
                    if (fs[i].Card != null && fs[i].Card.Owner.PlayerNumber == currentPlayer) continue;
                    if (fs[i].Card != null && !(fs[i].Card is OnlineCard)) continue; // Can only jump on online cards <- This ignores the firewall card
                    if (fs[i].IsStack) continue;
                    if (fs[i].IsExit)
                    {
                        // Exit field is allowed, but only your opponents
                        if (currentPlayer == 1 && fs[i].Y == 0) continue;
                        if (currentPlayer == 2 && fs[i].Y == 7) continue;
                    }
                    fields.Add(fs[i]);
                }

                // Boost can add additional fields
                if (card.HasBoost)
                {
                    // The same checks as above apply for all fields
                    var additionalfields = new List<BoardField>();
                    foreach (var f in fields)
                    {
                        // Ignore field if it has an opponents card
                        if (f.Card != null) continue;

                        fs = new BoardField[4];
                        x = f.X;
                        y = f.Y;
                        if (y > 0 && y <= 7)
                            fs[0] = game.Board[x, y - 1];
                        else
                        {
                            // Special case for exit fields:
                            if (y == 0 && f.IsExit && currentPlayer == 2)
                                fs[1] = game.Board[4, 10];
                        }

                        if (y >= 0 && y < 7)
                            fs[1] = game.Board[x, y + 1];
                        else
                        {
                            // Special case for exit fields:
                            if (y == 7 && f.IsExit && currentPlayer == 1)
                                fs[1] = game.Board[5, 10];
                        }

                        if (x > 0 && x <= 7 && y <= 7) // No Stack fields!
                            fs[2] = game.Board[x - 1, y];
                        if (x >= 0 && x < 7 && y <= 7) // No Stack fields!
                            fs[3] = game.Board[x + 1, y];

                        for (int i = 0; i < 4; ++i)
                        {
                            if (fs[i] == null) continue;
                            if (fs[i].Card != null && fs[i].Card.Owner.PlayerNumber == currentPlayer) continue;
                            if (fs[i].IsStack) continue;
                            if (fs[i].IsExit)
                            {
                                // Exit field is allowed, but only your opponents
                                if (currentPlayer == 1 && fs[i].Y == 0) continue;
                                if (currentPlayer == 2 && fs[i].Y == 7) continue;
                            }
                            additionalfields.Add(fs[i]);
                        }
                    }

                    foreach (var f in additionalfields)
                    {
                        if (!fields.Contains(f))
                            fields.Add(f);
                    }
                }

            }

            return fields;
        }

        #endregion
    }
}
