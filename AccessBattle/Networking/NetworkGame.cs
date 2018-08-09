using System;
using System.Diagnostics;

namespace AccessBattle.Networking
{
    /// <summary>
    /// Extension of game that is used by the game server.
    /// </summary>
    public class NetworkGame : Game
    {
        object _locker = new object();

        /// <summary>
        /// Used by the server to determine if a rematch was requested.
        /// </summary>
        public bool[] RematchRequested { get; } = new bool[] { false, false };

        /// <summary>Used to detect timeouts.</summary>
        Stopwatch _inactivityClock = new Stopwatch();

        /// <summary>
        /// Resets the internal inactivity timer.
        /// This is called automatically if game phase changes.
        /// </summary>
        public void ResetInactivityTimeout()
        {
            _inactivityClock.Restart();
        }

        /// <summary>Used to detect timeouts.</summary>
        public bool CheckForInactivityTimeout(TimeSpan limit)
        {
            return _inactivityClock.Elapsed > limit;
        }

        /// <summary>
        /// Constructor that applies a unique id to the game.
        /// </summary>
        /// <param name="uid">ID of this game.</param>
        public NetworkGame(uint uid)
        {
            _uid = uid;
            _inactivityClock.Start();
            PropertyChanged += (s, a) =>
            {
                if (a.PropertyName == nameof(Phase))
                {
                    ResetInactivityTimeout();
                }
            };
        }

        string _name;
        /// <summary>
        /// This game's name.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { SetProp(ref _name, value); }
        }

        uint _uid;
        /// <summary>
        /// This game's ID on the server.
        /// </summary>
        public uint UID { get { return _uid; } }

        /// <summary>
        /// Changes the UID. Should only be used for network play when player joined a game.
        /// </summary>
        /// <param name="uid">New UID.</param>
        public void SetUid(uint uid)
        {
            SetProp(ref _uid, uid);
        }

        /// <summary>
        /// Starts joining a player. Player 1 must accept. Then player 2 must accept after waiting.
        /// </summary>
        /// <param name="player">Player to join.</param>
        /// <returns></returns>
        public bool BeginJoinPlayer(IPlayer player)
        {
            var result = false;
            lock (_locker)
            {
                if (Phase == GamePhase.WaitingForPlayers)
                {
                    Phase = GamePhase.PlayerJoining;
                    Players[1].Player = player;
                    result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Confirms the joining process and starts Game Init phase.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="accepted"></param>
        /// <returns></returns>
        public bool JoinPlayer(IPlayer player, bool accepted)
        {
            var result = false;
            lock (_locker)
            {
                if (Phase == GamePhase.PlayerJoining && Players[1].Player == player)
                {
                    if (!accepted)
                    {
                        Players[1].Player = null;
                        Phase = GamePhase.WaitingForPlayers;
                    }
                    // If accepted, server must call InitGame
                    result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Lets player exit a game. Game is automatically lost if game was running.
        /// </summary>
        /// <param name="player"></param>
        public void ExitGame(IPlayer player)
        {
            if (player == null)
            {
                Phase = GamePhase.Aborted;
                return;
            }

            if (Phase == GamePhase.Player1Turn || Phase == GamePhase.Player2Turn)
            {
                if (Players[0]?.Player == player) Phase = GamePhase.Player2Win;
                if (Players[1]?.Player == player) Phase = GamePhase.Player1Win;
            }
            if (Phase != GamePhase.Player1Win || Phase != GamePhase.Player2Win)
                Phase = GamePhase.Aborted;
        }
    }
}
