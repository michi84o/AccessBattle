using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Wpf.Model
{
    class GameModel : PropChangeNotifier
    {
        Game _game = new Game();

        bool _isPlayerHost = true;
        public bool IsPlayerHost
        {
            get { return _isPlayerHost; }
            set { SetProp(ref _isPlayerHost, value); }
        }

    }
}
