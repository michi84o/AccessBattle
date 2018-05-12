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

        static void UpdateUI()
        {
            Console.Clear();
            switch (CurrentMenu)
            {
                case MenuType.Main:
                    DrawMainMenu();
                    break;
                case MenuType.Game:
                    DrawBoard();
                    break;
                case MenuType.SinglePlayer:
                    DrawSinglePlayer();
                    break;
                case MenuType.SinglePlayerDeploy:
                    DrawSinglePlayerDeploy();
                    break;
                default:
                    Console.WriteLine("Oops! Nothing to see here");
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
                    Thread.Sleep(2000);
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

            }
        }

        static void DrawSinglePlayerDeploy()
        {
            Console.WriteLine("Deployment");
        }

        static void DrawBoard()
        {

        }

    }
}
