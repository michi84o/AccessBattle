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
        public Game Game { get {return _game;}}

        public MainWindowViewModel()
        {
            _game = new Game();
            _board = new Board();
        }

        Board _board;
        public Board Board { get { return _board; } }

    }
}
