using AccessBattle.Networking;
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
        /// <summary>
        /// Name of player. Limited to 160 characters.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                var n = value;
                if (n != null && n.Length > 160)
                    n = n.Substring(0, 160);
                SetProp(ref _name, n);
            }
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
