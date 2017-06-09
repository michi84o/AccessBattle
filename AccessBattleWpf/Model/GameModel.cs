using AccessBattle.Networking;
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
        public Game Game { get { return _game; } }

        NetworkGameClient _client = new NetworkGameClient();

        bool _isPlayerHost = true;
        public bool IsPlayerHost
        {
            get { return _isPlayerHost; }
            set { SetProp(ref _isPlayerHost, value); }
        }

        // TODO: Behavior when opponent disconnects
        // TODO: Option to give up game

        public GameModel()
        {

        }

    }
}
