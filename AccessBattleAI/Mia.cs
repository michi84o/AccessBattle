using AccessBattle;
using AccessBattle.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattleAI
{
    [Export(typeof(IPlugin))]
    [ExportMetadata("Name", "AccessBattle.AI.Mia")]
    [ExportMetadata("Description", "Mia")]
    [ExportMetadata("Version", "0.3")]
    public class MiaFactory : IArtificialIntelligenceFactory
    {
        public IPluginMetadata Metadata { get; set; }

        public IArtificialIntelligence CreateInstance() => new Mia();
    }

    /// <summary>
    /// Implementation of Mia.
    /// </summary>
    public class Mia : AiBase
    {
        protected override string _name => "Mia (heuristic)";

        Random _rnd = new Random();
        /// <summary>
        /// Used for debugging. Changes seed of RNG.
        /// </summary>
        /// <param name="seed"></param>
        public void SetSeed(int seed)
        {
            _rnd = new Random(seed);
        }

        int _depth = 2;
        /// <summary>
        /// Depth to use for board state prediction.
        /// A depth of 2 is default.
        /// Allowed range: 1-5.
        /// </summary>
        public int Depth
        {
            get => _depth;
            set
            {
                if (value > 5) _depth = 5;
                else if (value < 1) _depth = 1;
                else _depth = value;
            }
        }

        class GameState : IBoardGame
        {
            public double LocalScore = -300;

            public PlayerState[] Players { get; private set; }

            public BoardField[,] Board { get; private set; }

            public GamePhase Phase { get; set; }

            public List<BoardField>[] OnlineCardFields { get; private set; } = new List<BoardField>[] { new List<BoardField>(), new List<BoardField>() };
            public BoardField[] FirewallFields { get; } = new BoardField[2];

            public List<GameState> NextStates { get; private set; } = new List<GameState>();

            public string Move { get; set; }

            /// <summary>
            /// Create a copy from the current board
            /// </summary>
            /// <param name="board"></param>
            /// <returns></returns>
            public static GameState GameStateFrom(IBoardGame game)
            {
                var state = new GameState
                {
                    Players = new PlayerState[] { game.Players[0], game.Players[1] },
                    Phase = game.Phase,
                    Board = new BoardField[8,11]
                };
                for (ushort x = 0; x < 8; ++x)
                    for (ushort y = 0; y < 11; ++y)
                    {
                        state.Board[x, y] = new BoardField(x, y);
                        var card = game.Board[x, y].Card;
                        var ocard = card as OnlineCard;
                        var fcard = card as FirewallCard;

                        if (ocard != null)
                        {
                            state.Board[x, y].Card = new OnlineCard
                            {
                                HasBoost = ocard.HasBoost,
                                IsFaceUp = ocard.IsFaceUp,
                                Owner = ocard.Owner,
                                Type = ocard.Type
                            };
                            state.OnlineCardFields[ocard.Owner.PlayerNumber - 1].Add(state.Board[x, y]);
                        }
                        else if (fcard != null)
                        {
                            state.Board[x, y].Card = new FirewallCard
                            {
                                Owner = fcard.Owner
                            };
                            state.FirewallFields[fcard.Owner.PlayerNumber - 1] = state.Board[x, y];
                        }
                    }
                return state;
            }
        }

        bool _hasPlayedVirusCheck = false;
        public override string PlayTurn()
        {
            // Strategy:
            // Try all combination of moves for the next 2 turns
            // To reduce ram usage only the 6 best subcombinations are used.
            // Score calculation:
            //   - Distance to exit: Link
            //   - Distance to opponent cards: Link
            //   - Distance to opponent cards: Virus

            if (Phase == GamePhase.Deployment)
                return Deploy();

            // Base class converts state of game so AI is always player 1
            if (Phase != GamePhase.Player1Turn) return "???";

            // 10% chance of playing a virus check
            if (!_hasPlayedVirusCheck && _rnd.NextDouble() <= .15)
            {
                var theirCards = TheirOnlineCards.Where(o=> (o.Card as OnlineCard)?.IsFaceUp == false).ToList();
                if (theirCards.Count > 0)
                {
                    _hasPlayedVirusCheck = true;
                    return PlayVirusCheck(theirCards[_rnd.Next(0, theirCards.Count)]);
                }
            }

            // 20% chance of playing a boost
            if (_rnd.NextDouble() <= 0.2)
            {
                // Check if boost is in use
                if (!MyLinkCards.Any(o => (o.Card as OnlineCard)?.HasBoost == true) &&
                    !MyVirusCards.Any(o => (o.Card as OnlineCard)?.HasBoost == true))
                {

                    List<BoardField> cards;
                    if (_rnd.NextDouble() >= .67) // 33% chance of boosting a virus
                        cards = MyVirusCards;
                    else
                        cards = MyLinkCards;

                    // Check if boost was already played
                    if (cards.Count > 0)
                    {
                        return PlayBoost(cards[_rnd.Next(0, cards.Count)], true);
                    }
                }

            }

            // Create a list of all possible combinations
            var cState = GameState.GameStateFrom(this);
            int variations = 0;
            GetNextStates(cState, _depth, ref variations);

            double score = 0;
            GameState bestState = null;
            foreach (var s in cState.NextStates)
            {
                var sc = CalculateScore(s);
                if (sc > score)
                {
                    score = sc;
                    bestState = s;
                }
            }

            return bestState.Move;
        }

        void CalculateLocalStore(GameState state)
        {
            if (state.LocalScore > -299) return; // Already calculated

            // Score the current state:
            double score = 0;
            // Link cards that reached server give 15 points.
            // Link cards that were capture give -10 points
            // Give 10 - 'Distance to Exit' for all otther Link cards.
            foreach (var field in state.OnlineCardFields[0]
                .Where(o => (o.Card as OnlineCard)?.Type == OnlineCardType.Link))
            {
                if (field.Y == 8) // Card reached server
                {
                    score += 15;
                }
                else if (field.Y == 9) // Card was captured
                {
                    score -= 10;
                }
                else if (field.Y < 8) // Card is on the field
                {
                    // Distance to exit
                    score += 10 - DistanceToExit(field);
                }
            } // Best score = 60

            // Virus cards should not enter exit field or server.
            // Give -20 if exit field is entered, -25 for server.
            // Give +10 if bait worked and opponent captured virus.
            // Give 10 - 'Min dist to opponent' for cards on the field.
            foreach (var field in state.OnlineCardFields[0]
                .Where(o => (o.Card as OnlineCard)?.Type == OnlineCardType.Virus))
            {
                if (field.Y == 8) // Card reached server
                {
                    score -= 25;
                }
                else if (field.Y == 9) // Captured by opponent
                {
                    score += 10;
                }
                else
                {
                    // Calculate min distance to opponent cards
                    double dMin = 7;
                    foreach (var c in TheirOnlineCards)
                    {
                        var dst = Distance(field.X, field.Y, c.X, c.Y);
                        if (dst < dMin) dst = dMin;
                    }
                    score += 10 - dMin;
                }
            } // Best score = 40 (only of opponent is stupid enough)

            // Give +5 bonus for opponent link cards that have been captured.
            // Give -10 penalty for opponent virus cards that have been captured.
            for (int i = 0; i < 8; ++i)
            {
                var card = state.Board[i, 8].Card as OnlineCard;
                // Ignore own cards. We scored them already
                if (card == null || card.Owner.PlayerNumber == 1) continue;

                // Don't add too much. AI might catch cards by accident
                if (card.Type == OnlineCardType.Link) score += 5;
                // Give higher penalty for capturing virus cards
                else if (card.Type == OnlineCardType.Virus) score -= 10;
                // Unknown cards give the sum of both
                else score -= 5;
            } // Best score = 20

            // Theoretical best score: 120 (cannot be reached within a normal game)
            state.LocalScore = score;
        }

        double CalculateScore(GameState state)
        {
            CalculateLocalStore(state);

            var score = state.LocalScore;

            foreach (var s in state.NextStates)
            {
                var score2 = CalculateScore(s);
                if (score2 > score) score = score2;
            }
            return score;
        }

        double DistanceToExit(BoardField field)
        {
            return Distance(field.X, field.Y, field.X < 4 ? 3 : 4, 7);
        }

        double Distance(int x0, int y0, int x1, int y1)
        {
            var dx = x1 - x0;
            var dy = y1 - y0;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        void PutOnStack(GameState state, BoardField field, int playerIndex)
        {
            // Capture Card to own stack
            for (int i = 0; i < 8; ++i)
            {
                if (state.Board[i, 8+playerIndex].Card == null)
                {
                    state.Board[i, 8 + playerIndex].Card = field.Card;

                    // Fix OnlineCardFields array
                    var index = state.OnlineCardFields[field.Card.Owner.PlayerNumber - 1].FindIndex(o => o.X == field.X && o.Y == field.Y);
                    state.OnlineCardFields[field.Card.Owner.PlayerNumber - 1][index] = state.Board[i, 8 + playerIndex];

                    field.Card = null;
                    break;
                }
            }
        }

        void MoveCard(GameState state, BoardField from, BoardField to)
        {
            if (state.Board[to.X, to.Y].Card != null)
                PutOnStack(state, state.Board[to.X, to.Y], from.Card.Owner.PlayerNumber - 1);

            state.Board[to.X, to.Y].Card = state.Board[from.X, from.Y].Card;

            // Fix OnlineCardFields array
            var index = state.OnlineCardFields[from.Card.Owner.PlayerNumber - 1].FindIndex(o=>o.X == from.X && o.Y == from.Y);
            state.OnlineCardFields[from.Card.Owner.PlayerNumber - 1][index] = state.Board[to.X, to.Y];
            state.Board[from.X, from.Y].Card = null;

            // Reached server?
            if (to.Y == 10)
            {
                PutOnStack(state, state.Board[to.X, to.Y], from.Card.Owner.PlayerNumber - 1);
            }
        }

        void GetNextStates(GameState state, int depth, ref int variations, int iterationCount = 0)
        {
            // Cycle through all board game cards and try all moves
            foreach (var field in state.OnlineCardFields[0])
            {
                if (field.Y > 7) continue;
                var moves = Game.GetMoveTargetFields(state, field);
                foreach (var move in moves)
                {
                    var nstate = GameState.GameStateFrom(state);

                    var mv = PlayMove(field, move);

                    // Apply move
                    MoveCard(nstate, field, move);

                    // Apply all possible opponent moves
                    nstate.Phase = GamePhase.Player2Turn;
                    foreach (var field2 in nstate.OnlineCardFields[1])
                    {
                        var moves2 = Game.GetMoveTargetFields(nstate, field2);
                        foreach (var move2 in moves2)
                        {
                            var nstate2 = GameState.GameStateFrom(nstate);

                            // Apply move
                            MoveCard(nstate2, field2, move2);

                            nstate2.Phase = GamePhase.Player1Turn;
                            nstate2.Move = mv;
                            CalculateLocalStore(nstate2);
                            state.NextStates.Add(nstate2);
                            ++variations;
                        }
                    }

                    // Only keep the 3 states with the best score and 3 random states
                    if (state.NextStates.Count > 6)
                    {
                        var orderedStates = state.NextStates.OrderByDescending(o => o.LocalScore).ToList();
                        state.NextStates.Clear();
                        for (int i = 0; i < 3; ++i) // Adds 3 best
                        {
                            state.NextStates.Add(orderedStates[0]); orderedStates.RemoveAt(0);
                        }
                        for (int i = 0; i < 3; ++i) // Adds 3 random ones
                        {
                            int index = _rnd.Next(0, orderedStates.Count);
                            state.NextStates.Add(orderedStates[index]); orderedStates.RemoveAt(index);
                        }
                    }
                }
            }

            if (++iterationCount < depth)
            {
                foreach (var s in state.NextStates)
                    GetNextStates(s, depth, ref variations, iterationCount);
            }
        }

        string Deploy()
        {
            // Randomize cards:
            var list = new List<char> { 'V', 'V', 'V', 'V', 'L', 'L', 'L', 'L', };
            var n = list.Count;
            while (n > 1)
            {
                --n;
                int i = _rnd.Next(n + 1);
                char c = list[i];
                list[i] = list[n];
                list[n] = c;
            }
            string ret = "dp ";
            foreach (char c in list)
            {
                ret += c;
            }
            return ret;
        }
    }
}
