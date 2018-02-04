using AccessBattle.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccessBattle.Networking.Packets;
using AccessBattle;

namespace AccessBattleAI
{
    [Export(typeof(IPlugin))]
    [ExportMetadata("Name", "AccessBattle.AI.BAKA")]
    [ExportMetadata("Description", "BAKA (jap. fool) is a simple stupid AI that just does nearly random moves.")]
    [ExportMetadata("Version", "0.1")]
    public class BakaFactory : IArtificialIntelligenceFactory
    {
        public IPluginMetadata Metadata { get; set; }

        public IArtificialIntelligence CreateInstance() => new Baka();
    }

    public class Baka : AiBase 
    {
        protected override string _name => "BAKA";

        Random rnd = new Random();

        public override string PlayTurn()
        {
            if (Phase == GamePhase.Deployment)
                return Deploy();

            // Base class converts state of game so AI is always player 1
            if (Phase != GamePhase.Player1Turn) return "???";

            List<BoardField> targets;
            // =====================================================================
            // Turn logic
            // =====================================================================

            // First check if any card can enter the server area
            foreach (var link in MyLinkCards)
            {
                targets = Game.GetMoveTargetFields(this, link);
                var t = targets.FirstOrDefault(o=>o.Y == 10);
                if (t != null)
                {
                    return PlayMove(link, t);
                }
            }

            // Just grab a random card and move forward
            // Choose to move a virus or link
            List<BoardField> myCards = MyLinkCards;
            if (rnd.Next(0, 101) <= 40) // 40% Chance to pick virus
                myCards = MyVirusCards;
            // There is always at least one card of a type left. Otherwise game is over
            var card = myCards[rnd.Next(0, myCards.Count)];

            // Just in case card cannot move, reorder myCards:
            myCards.Remove(card);
            myCards.Insert(0, card);

            foreach (var c in myCards)
            {
                var possibleMoves = Game.GetMoveTargetFields(this, c);
                if (possibleMoves.Count == 0) continue;
                // Choose one of the moves preferably on in directon of exit
                var closestMove = possibleMoves[0];
                var closestDistance = DistanceToExit(closestMove);
                for (int i = 1; i < possibleMoves.Count; ++i)
                {
                    var curMove = possibleMoves[i];
                    var dist = DistanceToExit(curMove);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestMove = curMove;
                    }
                }
                return PlayMove(c, closestMove);
            }                       

            // =====================================================================            
            return "???";
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

        string Deploy()
        {
            // Randomize cards:
            var list = new List<char> { 'V', 'V', 'V', 'V', 'L', 'L', 'L', 'L', };
            Random rnd = new Random();
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
