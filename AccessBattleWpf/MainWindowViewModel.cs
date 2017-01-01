using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AccessBattle;

namespace AccessBattleWpf
{
    public class MainWindowViewModel : PropChangeNotifier
    {
        Game _game;

        public event EventHandler StartingNewGame;

        public MainWindowViewModel()
        {
            _game = new Game();
            _game.PropertyChanged += Game_PropertyChanged;
            // TODO: Ask for player name
        }

        void Game_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var justDoIt = string.IsNullOrEmpty(e.PropertyName);
            if (justDoIt || e.PropertyName == "CurrentPhase")
            {
                StartNewGame();
            }
        }

        public void StartNewGame()
        {
            var handler = StartingNewGame;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
