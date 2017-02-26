using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace AccessBattle
{
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

        int _playerNumber;
        public int PlayerNumber { get { return _playerNumber; } }
        
        public Player(int playerNumber)
        {
            _playerNumber = playerNumber;
        }
        
        NetworkPlayer _client;
        /// <summary>
        /// This object is managed by GameServer. Do not dispose it!
        /// </summary>
        public NetworkPlayer Client
        {
            get { return _client; }
            set { SetProp(ref _client, value); }
        }
    }
}
