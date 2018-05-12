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

        static void UpdateUI()
        {
            Console.Clear();
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
                default:
                    Console.WriteLine("Oops! Nothing to see here");
                    Thread.Sleep(500);
                    break;
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
            }

        }

        static void DrawSinglePlayer()
        {
            Console.WriteLine("Singleplayer Mode\n");

            Console.WriteLine("0. Return to Main Menu\n");

            if (AiPlugins == null)
                AiPlugins =  PluginHandler.Instance.GetPlugins<IArtificialIntelligenceFactory>();

            // TODO: Limit height and enable scrolling or flipping page
            int i = 0;
            foreach (var plug in AiPlugins)
            {
                Console.WriteLine(++i + ". " + plug.Metadata.Name);
            }
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
                // Set up Game
                if (CurGame != null)
                {
                    // CurGame.Dispose(); TODO
                }
                CurGame = new LocalGame();
                ((LocalGame)CurGame).SetAi(selectedPlugin.CreateInstance());
                CurGame.InitGame();
            }
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
            DrawBoard();

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
                return;
            }            

            Console.WriteLine("\nEnter a game command (? for help):");
            NextAction = HandleGameCommand;
        }

        static void HandleGameCommand(string str)
        {
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
                if (!CurGame.ExecuteCommand(str, 1))
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
                                if (onlCard.Type == OnlineCardType.Unknown || hideOpponent && !onlCard.IsFaceUp && card.Owner.PlayerNumber == 2) Console.Write(" X ");
                                else if (onlCard.Type == OnlineCardType.Link) Console.Write(" L ");
                                else if (onlCard.Type == OnlineCardType.Virus) Console.Write(" V ");
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
                    Console.WriteLine(middle);
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

    }
}
