using AccessBattle.Networking.Packets;
using AccessBattle.Wpf.Model;
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
    public class GameViewModel : PropChangeNotifier, IBoardGame
    {
        // TODO: Add a "register local game" method
        // TODO: Add a "register remote game" method
        // ==> Maybe just pass a reference to the game model?

        GameModel _model;

        GamePhase _phase;
        /// <summary>
        /// Current game phase.
        /// </summary>
        public GamePhase Phase
        {
            get { return _phase; }
            set { SetProp(ref _phase, value); }
        }

        PlayerState[] _players;
        public PlayerState[] Players => _players;

        public BoardField[,] Board { get; private set; }

        public GameViewModel(GameModel parent)
        {
            Board = new BoardField[8, 11];
            for (ushort y = 0; y < 11; ++y)
                for (ushort x = 0; x < 8; ++x)
                {
                    Board[x, y] = new BoardField(x, y);
                }

            _players = new PlayerState[2];
            _players[0] = new PlayerState(1);
            _players[1] = new PlayerState(2);
            _model = parent;
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
            _players[0].Update(sync.Player1);
            _players[1].Update(sync.Player2);
            foreach (var field in sync.FieldsWithCards)
            {
                int x = field.X;
                int y = field.Y;
                _model.ConvertCoordinates(ref x, ref y);
                Board[x, y].Update(field, _players);
            }
            Phase = sync.Phase;
        }
    }
}
