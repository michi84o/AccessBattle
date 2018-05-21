using AccessBattle;
using AccessBattleAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuralNetTrainer
{
    class Trainer
    {
        public Nou P1;
        public Nou P2;
        public LocalGame Game;
        public int Round;
        const int MaxRound = 200;
        public bool GameOver => Round >= MaxRound ||
                    Game.Phase == GamePhase.Aborted ||
                    Game.Phase == GamePhase.Player1Win ||
                    Game.Phase == GamePhase.Player2Win;

        public event EventHandler NeedsUiUpdate;

        public void StartGame(Nou p1, Nou p2)
        {
            P1 = p1;
            P2 = p2;
            if (Game != null)
            {
                Game.PropertyChanged -= Game_PropertyChanged;
                Game.SyncRequired -= Game_SyncRequired;
            }
            Round = 0;
            Game = new LocalGame { AiCommandDelay = 1000 };
            Game.SetAi(p1, 1);
            Game.SetAi(p2, 2);
            Game.InitGame();

            Game.AiCommandDelay = 0;

            Game.PropertyChanged += Game_PropertyChanged;
            Game.SyncRequired += Game_SyncRequired;

            NeedsUiUpdate?.Invoke(this, EventArgs.Empty);
            ++Round;
            Task.Run(async () => await Game.AiPlayer1Move());
        }

        private void Game_SyncRequired(object sender, EventArgs e)
        {
            // Fired when AI player 2 finishes move
            // Do nothing for now
            // Let Phase change trigger UI update
            //NeedsUiUpdate?.Invoke(this, EventArgs.Empty);
        }

        private void Game_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Game.Phase))
            {
                NeedsUiUpdate?.Invoke(this, EventArgs.Empty);
                if (Game.Phase == GamePhase.Player1Turn && ++Round < MaxRound)
                {
                    Task.Run(async () => await Game.AiPlayer1Move());
                }  
                if (GameOver)
                    NeedsUiUpdate?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
