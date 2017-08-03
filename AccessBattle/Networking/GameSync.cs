using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Networking
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

        // player: For which player this game sync should be. Hides cards. 1 = Player1, 2 = Player 2
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
    }
}
