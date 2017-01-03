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

        public Board Board { get; private set; }

        LinkCard[] LinkCards = new LinkCard[8];
        VirusCard[] VirusCards = new VirusCard[8];

        public DeploymentState DeploymentState { get; private set; }

        GamePhase _phase;
        public GamePhase Phase
        {
            get { return _phase; }
            set
            {
                if (value == _phase) return;
                _phase = value;
                OnPhaseChanged();
                OnPropertyChanged();
            }
        }

        void OnPhaseChanged()
        {
            // Reset states of fields
            for (int x = 0; x < 8; ++x)
                for (int y = 0; y < 10; ++y)
                    Board.Fields[x, y].IsHighlighted = false;

            var phase = _phase;
            if (phase == GamePhase.Init)
            {
                for (int x = 0; x < 8; ++x)
                    for (int y = 0; y < 10; ++y)
                        Board.Fields[x, y].Card = null;
                // Give each player 8 cards on his stack
                for (int i = 0; i < 4; ++i)
                {
                    LinkCards[i].Owner = Player1;
                    Board.Fields[i, 8].Card = LinkCards[i]; // Location of card is auto updated
                    VirusCards[i].Owner = Player1;
                    Board.Fields[i + 4, 8].Card = VirusCards[i];
                }
                for (int i = 4; i < 8; ++i)
                {
                    LinkCards[i].Owner = Player2;
                    Board.Fields[i-4, 9].Card = LinkCards[i];
                    VirusCards[i].Owner = Player2;
                    Board.Fields[i, 9].Card = VirusCards[i];
                }
            }

            if (phase == GamePhase.Deployment)
            {
                foreach (var field in Board.Player1DeploymentFields)
                    field.IsHighlighted = true;
            }
        }

        public Game()
        {
            Player1 = new Player { Name = "Player 1" , PlayerNumber = 1 };
            Player2 = new Player { Name = "Player 2" , PlayerNumber = 2 };
            Board = new Board();
            DeploymentState = new DeploymentState();
            for (int i=0; i<8; ++i)
            {                
                LinkCards[i] = new LinkCard();
                VirusCards[i] = new VirusCard();
            }

            Phase = GamePhase.Init;
            OnPhaseChanged();
        }
    }

    public class DeploymentState
    {
        public List<OnlineCard> CardsToDeploy { get; private set; }

        public DeploymentState()
        {
            CardsToDeploy = new List<OnlineCard>();
        }
    }
}
