using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AccessBattle
{
    public class GameAI
    {
        Game _game;
        int _playerIndex;
        Random rnd;

        SynchronizationContext _context;

        public GameAI(Game game, int playerIndex = 2)
        {
            _game = game;
            rnd = new Random(Guid.NewGuid().GetHashCode());
            _playerIndex = playerIndex;
            _context = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        void ContextExecute(Action action)
        {
            var handler = action;
            if (handler != null)
                _context.Send(o => handler(), null);
        }

        public virtual void PlayTurn()
        {
#pragma warning disable CC0022 // Should dispose object
            (new Task(() =>
            {

                if (_game.Phase == GamePhase.Deployment)
                {
                    DeployCards();
                }
                else if (_game.Phase == GamePhase.PlayerTurns)
                {

                }
                ContextExecute(() => { _game.CurrentPlayer = (_playerIndex == 2) ? 1 : 2; });
            })).Start();
#pragma warning restore CC0022 // Should dispose object
        }

        protected virtual void DeployCards()
        {
            // Randomly deploy cards:
            // All cards must be stored in stack
            var depFields = _game.Board.GetPlayerDeploymentFields(_playerIndex);
            var stackFields = _game.Board.GetPlayerStackFields(_playerIndex);
            // TODO: Shuffle Stack fields randomly

            for (int i = 0; i < 8; ++i)
            {
                int index = rnd.Next(0, depFields.Count);
                var depField = depFields[index];
                depFields.Remove(depField);
                Thread.Sleep(500);
                ContextExecute(() =>
                {
                    if (!_game.ExecuteCommand(_game.CreateMoveCommand(stackFields[i].Position, depField.Position)))
                    {
                        Trace.WriteLine("AI: Could not depoly card!!!");
                    }
                });
            }
            ContextExecute(() => { _game.Phase = GamePhase.PlayerTurns; });
        }
    }
}
