using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccessBattle.Networking.Packets;

namespace AccessBattle.Plugins
{
    /// <summary>
    /// Base class for AI. Takes care of the synchronization.
    /// Converts coordinates internally if AI is player 2, 
    /// so that the its logic has always the same coordinate system.
    /// </summary>
    public abstract class AiBase : IArtificialIntelligence, IBoardGame
    {
        public uint UID => 0;

        // IBoardGame
        public PlayerState[] Players { get; }
        public BoardField[,] Board { get; }
        public GamePhase Phase { get; set; }

        protected GameSync Sync { get; set; }

        public bool IsAiHost { get; set; }
        protected abstract string _name { get; }
        public string Name { get => _name; set { } }

        protected List<BoardField> MyVirusCards = new List<BoardField>();
        protected List<BoardField> MyLinkCards = new List<BoardField>();
        protected List<BoardField> TheirOnlineCards = new List<BoardField>();
        BoardField myPlacedFirewall;
        BoardField theirPlacedFirewall;

        public AiBase()
        {
            Players = new PlayerState[2];
            Players[0] = new PlayerState(1);
            Players[1] = new PlayerState(2);

            Board = new BoardField[8, 11];
            for (ushort y = 0; y < 11; ++y)
                for (ushort x = 0; x < 8; ++x)
                    Board[x, y] = new BoardField(x, y);

            Phase = GamePhase.WaitingForPlayers;
        }

        public abstract string PlayTurn();

        protected string PlayMove(BoardField from, BoardField to)
        {
            return PlayMove(from.X, from.Y, to.X, to.Y);
        }
        protected string PlayMove(int x0, int y0, int x1, int y1)
        {
            Helpers.ConvertCoordinates(ref x0, ref y0, IsAiHost);
            Helpers.ConvertCoordinates(ref x1, ref y1, IsAiHost);
            return string.Format("mv {0},{1},{2},{3}", x0+1,y0+1,x1+1,y1+1);
        }

        protected string PlayBoost(BoardField field, bool place)
        {
            return PlayBoost(field.X, field.Y, place);
        }
        protected string PlayBoost(int x, int y, bool place)
        {
            Helpers.ConvertCoordinates(ref x, ref y, IsAiHost);
            return string.Format("bs {0},{1},{2}", x+1, y+1, place ? 1 : 0);
        }

        protected string PlayFirewall(BoardField field, bool place)
        {
            return PlayFirewall(field.X, field.Y, place);
        }
        protected string PlayFirewall(int x, int y, bool place)
        {
            Helpers.ConvertCoordinates(ref x, ref y, IsAiHost);
            return string.Format("fw {0},{1},{2}", x+1, y+1, place ? 1 : 0);
        }

        protected string PlayVirusCheck(BoardField field)
        {
            return PlayVirusCheck(field.X, field.Y);
        }
        protected string PlayVirusCheck(int x, int y)
        {
            Helpers.ConvertCoordinates(ref x, ref y, IsAiHost);
            return string.Format("vc {0},{1}", x+1, y+1);
        }

        protected string PlayError404(BoardField field1, BoardField field2, bool switchPlaces)
        {
            return PlayError404(field1.X, field1.Y, field2.X, field2.Y, switchPlaces);
        }
        protected string PlayError404(int x0, int y0, int x1, int y1, bool switchPlaces)
        {
            Helpers.ConvertCoordinates(ref x0, ref y0, IsAiHost);
            Helpers.ConvertCoordinates(ref x1, ref y1, IsAiHost);
            return string.Format("er {0},{1},{2},{3},{4}", x0+1, y0+1, x1+1, y1+1, switchPlaces ? 1 : 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sync">GameSync of the current game. Coordinates be flipped internally.</param>
        public void Synchronize(GameSync sync)
        {
            // It is easier for the programmer to 
            // view the board from the perspective of player 1
            // Therefore we flip the coordinates.
            Sync = GameSync.FlipBoard(sync, IsAiHost);
            
            // Clear board
            for (ushort y = 0; y < 11; ++y)
                for (ushort x = 0; x < 8; ++x)
                {
                    Board[x, y].Card = null;
                }
            MyLinkCards.Clear();
            MyVirusCards.Clear();
            TheirOnlineCards.Clear();
            myPlacedFirewall = null;
            theirPlacedFirewall = null;

            // Update all fields
            Players[0].Update(Sync.Player1);
            Players[1].Update(Sync.Player2);

            int myPlayerNumber = 1; // Thanks to conversion, this is always 1

            foreach (var field in Sync.FieldsWithCards)
            {
                int x = field.X;
                int y = field.Y;
                Board[x, y].Update(field, Players);

                if (y < 8)
                {
                    if (field.Card.Owner == myPlayerNumber)
                    {
                        if (field.Card.Type == OnlineCardType.Link)
                            MyLinkCards.Add(Board[x, y]);
                        else if (field.Card.Type == OnlineCardType.Virus)
                            MyVirusCards.Add(Board[x, y]);
                        else if (field.Card.IsFirewall)
                            myPlacedFirewall = Board[x, y];
                    }
                    else
                    {
                        if (field.Card.IsFirewall)
                            theirPlacedFirewall = Board[x, y];
                        else
                            TheirOnlineCards.Add(Board[x, y]);
                    }
                }

            }
            Phase = Sync.Phase;
        }
    }
}
