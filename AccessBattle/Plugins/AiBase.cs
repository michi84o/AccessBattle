using System.Collections.Generic;
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
        /// <summary>Game id. Is always 0.</summary>
        public uint UID => 0;

        // IBoardGame
        /// <summary>List of players. Length is always 2.</summary>
        public PlayerState[] Players { get; }
        /// <summary>List of board fields.</summary>
        public BoardField[,] Board { get; }
        /// <summary>Current game phase.</summary>
        public GamePhase Phase { get; set; }

        /// <summary>Sync object for the game.</summary>
        protected GameSync Sync { get; set; }

        /// <summary>True if AI is the host of the game.</summary>
        public bool IsAiHost { get; set; }
        /// <summary>Name of the AI.</summary>
        protected abstract string _name { get; }
        /// <summary>Name of the AI.</summary>
        public string Name { get => _name; set { } }

        /// <summary>
        /// Virus cards that are currently playable on the field. y below 8.
        /// </summary>
        protected List<BoardField> MyVirusCards = new List<BoardField>();
        /// <summary>
        /// Link cards that are currently playable on the field. y below 8.
        /// </summary>
        protected List<BoardField> MyLinkCards = new List<BoardField>();
        /// <summary>
        /// Opponent online cards that are currently playable on the field. y below 8.
        /// </summary>
        protected List<BoardField> TheirOnlineCards = new List<BoardField>();

        /// <summary>
        /// Includes cards that are on the stack.
        /// </summary>
        protected List<BoardField> AllMyVirusCards = new List<BoardField>();
        /// <summary>
        /// Includes cards that are on the stack.
        /// </summary>
        protected List<BoardField> AllMyLinkCards = new List<BoardField>();
        /// <summary>
        /// Includes cards that are on the stack.
        /// </summary>
        protected List<BoardField> AllTheirOnlineCards = new List<BoardField>();

        BoardField myPlacedFirewall;
        BoardField theirPlacedFirewall;

        /// <summary>
        /// ctor
        /// </summary>
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

        /// <summary>
        /// Tell AI to create a game command string for its next move.
        /// </summary>
        /// <returns></returns>
        public abstract string PlayTurn();

        /// <summary>
        /// Play a move.
        /// </summary>
        /// <param name="from">Card position.</param>
        /// <param name="to">Target position.</param>
        /// <returns></returns>
        protected string PlayMove(BoardField from, BoardField to)
        {
            return PlayMove(from.X, from.Y, to.X, to.Y);
        }
        /// <summary>
        /// Play move. Coordinates must be zero based and will be converted.
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <returns></returns>
        protected string PlayMove(int x0, int y0, int x1, int y1)
        {
            Helpers.ConvertCoordinates(ref x0, ref y0, IsAiHost);
            Helpers.ConvertCoordinates(ref x1, ref y1, IsAiHost);
            return string.Format("mv {0},{1},{2},{3}", x0+1,y0+1,x1+1,y1+1);
        }

        /// <summary>
        /// Play a boost card.
        /// </summary>
        /// <param name="field">Target field.</param>
        /// <param name="place">True if boost should be placed. False if it should be removed.</param>
        /// <returns></returns>
        protected string PlayBoost(BoardField field, bool place)
        {
            return PlayBoost(field.X, field.Y, place);
        }
        /// <summary>
        /// Play boost. Coordinates must be zero based and will be converted.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="place"></param>
        /// <returns></returns>
        protected string PlayBoost(int x, int y, bool place)
        {
            Helpers.ConvertCoordinates(ref x, ref y, IsAiHost);
            return string.Format("bs {0},{1},{2}", x+1, y+1, place ? 1 : 0);
        }

        /// <summary>
        /// Plays a firewall card.
        /// </summary>
        /// <param name="field">Target field.</param>
        /// <param name="place">True if card should be placed. False if card should be removed.</param>
        /// <returns></returns>
        protected string PlayFirewall(BoardField field, bool place)
        {
            return PlayFirewall(field.X, field.Y, place);
        }

        /// <summary>
        /// Play firewall. Coordinates must be zero based and will be converted.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="place"></param>
        /// <returns></returns>
        protected string PlayFirewall(int x, int y, bool place)
        {
            Helpers.ConvertCoordinates(ref x, ref y, IsAiHost);
            return string.Format("fw {0},{1},{2}", x+1, y+1, place ? 1 : 0);
        }

        /// <summary>
        /// Play virus check.
        /// </summary>
        /// <param name="field">Target field.</param>
        /// <returns></returns>
        protected string PlayVirusCheck(BoardField field)
        {
            return PlayVirusCheck(field.X, field.Y);
        }

        /// <summary>
        /// Play virus check. Coordinates must be zero based and will be converted.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected string PlayVirusCheck(int x, int y)
        {
            Helpers.ConvertCoordinates(ref x, ref y, IsAiHost);
            return string.Format("vc {0},{1}", x+1, y+1);
        }

        /// <summary>
        /// Play 404 card.
        /// </summary>
        /// <param name="field1">Field of first card.</param>
        /// <param name="field2">Field of secind card.</param>
        /// <param name="switchPlaces">True if cards should be switched.</param>
        /// <returns></returns>
        protected string PlayError404(BoardField field1, BoardField field2, bool switchPlaces)
        {
            return PlayError404(field1.X, field1.Y, field2.X, field2.Y, switchPlaces);
        }

        /// <summary>
        /// Play error 404. Coordinates must be zero based and will be converted.
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="switchPlaces"></param>
        /// <returns></returns>
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
            if (!IsAiHost)
                Sync = GameSync.FlipBoard(sync);
            else
                Sync = sync;

            // Clear board
            for (ushort y = 0; y < 11; ++y)
                for (ushort x = 0; x < 8; ++x)
                {
                    Board[x, y].Card = null;
                }
            MyLinkCards.Clear();
            MyVirusCards.Clear();
            TheirOnlineCards.Clear();
            AllMyLinkCards.Clear();
            AllMyVirusCards.Clear();
            AllTheirOnlineCards.Clear();
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

                if (field.Card.Owner == myPlayerNumber)
                {
                    if (field.Card.Type == OnlineCardType.Link)
                    {
                        if (y < 8) MyLinkCards.Add(Board[x, y]);
                        AllMyLinkCards.Add(Board[x, y]);
                    }
                    else if (field.Card.Type == OnlineCardType.Virus)
                    {
                        if (y < 8) MyVirusCards.Add(Board[x, y]);
                        AllMyVirusCards.Add(Board[x, y]);
                    }
                    else if (field.Card.IsFirewall)
                        if (y < 8) myPlacedFirewall = Board[x, y];
                }
                else
                {
                    if (field.Card.IsFirewall)
                    {
                        if (y < 8) theirPlacedFirewall = Board[x, y];
                    }
                    else
                    {
                        if (y < 8) TheirOnlineCards.Add(Board[x, y]);
                        AllTheirOnlineCards.Add(Board[x, y]);
                    }
                }
            }
            Phase = Sync.Phase;
        }
    }
}
