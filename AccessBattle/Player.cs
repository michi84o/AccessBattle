using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace AccessBattle
{
    // TODO: Make Disposeable
    public class Player : PropChangeNotifier
    {
        string _name;
        public string Name
        {
            get { return _name; }
            set { SetProp(ref _name, value); }
        }
        public int Points { get; set; }

        public bool DidVirusCheck { get; set; }
        public bool Did404NotFound { get; set; }

        int _playerNumber = 0;
        public int PlayerNumber { get { return _playerNumber; } }
        public Socket Connection;

        public Player(int playerNumber)
        {
            _playerNumber = playerNumber;
        }

        ~Player()
        {
            if (Connection != null) Connection.Dispose();
        }
    }
}
