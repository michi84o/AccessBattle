using AccessBattle;
using AccessBattle.Plugins;
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
        public IArtificialIntelligence P2;
        public LocalGame Game;
        public int Round;
        const int MaxRound = 100;
        public bool GameOver => Round >= MaxRound ||
                    Game.Phase == GamePhase.Aborted ||
                    Game.Phase == GamePhase.Player1Win ||
                    Game.Phase == GamePhase.Player2Win;

        public event EventHandler NeedsUiUpdate;

        public int AiDelay = 0;

        public void StartGame(Nou p1, IArtificialIntelligence p2)
        {
            P1 = p1;
            P2 = p2;
            if (Game != null)
            {
                Game.PropertyChanged -= Game_PropertyChanged;
                Game.SyncRequired -= Game_SyncRequired;
            }
            Round = 0;
            Game = new LocalGame();
            Game.SetAi(p1, 1);
            Game.SetAi(p2, 2);
            Game.InitGame();

            Game.AiCommandDelay = AiDelay;

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

        //int _uiUpdateSkip = 0;
        void OnUiUpdate(bool force)
        {
            if (force)
            {
                NeedsUiUpdate?.Invoke(this, EventArgs.Empty);
                return;
            }
            if (Game.Phase == GamePhase.Player2Turn && Game.AiCommandDelay < 100)
                return; // Skip
            //if (Game.AiCommandDelay < 100)
            //{
            //    //if (++_uiUpdateSkip < 2) return; // Skip
            //    //_uiUpdateSkip = 0;
            //}
            NeedsUiUpdate?.Invoke(this, EventArgs.Empty);
        }

        private void Game_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Game.Phase))
            {
                OnUiUpdate(false);

                if (Game.Phase == GamePhase.Player1Turn && ++Round < MaxRound)
                {
                    Task.Run(async () => await Game.AiPlayer1Move());
                }
                if (GameOver)
                    OnUiUpdate(true);
            }
        }
    }
}
