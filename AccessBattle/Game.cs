using AccessBattle;
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
        WaitingForPlayers,
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
        #region Members

        string _name;
        public string Name
        {
            get { return _name; }
            set { SetProp(ref _name, value); }
        }

        Player[] _players;
        public Player[] Players { get { return _players; } }

        int _currentPlayer;
        public int CurrentPlayer
        {
            get { return _currentPlayer; }
            set { SetProp(ref _currentPlayer, value); } // TODO: Make private
        }

        int _winningPlayer;
        public int WinningPlayer
        {
            get { return _winningPlayer; }
            set { SetProp(ref _winningPlayer, value); }
        }

        Board _board;
        public Board Board { get { return _board; }  }

        GamePhase _phase;
        public GamePhase Phase
        {
            get { return _phase; }
            set // TODO: Private
            {
                if (_phase != value)
                {
                    _phase = value;
                    OnPhaseChanged(); // Should be done before event fires
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        void OnPhaseChanged()
        {
            var phase = _phase;
            if (phase == GamePhase.Init)
            {
                Board.GetFirewall(1).Owner = Players[0];
                Board.GetFirewall(2).Owner = Players[1];

                Players[0].Did404NotFound = false;
                Players[0].DidVirusCheck = false;
                Players[1].Did404NotFound = false;
                Players[1].DidVirusCheck = false;                

                _winningPlayer = 0;
                
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
            else if (phase == GamePhase.PlayerTurns)
            {
                // TODO: Random
                /* var rnd = new Random(Guid.NewGuid().GetHashCode());
                CurrentPlayer = rnd.Next(1, 3); */
                CurrentPlayer = 1;
            }
        }

        uint _uid;
        public uint UID { get { return _uid; } }

        public Game(uint uid = 0)
        {
            _uid = uid;
            _players = new Player[]
            {
                new Player(1) { Name = "Player 1"  },
                new Player(2) { Name = "Player 2"  }
            };
            _winningPlayer = 0;
            _board = new Board();
            _phase = GamePhase.Init;
            OnPhaseChanged();
        }

        public string CreateMoveCommand(Vector pos1, Vector pos2)
        {
            return string.Format("mv {0},{1},{2},{3}", pos1.X, pos1.Y, pos2.X, pos2.Y);
        }

        public string CreateSetBoostCommand(Vector pos, bool setBoost)
        {
            return string.Format("bs {0},{1},{2}", pos.X, pos.Y, setBoost ? 1 : 0);
        }

        public string CreateSetFirewallCommand(Vector pos, bool setFireWall)
        {
            return string.Format("fw {0},{1},{2}", pos.X, pos.Y, setFireWall ? 1 : 0);
        }

        public string CreateSetVirusCheckCommand(Vector pos)
        {
            return string.Format("vc {0},{1}", pos.X, pos.Y);
        }

        public string CreateUseError404Command(Vector pos1, Vector pos2, bool switchCards)
        {
            return string.Format("er {0},{1},{2},{3},{4}", pos1.X, pos1.Y, pos2.X, pos2.Y, switchCards ? 1 : 0);
        }

        void PlaceCardOnStack(OnlineCard card)
        {
            if (card == null) return;
            var stacks = Board.GetPlayerStackFields(CurrentPlayer);
            // First 4 fields are reserved for link cards, others for viruses
            // If a card is not revealed it is treated as a link card            
            int stackpos = -1;
            if (card.IsFaceUp && card.Type == OnlineCardType.Virus)
            {
                for (int i = 4; i < stacks.Count; ++i)
                {
                    if (stacks[i].Card != null) continue;
                    stackpos = i;
                    break;
                }
            }
            if (stackpos == -1)
            {
                for (int i = 0; i < 4 && i < stacks.Count; ++i)
                {
                    if (stacks[i].Card != null) continue;
                    stackpos = i;
                    break;
                }
                if (stackpos == -1) // Happens when card.IsFaceUp and card is virus and link stack is full
                {
                    for (int i = 4; i < stacks.Count; ++i)
                    {
                        if (stacks[i].Card != null) continue;
                        stackpos = i;
                        break;
                    }
                }
            }
            // stackpos must have a value by now or stack is full. 
            // Stack can never be full. If more than 6 cards are on the stack, the game is over
            stacks[stackpos].Card = card;
            card.Location.Card = null;
            card.Location = stacks[stackpos];
            //if (CurrentPlayer > 0)
            //    card.Owner = Players[CurrentPlayer-1]; // TODO: Color does not update with this code
            // Check if player won or lost
            int linkCount = 0;
            int virusCount = 0;
            foreach (var field in stacks)
            {
                var c = field.Card as OnlineCard;
                if (c == null) continue;
                if (c.Type == OnlineCardType.Link) ++linkCount;
                if (c.Type == OnlineCardType.Virus) ++virusCount;
                // TODO: Network Clients can have unknown cards. Make sure to reveal them first
            }
            if (virusCount >= 4)
            {
                _winningPlayer = _currentPlayer == 1 ? 2 : 1;
                Phase = GamePhase.GameOver;
            }
            if (linkCount >= 4)
            {
                _winningPlayer = _currentPlayer == 1 ? 1 : 2;
                Phase = GamePhase.GameOver;
            }
        }

        /// <summary>
        /// Command      Syntax             Example
        /// -------------------------------------------
        /// Move         mv x1,y1,x2,y2     mv 0,0,1,0
        /// Boost        bs x1,y1,e         bs 0,0,1
        /// Firewall     fw x1,y1,e         fw 0,0,1
        /// Virus Check  vc x1,y1           vc 0,0
        /// Error 404    er x1,y1,x2,y2,s   er 0,0,1,1,1
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public bool ExecuteCommand(string command)
        {
            if (string.IsNullOrEmpty(command)) return false;
            var cmdCopy = command;
            string[] split;

            if (_currentPlayer < 1 || _currentPlayer > 2)
            {
                Trace.WriteLine("Game: Cannot execute command! Current player is not set.");
                return false;
            }

            // Move Command
            // Syntax: mv x1,y1,x2,y2
            #region Move
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
                    if (x1 > 7 || x2 > 7 || y1 > 10 || y2 > 10)
                    {
                        Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Out of range.");
                        return false;
                    }
                    // Check if move is allowed
                    var field1 = Board.Fields[x1, y1];
                    var field2 = Board.Fields[x2, y2];
                    var card1 = field1.Card;
                    if (card1 == null || card1.Owner.PlayerNumber != CurrentPlayer)
                    {
                        if (card1 == null)
                            Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! First field has no card to move.");
                        else
                            Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Player '" + CurrentPlayer + "' cannot move cards of his opponent.");
                        return false;
                    }

                    if (field1.Type == BoardFieldType.Stack)
                    {
                        // Moving cards from Stack is only allowed during deployment
                        if (_phase != GamePhase.Deployment)
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
                        return true;
                    }
                    if (field1.Type == BoardFieldType.Main || field1.Type == BoardFieldType.Exit)
                    {
                        // Putting card back to stack is allowed during deployment or when card is claimed
                        if (field2.Type == BoardFieldType.Stack)
                        {
                            if (_phase == GamePhase.Deployment)
                            {
                                // Source field must be a deployment field
                                if (field2.Card != null || !Board.GetPlayerDeploymentFields(CurrentPlayer).Contains(field1))
                                {
                                    Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Source card is not on deployment field");
                                    return false;
                                }
                                field2.Card = field1.Card;
                                field1.Card = null;
                                field2.Card.Location = field2;
                                return true;
                            }
                            else
                            {
                                // Card claimed
                                // Currently not valid. Game will do that automatically
                            }
                        }
                        // Default movement: Main-Main or exit
                        else if (
                            field2.Type == BoardFieldType.Main ||
                            field2.Type == BoardFieldType.Exit ||
                            field2.Type == BoardFieldType.ServerArea)
                        {
                            // GetTargetFields already does some rule checks
                            if (GetTargetFields(field1).Contains(field2))
                            {
                                // Check if opponents card is claimed
                                if (field2.Card != null)
                                {
                                    var card = field2.Card as OnlineCard;
                                    if (card == null || card.Owner.PlayerNumber == CurrentPlayer)
                                    {
                                        Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Can only jump onto online cards of opponent");
                                        return false;
                                    }
                                    // Reveal card
                                    card.IsFaceUp = true;
                                    PlaceCardOnStack(card);
                                    // Remove boost if applied
                                    if (card.HasBoost) card.HasBoost = false;
                                }
                                if (field2.Type == BoardFieldType.ServerArea)
                                {
                                    var card = field1.Card as OnlineCard;
                                    PlaceCardOnStack(card);
                                    // Remove boost if applied
                                    if (card.HasBoost) card.HasBoost = false;
                                    return true;
                                }
                                field2.Card = field1.Card;
                                field1.Card = null;
                                field2.Card.Location = field2;
                                return true;
                            }
                        }
                    }
                    //if (field1.Type == BoardFieldType.Exit)
                    //{
                    //    // Cards are never stored on an exit field
                    //    Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Exit field cannot contain cards");
                    //    return false;
                    //}
                }
            }
            #endregion
            #region Boost
            else if (command.StartsWith("bs ", StringComparison.InvariantCulture) && command.Length > 3)
            {
                command = command.Substring(3);
                split = command.Split(new[] { ',' });
                if (split.Length != 3) return false;
                uint x1, y1, enabled;
                if (uint.TryParse(split[0], out x1) &&
                    uint.TryParse(split[1], out y1) &&
                    uint.TryParse(split[2], out enabled))
                {
                    // Check range
                    if (x1 > 7 || y1 > 9 || enabled > 1)
                    {
                        Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Out of range.");
                        return false;
                    }
                    var field1 = Board.Fields[x1, y1];
                    var card1 = field1.Card;
                    if (card1 == null || card1.Owner.PlayerNumber != CurrentPlayer)
                    {
                        Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Boost can only be placed on own cards.");
                        return false;
                    }

                    // Check if boost is already used
                    var boostedCard = Board.OnlineCards.FirstOrDefault(
                        card => card.HasBoost && card.Owner.PlayerNumber == CurrentPlayer);
                    if (enabled == 1)
                    {
                        if (boostedCard != null)
                        {
                            Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Boost is already placed.");
                            return false;
                        }
                        else
                        {
                            // Place boost
                            if (card1 is OnlineCard)
                            {
                                ((OnlineCard)card1).HasBoost = true;
                                return true;
                            }
                            else
                            {
                                Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Target is not an online card.");
                                return false;
                            }
                        }
                    }
                    else if (enabled == 0)
                    {
                        if (boostedCard == card1)
                        {
                            boostedCard.HasBoost = false;
                            return true;
                        }
                        else
                        {
                            Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Target has no boost.");
                            return false;
                        }
                    }
                }
            }
            #endregion
            #region Firewall
            else if (command.StartsWith("fw ", StringComparison.InvariantCulture) && command.Length > 3)
            {
                command = command.Substring(3);
                split = command.Split(new[] { ',' });
                if (split.Length != 3) return false;
                uint x1, y1, enabled;
                if (uint.TryParse(split[0], out x1) &&
                    uint.TryParse(split[1], out y1) &&
                    uint.TryParse(split[2], out enabled))
                {
                    // Check range
                    if (x1 > 7 || y1 > 9 || enabled > 1)
                    {
                        Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Out of range.");
                        return false;
                    }
                    var field1 = Board.Fields[x1, y1];
                    var fw = Board.GetFirewall(_currentPlayer);
                    if (enabled == 1) // enable firewall
                    {
                        if (fw.Location != null)
                        {
                            Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Firewall is already placed.");
                            return false;
                        }
                        if (field1.Card != null || field1.Type != BoardFieldType.Main)
                        {
                            Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Firewall can only be placed on empty main field.");
                            return false;
                        }
                        field1.Card = fw;
                        fw.Location = field1;
                        return true;
                    }
                    else // disable firewall
                    {
                        if (field1.Card != fw)
                        {
                            Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Firewall is not on the selected field.");
                        }
                        field1.Card = null;
                        fw.Location = null;
                        return true;
                    }
                }
            }
            #endregion
            #region Virus Check
            else if (command.StartsWith("vc ", StringComparison.InvariantCulture) && command.Length > 3)
            {
                command = command.Substring(3);
                split = command.Split(new[] { ',' });
                if (split.Length != 2) return false;
                uint x1, y1;
                if (uint.TryParse(split[0], out x1) &&
                    uint.TryParse(split[1], out y1))
                {
                    // Check range
                    if (x1 > 7 || y1 > 9)
                    {
                        Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Out of range.");
                        return false;
                    }
                    // Check usage
                    if (Players[_currentPlayer-1].DidVirusCheck)
                    {
                        Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Virus Check was already used.");
                        return false;
                    }
                    var field = Board.Fields[x1, y1];
                    int targetPlayer = 0;
                    if (_currentPlayer == 1) targetPlayer = 2;
                    else if (_currentPlayer == 2) targetPlayer = 1;
                    if (field.Card != null && field.Card is OnlineCard && field.Card.Owner != null && field.Card.Owner.PlayerNumber == targetPlayer)
                    {
                        ((OnlineCard)field.Card).IsFaceUp = true;
                        Players[_currentPlayer - 1].DidVirusCheck = true;
                        return true;
                    }
                    Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Virus Check can only be used on cards of opponent.");
                    return false;
                }
            }
            #endregion
            #region Error 404
            else if (command.StartsWith("er ", StringComparison.InvariantCulture) && command.Length > 3)
            {
                command = command.Substring(3);
                split = command.Split(new[] { ',' });
                if (split.Length != 5) return false;
                uint x1, y1, x2, y2, switchCards;
                if (uint.TryParse(split[0], out x1) &&
                    uint.TryParse(split[1], out y1) &&
                    uint.TryParse(split[2], out x2) &&
                    uint.TryParse(split[3], out y2) &&
                    uint.TryParse(split[4], out switchCards))
                {
                    // Check range
                    if (x1 > 7 || y1 > 9 || x2 > 7 || y2 > 9 || switchCards > 1)
                    {
                        Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Out of range.");
                        return false;
                    }
                    // Check usage
                    if (Players[_currentPlayer-1].Did404NotFound)
                    {
                        Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Error 404 was already used.");
                        return false;
                    }
                    var field1 = Board.Fields[x1, y1];
                    var field2 = Board.Fields[x2, y2];
                    var card1 = field1.Card as OnlineCard;
                    var card2 = field2.Card as OnlineCard;
                    if (card1 == null || card2 == null)
                    {
                        Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! One of the fields has no online card.");
                        return false;
                    }

                    if (card1.Owner.PlayerNumber != _currentPlayer || card2.Owner.PlayerNumber != _currentPlayer)
                    {
                        Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid! Only own cards can be switched.");
                        return false;
                    }

                    Players[_currentPlayer-1].Did404NotFound = true;                    

                    card1.IsFaceUp = false;
                    card2.IsFaceUp = false;

                    if (switchCards == 1)
                    {
                        field1.Card = card2;
                        field2.Card = card1;
                    }
                                        
                    return true;

                } 
            }
            #endregion
            Trace.WriteLine("Game: Move '" + cmdCopy + "' invalid!");
            return false;
        }

        public List<BoardField> GetTargetFields(BoardField field)
        {
            var fields = new List<BoardField>();
            if (field == null || field.Card == null || field.Card.Owner.PlayerNumber != CurrentPlayer)
                return fields;

            var card = field.Card as OnlineCard;
            if (card == null) return fields; // Firewall

            var p = field.Position;

            // without boost there are 4 possible locations
            var fs = new BoardField[4];
            if (p.Y > 0 && p.Y <= 7) // Down
                fs[0] = Board.Fields[p.X, p.Y - 1];
            else
            {
                // Special case for exit fields:
                if (p.Y == 0 && field.Type == BoardFieldType.Exit && CurrentPlayer == 2)
                    fs[0] = Board.Fields[4, 10];
            }

            if (p.Y >= 0 && p.Y < 7) // Up
                fs[1] = Board.Fields[p.X, p.Y + 1];
            else
            {
                // Special case for exit fields:
                if (p.Y == 7 && field.Type == BoardFieldType.Exit && CurrentPlayer == 1)
                    fs[1] = Board.Fields[5, 10];
            }

            if (p.X > 0 && p.X <= 7 && p.Y <= 7) // Left;  Mask out stack fields
                fs[2] = Board.Fields[p.X - 1, p.Y];
            if (p.X >= 0 && p.X < 7 && p.Y <= 7) // Right
                fs[3] = Board.Fields[p.X + 1, p.Y];            

            // Moves on opponents cards are allowed, move on own card not
            for (int i = 0; i < 4; ++i)
            {
                if (fs[i] == null) continue;
                if (fs[i].Card != null && fs[i].Card.Owner.PlayerNumber == CurrentPlayer) continue;
                if (fs[i].Card != null && !(fs[i].Card is OnlineCard)) continue; // Can only jump on online cards <- This ignores the firewall card
                if (fs[i].Type == BoardFieldType.Stack) continue;
                if (fs[i].Type == BoardFieldType.Exit)
                {
                    // Exit field is allowed, but only your opponents
                    if (CurrentPlayer == 1 && fs[i].Position.Y == 0) continue;
                    if (CurrentPlayer == 2 && fs[i].Position.Y == 7) continue;
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
                    p = f.Position;
                    if (p.Y > 0 && p.Y <= 7)
                        fs[0] = Board.Fields[p.X, p.Y - 1];
                    else
                    {
                        // Special case for exit fields:
                        if (p.Y == 0 && f.Type == BoardFieldType.Exit && CurrentPlayer == 2)
                            fs[1] = Board.Fields[4, 10];
                    }

                    if (p.Y >= 0 && p.Y < 7)
                        fs[1] = Board.Fields[p.X, p.Y + 1];
                    else
                    {
                        // Special case for exit fields:
                        if (p.Y == 7 && f.Type == BoardFieldType.Exit && CurrentPlayer == 1)
                            fs[1] = Board.Fields[5, 10];
                    }

                    if (p.X > 0 && p.X <= 7 && p.Y <= 7) // No Stack fields!
                        fs[2] = Board.Fields[p.X - 1, p.Y];
                    if (p.X >= 0 && p.X < 7 && p.Y <= 7) // No Stack fields!
                        fs[3] = Board.Fields[p.X + 1, p.Y];

                    for (int i = 0; i < 4; ++i)
                    {
                        if (fs[i] == null) continue;
                        if (fs[i].Card != null && fs[i].Card.Owner.PlayerNumber == CurrentPlayer) continue;
                        if (fs[i].Type == BoardFieldType.Stack) continue;
                        if (fs[i].Type == BoardFieldType.Exit)
                        {
                            // Exit field is allowed, but only your opponents
                            if (CurrentPlayer == 1 && fs[i].Position.Y == 0) continue;
                            if (CurrentPlayer == 2 && fs[i].Position.Y == 7) continue;
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

            return fields;
        }
    }
}
