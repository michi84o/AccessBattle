using AccessBattle;
using AccessBattle.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AccessBattleConsole
{
    class Program
    {
        static void Main(string[] args)
        {            
            string line = null;
            while (line != "exit")
            {
                NextAction?.Invoke(line);
                NextAction = null;
                UpdateUI();
                Console.WriteLine();
                line = Console.ReadLine();                
            }                        
        }

        static MenuType CurrentMenu = MenuType.Main;
        static Action<string> NextAction;
        static List<IArtificialIntelligenceFactory> AiPlugins;
        static Game CurGame;
        static bool IsAiBattle = false;
        static bool QuitAiBattleRequested = false;
        static int Round = 0;
        static int AiCommandDelay = 0;

        static readonly object UiLock = new object();
        static void UpdateUI(bool clear = true)
        {
            lock (UiLock) // A task might trigger change when game phase changed
            {
                if (clear)
                    Console.Clear();
                else
                    Console.SetCursorPosition(0, 0);

                switch (CurrentMenu)
                {
                    case MenuType.Main:
                        DrawMainMenu();
                        break;
                    case MenuType.SinglePlayer:
                        DrawSinglePlayer();
                        break;
                    case MenuType.Game:
                        DrawGame();
                        break;
                    case MenuType.AiBattle:
                        DrawAiBattle();
                        break;
                    default:
                        Console.WriteLine("Oops! Nothing to see here");
                        Thread.Sleep(500);
                        break;
                }
            }
        }

        static void Title()
        {
            Console.WriteLine("ACCESS BATTLE - Supaa Hacka Edition\n");
        }

        static void DrawMainMenu()
        {
            Title();
            Console.WriteLine("Select Mode:\n");
            Console.WriteLine("1. Singleplayer");
            Console.WriteLine("2. Multiplayer");
            Console.WriteLine("3. AI vs AI");
            Console.WriteLine("\nEnter 'exit' to quit.");
            NextAction = MainMenuApplyChoice;
        }

        static void MainMenuApplyChoice(string str)
        {
            switch (str)
            {
                case "1":
                    CurrentMenu = MenuType.SinglePlayer;
                    break;
                case "2":
                    Console.WriteLine("Not implemented yet!");
                    Thread.Sleep(1000);
                    CurrentMenu = MenuType.Main;
                    break;
                case "3":
                    CurrentMenu = MenuType.AiBattle;
                    break;
            }

        }

        static void DrawAiPluginList()
        {
            if (AiPlugins == null)
                AiPlugins = PluginHandler.Instance.GetPlugins<IArtificialIntelligenceFactory>();
            // TODO: Limit height and enable scrolling or flipping page
            int i = 0;
            foreach (var plug in AiPlugins)
            {
                Console.WriteLine(++i + ". " + plug.Metadata.Name);
            }
        }

        static void DrawSinglePlayer()
        {
            Console.WriteLine("Singleplayer Mode\n");

            Console.WriteLine("0. Return to Main Menu\n");

            Console.WriteLine("Select an AI opponent:\n");

            DrawAiPluginList();
            
            NextAction = SinglePlayerApplyChoice;
        }

        static void SinglePlayerApplyChoice(string str)
        {
            if (str == "0")
            {
                CurrentMenu = MenuType.Main;
                return;
            }

            IArtificialIntelligenceFactory selectedPlugin = null;
            int i = 0;
            foreach (var plugin in AiPlugins)
            {
                if (str == (++i).ToString())
                {
                    selectedPlugin = plugin;
                    break;
                }
            }
            if (selectedPlugin != null)
            {
                CurrentMenu = MenuType.Game;
                IsAiBattle = false;
                // Set up Game
                CleanupGame();
                CurGame = new LocalGame() { AiCommandDelay = AiCommandDelay };
                ((LocalGame)CurGame).SetAi(selectedPlugin.CreateInstance());
                CurGame.InitGame();
                CurGame.PropertyChanged += CurGame_PropertyChanged;
                ((LocalGame)CurGame).SyncRequired += Program_SyncRequired;
            }
        }

        private static void CurGame_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Game.Phase))
            {
                UpdateUI(CurrentMenu != MenuType.Game);

                var game = sender as Game;
                var locGame = game as LocalGame;

                if (locGame != null && IsAiBattle && locGame.Phase == GamePhase.Player1Turn && !QuitAiBattleRequested)
                {
                    ++Round;
                    Task.Run(() =>
                    {                        
                        if (AiCommandDelay > 0)
                            Thread.Sleep(AiCommandDelay);
                        locGame.AiPlayer1Move();                        
                    });
                }                
            }
        }

        static void Program_SyncRequired(object sender, EventArgs e)
        {
            // Fired when AI player 2 finishes move
            // Do nothing for now
            // Let Phase change trigger UI update
        }

        static void DrawGame()
        {
            if (CurGame == null)
            {
                Console.WriteLine("Game was not set up properly.\nPress enter to restart.");
                CurrentMenu = MenuType.Main;                           
                return;
            }

            // Draw the board first
            DrawBoard(!IsAiBattle);

            if (CurGame.Phase == GamePhase.Player1Win)
            {
                Console.WriteLine("\nYou win!");
            }
            else if (CurGame.Phase == GamePhase.Player2Win)
            {
                Console.WriteLine("\nYou lose!");
            }
            else if (CurGame.Phase == GamePhase.Aborted)
            {
                Console.WriteLine("\nGame over!");
            }

            if (CurGame.Phase == GamePhase.Player1Win ||
                CurGame.Phase == GamePhase.Player2Win ||
                CurGame.Phase == GamePhase.Aborted)
            {
                Console.WriteLine("\nPress enter to return to main menu");
                CurrentMenu = MenuType.Main;
                NextAction = null;
                return;
            }

            if (!IsAiBattle)
            {

                if (CurGame.Phase == GamePhase.Player2Turn)
                {
                    Console.WriteLine("\nPlayer 2 turn, please wait...");
                }
                else
                    Console.WriteLine("\nEnter a game command (? for help):");
            }
            else
            {
                Console.WriteLine("\nAI Battle. Enter 'quit' to stop game");
            }

            NextAction = HandleGameCommand;
        }

        static void HandleGameCommand(string str)
        {
            if (IsAiBattle)
            {
                if (str == "quit")
                {

                }
            }

            if (str == "?")
            {
                Console.WriteLine("Command      Syntax             Example");
                Console.WriteLine("-------------------------------------------");
                Console.WriteLine("Deploy       dp card list       dp VVVVLLLL");
                Console.WriteLine("Move         mv x1,y1,x2,y2     mv a1,b1");
                Console.WriteLine("Boost        bs x1,y1,e         bs a1,1");
                Console.WriteLine("Firewall     fw x1,y1,e         fw a1,1");
                Console.WriteLine("Virus Check  vc x1,y1           vc a1");
                Console.WriteLine("Error 404    er x1,y1,x2,y2,s   er a1,b2,1");
                Console.WriteLine();
                Console.WriteLine("x1;x2 = horizontal coordinate (a-h or 1-8)");
                Console.WriteLine("y1;y2 = vertical   coordinate (a-h or 1-8)");
                Console.WriteLine("e=enable: 1=yes, 0=no");
                Console.WriteLine("s=switch: 1=yes, 0=no");
                Console.WriteLine();
                Console.WriteLine("Press enter to continue");
                Console.ReadLine();
            }
            else
            {                
                if (CurGame?.ExecuteCommand(str, 1) != true)
                {
                    Console.WriteLine("Wrong command!");
                    Thread.Sleep(1000);
                }
            }
        }

        static void DrawStack(int y, bool hideOpponent) // P1: y=8, P2: y=9
        {
            var b = CurGame.Board;
            for (int s = 0; s < 8; ++s)
            {
                if (s == 4) Console.Write(" ");
                var card = b[s, y].Card as OnlineCard;
                if (card == null)
                {
                    Console.Write("_");
                    continue;
                }
                if (card.Type == OnlineCardType.Unknown || hideOpponent && !card.IsFaceUp && card.Owner.PlayerNumber == 2)
                    Console.Write("X");
                else if (card.Type == OnlineCardType.Link) Console.Write("L");
                else if (card.Type == OnlineCardType.Virus) Console.Write("V");
                else Console.Write("?");

                var oldCol = Console.ForegroundColor;
                Console.ForegroundColor = card.Owner.PlayerNumber == 1 ? ConsoleColor.Cyan : ConsoleColor.Red;
                Console.ForegroundColor = oldCol;
            }
        }

        static void DrawBoard(bool hideOpponent = true)
        {
            var b = CurGame.Board;            

            string upper =  "   ┌───┬───┬───┬═══┬═══┬───┬───┬───┐";
          //string row =     "  │   │   │   │   │   │   │   │   │";
            string middle = "   ├───┼───┼───┼───┼───┼───┼───┼───┤";
            string lower =  "   └───┴───┴───┴═══┴═══┴───┴───┴───┘";
            string xlabel = "     a   b   c   d   e   f   g   h";

            Console.WriteLine(upper);
            for (int y = 8; y > 0; --y)
            {
                Console.Write(" " + y + " │");
                for (int x = 0; x < 8; ++x)
                {                    
                    var card = b[x, y-1].Card;
                    if (card == null)
                        Console.Write("   ");
                    else 
                    {
                        var oldCol = Console.ForegroundColor;
                        Console.ForegroundColor = card.Owner.PlayerNumber == 1 ? ConsoleColor.Cyan : ConsoleColor.Red;
                        if (card is FirewallCard) Console.Write(" F ");
                        else
                        {
                            var onlCard = card as OnlineCard;
                            if (onlCard == null) Console.Write("err");
                            else
                            {                                
                                if (onlCard.Type == OnlineCardType.Unknown || hideOpponent && !onlCard.IsFaceUp && card.Owner.PlayerNumber == 2)
                                    Console.Write(onlCard.HasBoost ? "-X-" : " X ");
                                else if (onlCard.Type == OnlineCardType.Link) Console.Write(onlCard.HasBoost ? "-L-" : " L ");
                                else if (onlCard.Type == OnlineCardType.Virus) Console.Write(onlCard.HasBoost ? "-V-" : " V ");
                                else Console.Write("ERR");
                            }
                        }
                        Console.ForegroundColor = oldCol;
                    }
                    Console.Write("│");
                }
                if (y == 8)
                {
                    Console.WriteLine(" Stack: ");
                    Console.Write(middle + " P2: ");
                    DrawStack(9, hideOpponent);
                    Console.WriteLine();
                }
                else if (y == 7)
                {
                    Console.Write(" P1: ");
                    DrawStack(8, hideOpponent);
                    Console.WriteLine();
                    Console.WriteLine(middle);
                }
                else if (y == 6)
                {
                    Console.WriteLine(" Game Phase: " + CurGame.Phase);
                    Console.WriteLine(middle + " Round: " + Round);
                }
                else if (y == 3)
                {
                    Console.WriteLine(" Server Location: 6,11 ");
                    Console.WriteLine(middle);
                }
                else if (y == 2)
                {
                    Console.WriteLine(" Board: ");
                    Console.WriteLine(middle + " X = Unknown");
                }
                else if (y == 1)
                {
                    Console.WriteLine(" L = Link ");
                    Console.WriteLine(lower + " V = Virus");
                }
                else
                {
                    Console.WriteLine();
                    if (y > 1) Console.WriteLine(middle);
                }
            }
            //Console.WriteLine(lower);
            Console.WriteLine(xlabel);

        }

        static void DrawAiBattle()
        {
            Console.WriteLine("AI vs AI Mode\n");            

            Console.WriteLine("0. Return to Main Menu\n");

            Console.WriteLine("Select two AI opponents:");
            Console.WriteLine("Separate choice by comma\n");

            if (AiPlugins == null)
                AiPlugins = PluginHandler.Instance.GetPlugins<IArtificialIntelligenceFactory>();

            DrawAiPluginList();

            NextAction = AiBattleApplyChoice;
        }

        static void AiBattleApplyChoice(string str)
        {
            if (str == "0")
            {
                CurrentMenu = MenuType.Main;
                return;
            }

            var spl = str.Split(',');

            if (spl.Length != 2)
            {
                Console.WriteLine("Invalid input");
                Thread.Sleep(1000);
                return;
            }

            IArtificialIntelligenceFactory[] factories = new IArtificialIntelligenceFactory[2];
            for (int n = 0; n < 2; ++n)
            {
                int i = 0;
                foreach (var plugin in AiPlugins)
                {
                    var choice = spl[n].Trim();
                    if (choice == (++i).ToString())
                    {
                        factories[n] = plugin;
                        break;
                    }
                }
            }

            if (factories[0] == null || factories[1] == null)
            {
                Console.WriteLine("Invalid choice");
                Thread.Sleep(1000);
                return;
            }

            CurrentMenu = MenuType.Game;
            IsAiBattle = true;
            // Set up Game
            CleanupGame();
            
            CurGame = new LocalGame() { AiCommandDelay = 1000 };
            ((LocalGame)CurGame).SetAi(factories[0].CreateInstance(), 1);
            ((LocalGame)CurGame).SetAi(factories[1].CreateInstance(), 2);
            CurGame.InitGame();
            CurGame.PropertyChanged += CurGame_PropertyChanged;
            ((LocalGame)CurGame).SyncRequired += Program_SyncRequired;
            
            UpdateUI();
            ++Round;
            ((LocalGame)CurGame).AiPlayer1Move();
        }        

        static void CleanupGame()
        {
            Round = 0;
            QuitAiBattleRequested = false;
            if (CurGame != null)
            {
                CurGame.PropertyChanged -= CurGame_PropertyChanged;
                if (CurGame is LocalGame)
                {
                    ((LocalGame)CurGame).SyncRequired -= Program_SyncRequired;
                }
                // CurGame.Dispose(); TODO
            }
        }
    }
}
