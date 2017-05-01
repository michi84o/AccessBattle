using AccessBattle.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace AccessBattle
{
    /// <summary>
    /// Class for player data.
    /// </summary>
    public class PlayerState : PropChangeNotifier
    {
        string _name; // This value is only used if Player is not set
        /// <summary>
        /// Name of player. Limited to 160 characters.
        /// </summary>
        public string Name
        {
            get
            {
                var pl = _player;
                if (pl != null)
                {
                    var n = pl.Name;
                    if (n == null) return "";
                    if (n != null && n.Length > 160)
                        n = n.Substring(0, 160);
                    return n;
                }
                return _name;
            }
            set
            {
                var n = value;
                if (n != null && n.Length > 160)
                    n = n.Substring(0, 160);
                var pl = _player;
                if (pl != null)
                {
                    pl.Name = n;
                }
                SetProp(ref _name, n);
            }
        }

        /// <summary>
        /// Points of the player (to track score if played multiple times).
        /// </summary>
        public int Points { get; set; }

        /// <summary>Player already did his virus check.</summary>
        public bool DidVirusCheck { get; set; }
        /// <summary>Player already used the 404 card.</summary>
        public bool Did404NotFound { get; set; }

        int _playerNumber;
        /// <summary>Player number (1 or 2).</summary>
        public int PlayerNumber { get { return _playerNumber; } }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="playerNumber">Player number (1 or 2).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if player number is not 1 or 2.</exception>
        public PlayerState(int playerNumber)
        {
            if (playerNumber > 2 || playerNumber < 1) throw new ArgumentOutOfRangeException(nameof(playerNumber));
            _playerNumber = playerNumber;
        }

        IPlayer _player;
        /// <summary>
        /// This object is managed by GameServer. Do not dispose it!
        /// </summary>
        public IPlayer Player
        {
            get { return _player; }
            set
            {
                if (SetProp(ref _player, value))
                {
                    if (value != null) Name = value.Name;
                }
            }
        }
    }
}
