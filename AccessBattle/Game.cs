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
        PlayerTurns,
        GameOver
    }

    public enum PlayerAction
    {
        SelectCard,
        MoveSelectedCard,
        TakeSelectedCard,
        PlaceBoost,
        TakeBoost,
        Error404,
    }

    public class Game : PropChangeNotifier
    {
        public Player[] Players { get; private set; }
        int _currentPlayer;
        public int CurrentPlayer
        {
            get { return _currentPlayer; }
            set { SetProp(ref _currentPlayer, value); }
        }

        public Board Board { get; private set; }
        GamePhase _phase;
        public GamePhase Phase
        {
            get { return _phase; }
            set
            {
                if (SetProp(ref _phase, value))
                    OnPhaseChanged();
            }
        }

        void OnPhaseChanged()
        {
            var phase = _phase;
            if (phase == GamePhase.Init)
            {
                for (int x = 0; x < 8; ++x)
                    for (int y = 0; y < 10; ++y)
                        Board.Fields[x, y].Card = null;
                // Give each player 8 cards on his stack
                for (int p = 0; p < 2; ++p)
                    for (ushort i = 0; i < 4; ++i)
                    {
                        var pos = (ushort)(p * 8 + i);
                        // TODO: Randomization to prevent cheating ??? 
                        // Links
                        Board.OnlineCards[pos].Owner = Players[p];
                        Board.OnlineCards[pos].Type = OnlineCardType.Link;
                        Board.PlaceCard(i, (ushort)(8 + p), Board.OnlineCards[pos]);
                        // Viruses
                        pos += 4;
                        Board.OnlineCards[pos].Owner = Players[p];
                        Board.OnlineCards[pos].Type = OnlineCardType.Virus;
                        Board.PlaceCard((ushort)(i + 4), (ushort)(8 + p), Board.OnlineCards[pos]);
                    }
            }
        }

        public Game()
        {
            Players = new Player[]
            {
                new Player { Name = "Player 1" , PlayerNumber = 1 },
                new Player { Name = "Player 2" , PlayerNumber = 2 }
            };            
            Board = new Board();
            Phase = GamePhase.Init;
            OnPhaseChanged();
        }
    }
}
