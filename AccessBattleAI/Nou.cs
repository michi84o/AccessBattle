using AccessBattle;
using AccessBattle.Plugins;
using AccessBattleAI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// The configuration of the neural network was created uding a genetic algorithm. (TODO)

namespace AccessBattleAI
{
    [Export(typeof(IPlugin))]
    [ExportMetadata("Name", "AccessBattle.AI.Nou")]
    [ExportMetadata("Description", "Nou 脳 (Brain). A neural network.")]
    [ExportMetadata("Version", "0.1")]
    public class NouFactory : IArtificialIntelligenceFactory
    {
        public IPluginMetadata Metadata { get; set; }

        public IArtificialIntelligence CreateInstance() => new Nou();
    }

    /// <summary>
    /// Implementation of Nou.
    /// </summary>
    public class Nou : AiBase
    {
        protected override string _name => "Nou";

        NeuralNetwork _net1;
        NeuralNetwork _net2;

        public const double MutateDelta = 0.01;

        public NeuralNetwork Net1 => _net1;
        public NeuralNetwork Net2 => _net2;

        /// <summary>
        /// Read neural network definition from file.
        /// </summary>
        /// <param name="index">0 for card selection network. 1 for movement network</param>
        /// <param name="path">File to load.</param>
        /// <returns></returns>
        public bool ReadFromFile(ushort index, string path)
        {
            if (!System.IO.File.Exists(path)) return false;
            if (index > 1) return false;

            var net = NeuralNetwork.ReadFromFile(path);
            if (net == null) return false;

            if (index == 0) _net1 = net;
            else _net2 = net;
            return true;
        }

        class FieldScore
        {
            public double Score { get; set; }
            public BoardField Field { get; set; }
            public FieldScore(BoardField field) { Field = field; }
        }

        public override string PlayTurn()
        {
            // Strategy: There is no strategy. The neurons just do their thing.

            if (_net1 == null)
            {
                _net1 = new NeuralNetwork(94, 4, 1);
                _net1.Mutate(MutateDelta);
            }
            if (_net2 == null)
            {
                _net2 = new NeuralNetwork(94, 4, 12);
                _net2.Mutate(MutateDelta);
            }

            // Deployment is random.
            if (Phase == GamePhase.Deployment)
                return Deploy();

            // Action 1: Card selection
            List<FieldScore> scores = new List<FieldScore>();

            // The net knows nothing about the server field
            // If a card is on the exit field, bring it in
            var enterServer = EnterServer();
            if (enterServer != null) return enterServer;

            foreach (var field in MyLinkCards)
            {
                scores.Add(new FieldScore(field));
            }
            foreach (var field in MyVirusCards)
            {
                scores.Add(new FieldScore(field));
            }
            foreach (var sc in scores)
            {
                ApplyInputs(_net1, sc.Field);
                _net1.ComputeOutputs(false);
                sc.Score = _net1.Outputs[0];
            }
            scores = scores.OrderByDescending(o => o.Score).ToList();

            BoardField chosenField = null;
            List<BoardField> targetMoves = null;
            for (int i = 0; i < scores.Count; ++i)
            {
                targetMoves = Game.GetMoveTargetFields(this, scores[i].Field);
                if (targetMoves.Count == 0)
                    continue;
                chosenField = scores[i].Field;
                break;
            }
            if (chosenField == null)
                return "???";

            // Action 2: Move
            ApplyInputs(_net2, chosenField);
            _net2.ComputeOutputs(false);
            // Mapping is same as Input Array2
            scores.Clear();
            bool hasBoost = (chosenField.Card as OnlineCard)?.HasBoost == true;
            int x = chosenField.X;
            int y = chosenField.Y;
            for (int i = 0; i < 12; ++i)
            {
                var rel = InputArray2[i];
                int absX = x + rel.X;
                int absY = y + rel.Y;
                // Skip fields we cannot move to
                if (!targetMoves.Exists(o => o.X == absX && o.Y == absY))
                    continue;

                scores.Add(new FieldScore(Board[absX, absY]) { Score = _net2.Outputs[i] });
            }
            scores = scores.OrderByDescending(o => o.Score).ToList();

            BoardField targetField = null;
            if (scores.Count > 0) targetField = scores[0].Field;

            if (targetField == null)
                return "???";
            return PlayMove(chosenField, targetField);
        }

        static int SeedBase = Environment.TickCount;
        static readonly object SeedLock = new object();
        string Deploy()
        {
            Random rnd;
            lock (SeedLock) { rnd = new Random((++SeedBase).GetHashCode() ^ Environment.TickCount); }
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

        struct Point
        {
            public int X;
            public int Y;
            public Point(int x, int y) { X = x; Y = y; }
        }

        void ApplyInputs(NeuralNetwork net, BoardField field)
        {
            int x = field.X;
            int y = field.Y;
            // Input 1+2: Opponent link/virus cards (2x40 fields)
            for (int i = 0; i < 40; ++i)
            {
                int nx = x + InputArray1[i].X;
                int ny = y + InputArray1[i].Y;

                // Empty fields get a zero
                if (nx < 0 || nx > 7 || ny < 0 || ny > 7 || Board[nx, ny].Card == null || Board[nx, ny].Card.Owner.PlayerNumber == 1
                    || !(Board[nx, ny].Card is OnlineCard))
                {
                    net.Inputs[i] = 0;
                    net.Inputs[i+40] = 0;
                    continue;
                }
                var card = Board[nx, ny].Card as OnlineCard;
                if (card.Type == OnlineCardType.Link)
                {
                    net.Inputs[i] = 1;
                    net.Inputs[i+40] = 0;
                }
                else if (card.Type == OnlineCardType.Virus)
                {
                    net.Inputs[i] = 0;
                    net.Inputs[i+40] = 1;
                }
                else
                {
                    net.Inputs[i] = 0.5;
                    net.Inputs[i+40] = 0.5;
                }
            }
            // Input 3: Movement fields
            var moveFields = Game.GetMoveTargetFields(this, field);
            for (int i = 0; i < 12; ++i)
            {
                int nx = x + InputArray2[i].X;
                int ny = y + InputArray2[i].Y;

                var mv = moveFields.FirstOrDefault(o => o.X == nx && o.Y == ny);
                if (mv == null ||
                    // Don't allow Virus on Exit field
                    (mv.IsExit && (field.Card as OnlineCard)?.Type == OnlineCardType.Virus))
                {
                    net.Inputs[i + 80] = 0;
                    continue;
                }

                // Field can be moved to. Assign a value between 0.5 and 1.0 depending on how far it is from the server
                var distance = DistanceToExit(mv.X, mv.Y);
                int isExit = (mv.IsExit && mv.Y == 7) ? 5 : 0; // give +5 if exit
                // Largest distance is somewhere between 7 and 9
                double distVal = 1 - (0.7 * distance / 7.0); // Gives value between 0.1 and 1
                distVal *= distVal; // Gives value between 0.01 and 1
                // Second term gives
                net.Inputs[i + 80] = isExit + 2*distVal; // Added a additional factor of 2
            }
            var myCard = field.Card as OnlineCard;
            if (myCard?.Type == OnlineCardType.Link)
            {
                net.Inputs[80 + 12] = 1;
                net.Inputs[80 + 13] = 0;
            }
            else
            {
                net.Inputs[80 + 12] = 0;
                net.Inputs[80 + 13] = 1;
            }
        }

        double DistanceToExit(int x, int y)
        {
            return Distance(x, y, x < 4 ? 3 : 4, 7);
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

        string EnterServer()
        {
            foreach (var link in MyLinkCards)
            {
                var targets = Game.GetMoveTargetFields(this, link);
                var t = targets.FirstOrDefault(o => o.Y == 10);
                if (t != null)
                {
                    return PlayMove(link, t);
                }
            }
            return null;
        }

        // Relative coordinates for Input 1+2
        Point[] InputArray1 = new Point[40]
        {
                                                                               new Point(0, 4),
                                                              new Point(-1, 3),new Point(0, 3),new Point(1, 3),
                                             new Point(-2, 2),new Point(-1, 2),new Point(0, 2),new Point(1, 2),new Point(2, 2),
                            new Point(-3, 1),new Point(-2, 1),new Point(-1, 1),new Point(0, 1),new Point(1, 1),new Point(2, 1),new Point(3, 1),
            new Point(-4,0),new Point(-3, 0),new Point(-2, 0),new Point(-1, 0),                new Point(1, 0),new Point(2, 0),new Point(3, 0),new Point(4,0),
                            new Point(-3,-1),new Point(-2,-1),new Point(-1,-1),new Point(0,-1),new Point(1,-1),new Point(2,-1),new Point(3,-1),
                                             new Point(-2,-2),new Point(-1,-2),new Point(0,-2),new Point(1,-2),new Point(2,-2),
                                                              new Point(-1,-3),new Point(0,-3),new Point(1,-3),
                                                                               new Point(0,-4)
        };
        Point[] InputArray2 = new Point[12]
        {
                                                  new Point(0, 2),
                                 new Point(-1, 1),new Point(0, 1),new Point(1, 1),
                new Point(-2, 0),new Point(-1, 0),                new Point(1, 0),new Point(2, 0),
                                 new Point(-1,-1),new Point(0,-1),new Point(1,-1),
                                                  new Point(0,-2)
        };

        public double Fitness()
        {
            // Score the current board:
            // Calculate the distance of the link cards to the server.
            // Link cards inside server give extra points.
            // Captured cards give penalty.

            double score = 0;
            // At the start of the game, the distance sum is about 28
            double distanceSum = 0;
            foreach (var field in AllMyLinkCards)
            {
                if (field.Y == 8)
                {
                    // Card reached server. Remove 1
                    --distanceSum;
                    score += 5; // Give some extra reward
                }
                else if (field.Y == 9)
                {
                    // Card captured by opponent
                    distanceSum += 10; // Add penalty
                }
                else if (field.Y < 8)
                {
                    // Distance to exit
                    distanceSum += DistanceToExit(field);
                }
            }

            if (distanceSum < 4) distanceSum = 4;
            // 3.6 at start of the game
            score += 100 / distanceSum;
            // Score will be between 2.5 and 25.

            // Also let the net use the Virus cards:
            // Distance of Viruses to Opponent cards
            foreach (var field in AllMyVirusCards)
            {
                if (field.Y == 8)
                {
                    // Played virus to server. This is already prevented.
                    // ... just in case:
                    score -= 10;
                }
                else if (field.Y == 9)
                {
                    // Player 2 caught a Virus, nice!
                    score += 5;
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
                    score += 1 / dMin; // Give a point if virus near opponent card
                }
            }

            double tenpc = score * 0.1; // 10% of score

            // Also add a score for captured link cards
            // Player 1 stack: Player1: Fields (0,8) - (7,8)
            for (int i = 0; i < 8; ++i)
            {
                var card = Board[i, 8].Card as OnlineCard;
                // Ignore own cards. We scored them already
                if (card == null || card.Owner.PlayerNumber == 1) continue;

                // Don't add too much. AI might catch cards by accident
                if (card.Type == OnlineCardType.Link) score += tenpc;
                // Give higher penalty for capturing virus cards
                if (card.Type == OnlineCardType.Virus) score -= tenpc * 2;
            }
            return score;
        }

        public void Mutate(double delta)
        {
            _net1?.Mutate(delta);
            _net2?.Mutate(delta);
        }

        public static Nou Copy(Nou nou)
        {
            var newNou = new Nou();
            newNou._net1 = NeuralNetwork.ReadFromString(nou._net1.SaveAsString());
            newNou._net2 = NeuralNetwork.ReadFromString(nou._net2.SaveAsString());
            return newNou;
        }



    }
}
