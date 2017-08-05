using AccessBattle.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Wpf.ViewModel
{
    /// <summary>
    /// View model for displaying the state of a game.
    /// Includes board and cards.
    /// Provides abstraction of the game class for remote and local games.
    /// </summary>
    public class GameViewModel : PropChangeNotifier
    {
        // TODO: Add a "register local game" method
        // TODO: Add a "register remote game" method
        // ==> Maybe just pass a reference to the game model?

        GamePhase _phase;
        /// <summary>
        /// Current game phase.
        /// </summary>
        public GamePhase Phase
        {
            get { return _phase; }
            protected set { SetProp(ref _phase, value); }
        }

        public PlayerState Player1 { get; private set; }
        public PlayerState Player2 { get; private set; }

        public BoardField[,] Board { get; private set; }

        public GameViewModel()
        {
            Board = new BoardField[8, 11];
            for (ushort y = 0; y < 11; ++y)
                for (ushort x = 0; x < 8; ++x)
                {
                    Board[x, y] = new BoardField(x, y);
                }

            Player1 = new PlayerState(1);
            Player2 = new PlayerState(2);
        }

        public void Synchronize(GameSync sync)
        {
            // Clear board
            for (ushort y = 0; y < 11; ++y)
                for (ushort x = 0; x < 8; ++x)
                {
                    Board[x, y].Card = null;
                }
            // Update all fields
            Player1.Update(sync.Player1);
            Player2.Update(sync.Player2);
            foreach (var field in sync.FieldsWithCards)
            {
                Board[field.X, field.Y].Update(field, Player1, Player2);
            }
            Phase = sync.Phase;
        }
    }
}
