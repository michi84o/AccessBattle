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
    [ExportMetadata("Version", "0.1")]
    public class MiaFactory : IArtificialIntelligenceFactory
    {
        public IPluginMetadata Metadata { get; set; }

        public IArtificialIntelligence CreateInstance() => new Mia();
    }

    public class Mia : AiBase
    {
        protected override string _name => "Mia (alpha)";

        Random rnd = new Random();

        class GameState : IBoardGame
        {
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

        public override string PlayTurn()
        {
            // Strategy:
            // Try all combination of moves for the next 2 turns
            // Calculate a score for each combination an choose the best one
            // Number of possible combinations (the following formulas do not match the numbers in the trials):
            //   Default: (16 * 4) ^ 2
            //   Boost: (14*4 + 2*12) ^ 2
            //   !!! A depth of 3 requires 2 GB of additional RAM !!!
            // Score calculation:
            //   - Distance to exit: Link
            //   - Distance to opponent cards: Link
            //   - Distance to opponent cards: Virus
            //   f1, f2, f3 have to be adjusted
            //   Proposed factors to start with: f1=2, f2=1, f3=1

            if (Phase == GamePhase.Deployment)
                return Deploy();

            // Base class converts state of game so AI is always player 1
            if (Phase != GamePhase.Player1Turn) return "???";

            // Create a list of all possible combinations
            var cState = GameState.GameStateFrom(this);
            int variations = 0;
            GetNextStates(cState, 2, ref variations);

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

        double CalculateScore(GameState state)
        {
            double score = 0;

            // 1. Distance of Links to Server
            double distanceSum = 0;
            foreach (var field in state.OnlineCardFields[0])
            {
                if ((field.Card as OnlineCard)?.Type != OnlineCardType.Link) continue;

                if (field.Y == 8)
                {
                    // Card reached server. Remove 1
                    --distanceSum;
                }
                else if (field.Y == 9)
                {
                    // Card captured by opponent
                    distanceSum += 15; // Add penalty
                }
                else if (field.Y < 8)
                {
                    // Distance to exit                
                    distanceSum += DistanceToExit(field);
                }
            }
            if (distanceSum < 1) score += 4;
            else score += 100 / distanceSum;

            // 2. Distance of Links and Viruses to Opponent cards
            distanceSum = 0;
            foreach (var field in state.OnlineCardFields[0])
            {
                var card = field.Card as OnlineCard;
                if (card == null) continue; // Should not happen

                foreach (var c1 in state.OnlineCardFields[1])
                {
                    var dst = Distance(field.X, field.Y, c1.X, c1.Y);
                    if (card.Type == OnlineCardType.Link)
                        distanceSum += dst*2;
                    else
                        distanceSum -= dst;
                }
            }            
            score += distanceSum / 200;

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
                            state.NextStates.Add(nstate2);
                            ++variations;
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
                int i = rnd.Next(n + 1);
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
