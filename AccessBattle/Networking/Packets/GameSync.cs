using System.Collections.Generic;

namespace AccessBattle.Networking.Packets
{
    /// <summary>
    /// Game synchronization class.
    /// </summary>
    public class GameSync
    {
        /// <summary>Game ID.</summary>
        public uint UID { get; set; }
        /// <summary>Current game phase.</summary>
        public GamePhase Phase { get; set; }

        /// <summary>State of player that created the game.</summary>
        public PlayerState.Sync Player1 { get; set; }
        /// <summary>State of player that joined the game.</summary>
        public PlayerState.Sync Player2 { get; set; }
        /// <summary>State of the board.</summary>
        public List<BoardField.Sync> FieldsWithCards { get; set; }

        /// <summary>
        /// Last command that was played.
        /// Can be used by clients to show animations.
        /// Error 404 flip bit must not be included!
        /// This should also contain a prefix with the player who played, followed by ':'.
        /// Example: Player 1 played move: "1:mv 1,1,2,2"
        /// </summary>
        public string LastExecutedCommand { get; set; }

        /// <summary>
        /// Get a sync object for a game.
        /// </summary>
        /// <param name="game">Game to get state from.</param>
        /// <param name="id">Unique id of the game.</param>
        /// <param name="player">For which player this game sync should be. Hides cards. 1 = Player1, 2 = Player 2.</param>
        /// <param name="lastExecutedCommand">Last command that was executed. Optional, should not leak secret information!</param>
        /// <returns></returns>
        public static GameSync FromGame(Game game, uint id, int player)
        {
            var board = game.Board;

            // Board = new BoardField[8, 11];
            var fieldsWithCard = new List<BoardField.Sync>();
            for (ushort y = 0; y < 11; ++y)
                for (ushort x = 0; x < 8; ++x)
                {
                    var field = board[x, y];
                    if (field.Card != null) fieldsWithCard.Add(board[x, y].GetSync());
                }
            var sync = new GameSync
            {
                UID = id,
                Phase = game.Phase,
                Player1 = game.Players[0].GetSync(),
                Player2 = game.Players[1].GetSync(),
                FieldsWithCards = fieldsWithCard,
                LastExecutedCommand = game.LastExecutedCommand
            };

            if (player != 1 && player != 2) return sync;

            // Hide cards of opponent
            foreach (var field in sync.FieldsWithCards)
            {
                if (field.Card.Owner != player && !field.Card.IsFaceUp)
                {
                    field.Card.Type = OnlineCardType.Unknown;
                }
            }
            return sync;
        }

        /// <summary>
        /// Converts the coordinates as if playes switched sides. Converts coordinates and player numbers.
        /// Used for AI.
        /// </summary>
        /// <param name="sync"></param>
        /// <returns></returns>
        public static GameSync FlipBoard(GameSync sync)
        {
            bool isPlayerHost = false;

            var s = new GameSync();

            s.Phase = sync.Phase;
            if (s.Phase == GamePhase.Player1Turn) s.Phase = GamePhase.Player2Turn;
            else if (s.Phase == GamePhase.Player2Turn) s.Phase = GamePhase.Player1Turn;
            else if (s.Phase == GamePhase.Player1Win) s.Phase = GamePhase.Player2Win;
            else if (s.Phase == GamePhase.Player2Win) s.Phase = GamePhase.Player1Win;

            s.Player1 = new PlayerState.Sync
            {
                Did404NotFound = sync.Player2.Did404NotFound,
                DidVirusCheck = sync.Player2.DidVirusCheck,
                PlayerNumber = sync.Player2.PlayerNumber == 1 ? 2 : 1,
                Points = sync.Player2.Points
            };
            s.Player2 = new PlayerState.Sync
            {
                Did404NotFound = sync.Player1.Did404NotFound,
                DidVirusCheck = sync.Player1.DidVirusCheck,
                PlayerNumber = sync.Player1.PlayerNumber == 1 ? 2 : 1,
                Points = sync.Player1.Points
            };

            s.UID = sync.UID;
            s.FieldsWithCards = new List<BoardField.Sync>();
            foreach (var field in sync.FieldsWithCards)
            {
                int x = field.X;
                int y = field.Y;
                Helpers.ConvertCoordinates(ref x, ref y, isPlayerHost);
                var bs = new BoardField.Sync
                {
                    Card = new Card.Sync
                    {
                        HasBoost = field.Card.HasBoost,
                        IsFaceUp = field.Card.IsFaceUp,
                        IsFirewall = field.Card.IsFirewall,
                        Owner = field.Card.Owner == 1 ? 2 : 1,
                        Type = field.Card.Type
                    },
                    X = (ushort)x,
                    Y = (ushort)y
                };
                s.FieldsWithCards.Add(bs);
            }
            return s;
        }
    }
}
