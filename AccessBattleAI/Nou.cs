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

        Random _rnd = new Random();

        NeuralNetwork _net1;
        NeuralNetwork _net2;

        public NeuralNetwork Net1 => _net1;
        public NeuralNetwork Net2 => _net2;

        /// <summary>
        /// Read neural network definition from file.
        /// </summary>
        /// <param name="index">0 for card selection network. 1 for movement netwoek</param>
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
                _net1.Mutate();
            }
            if (_net2 == null)
            {
                _net2 = new NeuralNetwork(94, 4, 1);
                _net2.Mutate();
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
                if (field.Y < 8)
                {
                    scores.Add(new FieldScore(field));
                }
            }
            foreach (var field in MyVirusCards)
            {
                if (field.Y < 8)
                    scores.Add(new FieldScore(field));
            }
            foreach (var sc in scores)
            {
                ApplyInputs(_net1, sc.Field);
                _net1.ComputeOutputs();
                sc.Score = _net1.Outputs[0];
            }
            scores = scores.OrderBy(o => o.Score).ToList();

            BoardField chosenField = null;
            for (int i = scores.Count - 1; i >= 0; --i)
            {
                if (Game.GetMoveTargetFields(this, scores[i].Field).Count == 0)
                    continue;
                chosenField = scores[i].Field;
                break;
            }
            if (chosenField == null) return "???";

            // Action 2: Move
            ApplyInputs(_net2, chosenField);
            _net2.ComputeOutputs();
            // Mapping is same as Input Array2
            scores.Clear();
            bool hasBoost = (chosenField.Card as OnlineCard)?.HasBoost == true;
            var targetMoves = Game.GetMoveTargetFields(this, chosenField);
            int x = chosenField.X;
            int y = chosenField.Y;
            for (int i = 0; i < 12; ++i)
            {
                var rel = InputArray2[i];
                int absX = x + rel.X;
                int absY = x + rel.Y;
                // Skip fields we cannot move to
                if (!targetMoves.Exists(o => o.X == absX && o.Y == absY))
                    continue;

                scores.Add(new FieldScore(Board[absX, absY]) { Score = _net2.Outputs[i] });
            }
            scores = scores.OrderBy(o => o.Score).ToList();

            BoardField targetField = null;
            if (scores.Count > 0) targetField = scores[scores.Count-1].Field;

            if (targetField == null) return "???";
            return PlayMove(chosenField, targetField);
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
                net.Inputs[i + 80] = 1 - 0.4*distance/7; // Largest distance is somewhere between 7 and 9
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
            // TODO:
            return 0;
        }

    }
}
