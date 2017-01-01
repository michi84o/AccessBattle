using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle
{
    public enum GamePhase
    {
        Init,
        Deployment,
        Player1Turn,
        Player2Turn,
        GameOver
    }

    public class Game : PropChangeNotifier
    {
        public Player Player1 { get; set; }
        public Player Player2 { get; set; }

        GamePhase _currentPhase;
        public GamePhase CurrentPhase
        {
            get { return _currentPhase; }
            set
            {
                if (value == _currentPhase) return;
                _currentPhase = value;
                OnPropertyChanged();
            }
        }

        public Game()
        {
            Player1 = new Player { Name = "Player 1" };
            Player2 = new Player { Name = "Player 2" };
            CurrentPhase = GamePhase.Init;
        }
    }
}
