using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle
{
    public enum GamePhase
    {
        Init,
        Deployment,
        PlayerTurns,
        GameOver
    }

    public enum PlayerAction
    {
        SelectCard,
        MoveSelectedCard,
        TakeSelectedCard,
        PlaceBoost,
        TakeBoost,
        Error404,
    }

    public class Game : PropChangeNotifier
    {
        public Player[] Players { get; private set; }
        int _currentPlayer;
        public int CurrentPlayer
        {
            get { return _currentPlayer; }
            set { SetProp(ref _currentPlayer, value); }
        }

        public Board Board { get; private set; }
        GamePhase _phase;
        public GamePhase Phase
        {
            get { return _phase; }
            set
            {
                if (SetProp(ref _phase, value))
                    OnPhaseChanged();
            }
        }

        void OnPhaseChanged()
        {
            var phase = _phase;
            if (phase == GamePhase.Init)
            {
                for (int x = 0; x < 8; ++x)
                    for (int y = 0; y < 10; ++y)
                        Board.Fields[x, y].Card = null;
                // Give each player 8 cards on his stack
                for (int p = 0; p < 2; ++p)
                    for (ushort i = 0; i < 4; ++i)
                    {
                        var pos = (ushort)(p * 8 + i);
                        // TODO: Randomization to prevent cheating ??? 
                        // Links
                        Board.OnlineCards[pos].Owner = Players[p];
                        Board.OnlineCards[pos].Type = OnlineCardType.Link;
                        Board.PlaceCard(i, (ushort)(8 + p), Board.OnlineCards[pos]);
                        // Viruses
                        pos += 4;
                        Board.OnlineCards[pos].Owner = Players[p];
                        Board.OnlineCards[pos].Type = OnlineCardType.Virus;
                        Board.PlaceCard((ushort)(i + 4), (ushort)(8 + p), Board.OnlineCards[pos]);
                    }
            }
            else if (phase == GamePhase.Deployment)
            {
                CurrentPlayer = 1; // UI can start deployment commands
            }
        }

        public Game()
        {
            Players = new Player[]
            {
                new Player(1) { Name = "Player 1"  },
                new Player(2) { Name = "Player 2"  }
            };            
            Board = new Board();
            Phase = GamePhase.Init;
            OnPhaseChanged();
        }

        public string CreateMoveCommand(Vector pos1, Vector pos2)
        {
            return string.Format("mv {0},{1},{2},{3}", pos1.X, pos1.Y, pos2.X, pos2.Y);
        }

        public bool ExecuteCommand(string command)
        {
            if (string.IsNullOrEmpty(command)) return false;
            var cmdCopy = command;
            string[] split;

            // Move Command
            // Syntax: mv x1,y1,x2,y2
            if (command.StartsWith("mv ", StringComparison.InvariantCulture) && command.Length > 3)
            {
                command = command.Substring(3);
                split = command.Split(new[] { ',' });
                if (split.Length != 4) return false;
                uint x1, x2, y1, y2;
                if (uint.TryParse(split[0], out x1) &&
                    uint.TryParse(split[1], out y1) &&
                    uint.TryParse(split[2], out x2) &&
                    uint.TryParse(split[3], out y2))
                {
                    // Check range
                    if (x1 > 7 || x2 > 7 || y1 > 9 || y2 > 9) return false;
                    // Check if move is allowed
                    var field1 = Board.Fields[x1, y1];
                    var field2 = Board.Fields[x2, y2];
                    var card1 = field1.Card;
                    if (card1 == null || card1.Owner.PlayerNumber != CurrentPlayer)
                    {
                        Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Player '"+ CurrentPlayer +"' cannot move cards of his opponent.");
                        return false;
                    }
                    if (field1.Type == BoardFieldType.Stack)
                    {
                        if ( _phase != GamePhase.Deployment)
                        {
                            Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Card can only be moved from stack during deployment phase");
                            return false;
                        }
                        // Deployment:
                        // Target field must be empty and on a deployment field
                        if (field2.Card != null || !Board.GetPlayerDeploymentFields(CurrentPlayer).Contains(field2))
                        {
                            Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Card can only be moved from stack to an empty deployment field");
                            return false;
                        }
                        field2.Card = field1.Card;
                        field1.Card = null;
                        field2.Card.Location = field2;
                    }
                }
            }

            return true;
        }
    }
}
