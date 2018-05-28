using AccessBattle;
using AccessBattleAI;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace NeuralNetTrainer
{
    class Program
    {
        static uint gen = 0; // Keeps track of generations
        static int genProgress = 0;
        static uint maxGen = 0;

        static int miaWins = 0;
        static int nouWins = 0;

        static void Main(string[] args)
        {
            string dir = "Nou_AI";
            string genTxt = dir + "\\gen.txt";
            string genLogTxt = dir + "\\genLog.txt";
            Func<int, int, string> netFile = (a,b) => { return dir + "\\net" + a + "." + b + ".txt"; };

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists(genTxt)) { uint.TryParse(File.ReadAllText(genTxt), out gen); }

            Console.WriteLine("NOU AI Trainer\n");
            Console.WriteLine("Current generation: " + gen);
            Console.WriteLine("Please enter number of iterations: ");
            var inp = Console.ReadLine();
            if (!uint.TryParse(inp, out uint gen2Go))
            {
                Console.WriteLine("Not a number! Press any key to exit");
                Console.ReadKey();
                return;
            }
            Console.CursorVisible = false;

            Trainer.MaxRound = 15;

            // We have 50 AIs. All fight the same MIA instance.
            int aiCnt = 50; // Number of AIs per generation
            double mutationDelta = Nou.MutateDelta/10;
            var log = new Dictionary<int, TrainingLog>();
            for (int i = 0; i < aiCnt; ++i)
            {
                log.Add(i, new TrainingLog { AI = new Nou(), ID = i });
            }

            // Check if there are saved nets on the hard drive
            if (gen > 0) // Ignore if we start from gen 0
            {
                for (int i = 0; i < aiCnt; ++i)
                {
                    log[i].AI.ReadFromFile(0, netFile(i, 0));
                    log[i].AI.ReadFromFile(1, netFile(i, 1));
                }
            }
            var rnd = new Random(); // Used for applying seed to MIA.
            genProgress = 0;
            maxGen = gen + gen2Go;



            // Cycle through all defined generations
            for (int it = 0; it < gen2Go; ++it)
            {
                genProgress = (int)(0.5 + 100.0 * ((1.0 * it) / gen2Go));

                int curGenSeed = rnd.Next();
                int nouSeed = rnd.Next();

                #region NextGen Prep
                if (it > 0)
                {
                    // Based on 50 AIs with 5 battles:
                    // 1. Delete the 25 least scoring nets
                    // 2. Keep the best 5 nets untouched
                    // 3. Mutate the remaining 20 nets
                    // 4. Create 4 copies of the 5 best nets and mutate them (gives 20 new nets)
                    // 5. Add 5 nets so we have 50 nets again

                    // 1. + 2.
                    var sortedList = log.Select(o => o.Value).OrderByDescending(o => o.Score).ToList();
                    log.Clear();
                    for (int i = 0; i < aiCnt / 2; ++i)
                    {
                        sortedList[i].ID = i; // Update ID
                        log.Add(i, sortedList[i]);
                    }
                    // 3.
                    for (int i = 5; i < aiCnt / 2; ++i)
                    {
                        log[i].AI.Mutate(mutationDelta);
                    }
                    // 4.
                    int fillerRatio = (aiCnt / 2 - 5)/5; // =4 when aiCnt=50
                    int nId = aiCnt / 2;
                    for (int iAi = 0; iAi < 5; iAi++) // The first five AIs
                        for (int ii = 0; ii < fillerRatio; ++ii)
                        {
                            var nou = Nou.Copy(log[iAi].AI);
                            nou.Mutate(mutationDelta);
                            log[nId] = new TrainingLog() { ID = nId, AI = nou};
                            ++nId;
                        }
                    while (nId < aiCnt)
                    {
                        log.Add(nId, new TrainingLog { AI = new Nou(), ID = nId });
                        ++nId;
                    }
                }

                // Reset score at the start of each generation
                for (int ai = 0; ai < aiCnt; ++ai)
                {
                    log[ai].Score = 0;
                }

                #endregion

                Trainer trainer = null;

                // Cycle through all AIs
                Parallel.ForEach(log.Select(o => o.Value), new ParallelOptions() { MaxDegreeOfParallelism = 8 }, (aiLog) =>
                {
                    Trainer trn = new Trainer();

                    var mia = new Mia();
                    mia.SetSeed(curGenSeed);
                    mia.Depth = 1;

                    if (aiLog.ID == 0)
                    {
                        trainer = trn;
                        trn.AiDelay = 0; // This is the displayed trainer.
                    }

                   var waitHandle = new AutoResetEvent(false);
                    EventHandler handler = (s, a) =>
                    {
                        if (trn.GameOver)
                            try { waitHandle.Set(); } catch { }
                    };
                    trn.NeedsUiUpdate += handler;

                    aiLog.AI.DeploySeed = nouSeed;
                    // Run game:
                    trn.StartGame(aiLog.AI, mia);

                    waitHandle.WaitOne( 100*Trainer.MaxRound ); // Give 100ms per round
                    trn.NeedsUiUpdate -= handler;
                    trn.Abort = true;

                    if (trn.Game.Phase == GamePhase.Player1Win) { Interlocked.Increment(ref nouWins); }
                    else if (trn.Game.Phase == GamePhase.Player2Win) { Interlocked.Increment(ref miaWins); }

                    // Post Game: Add score
                    aiLog.Score = aiLog.AI.Fitness();

                });
                genProgress = (int)(0.5 + 100.0 * ((it + 1.0) / gen2Go));
                if (trainer != null) DrawBoard(trainer);

                // Log score to file
                ++gen;
                try
                {
                    var genSorted = log.Select(o => o.Value).OrderByDescending(o => o.Score).ToList();
                    using (var f = File.AppendText(genLogTxt))
                    {
                        f.WriteLine("Gen:\t" + gen + "\tScore:\t" + genSorted[0].Score.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
                catch { }
            }

            // Save all current Nets
            for (int i = 0; i < aiCnt; ++i)
            {
                log[i].AI.Net1.SaveAsFile(netFile(i, 0));
                log[i].AI.Net2.SaveAsFile(netFile(i, 1));
            }
            File.WriteAllText(genTxt, gen.ToString());

            System.Threading.Thread.Sleep(500); // Wait for UI updates
            Console.CursorVisible = true;
            Console.WriteLine("Finished. Press any key to exit");
            Console.ReadKey();
        }

        static readonly object DrawLock = new object();
        static void DrawBoard(Trainer trainer)
        {
            lock (DrawLock)
            {
                Console.SetCursorPosition(0, 0);
                var b = trainer.Game.Board;

                string upper = "   ┌───┬───┬───┬═══┬═══┬───┬───┬───┐";
                //string row =   "  │   │   │   │   │   │   │   │   │";
                string middle = "   ├───┼───┼───┼───┼───┼───┼───┼───┤";
                string lower = "   └───┴───┴───┴═══┴═══┴───┴───┴───┘";
                string xlabel = "     a   b   c   d   e   f   g   h";

                Console.WriteLine(upper);
                for (int y = 8; y > 0; --y)
                {
                    Console.Write(" " + y + " │");
                    for (int x = 0; x < 8; ++x)
                    {
                        var card = b[x, y - 1].Card;
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
                                    if (onlCard.Type == OnlineCardType.Unknown) Console.Write(onlCard.HasBoost ? "-X-" : " X ");
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
                        DrawStack(9, trainer);
                        Console.WriteLine();
                    }
                    else if (y == 7)
                    {
                        Console.Write(" P1: ");
                        DrawStack(8, trainer);
                        Console.WriteLine();
                        Console.WriteLine(middle);
                    }
                    else if (y == 6)
                    {
                        Console.WriteLine(" Game Phase: " + trainer.Game.Phase + " " );
                        Console.WriteLine(middle + " Round: " + trainer.Round + "  ");
                    }
                    else if (y == 5)
                    {
                        Console.WriteLine(" Generation: " + (gen+1) + "/" + maxGen + "  " );
                        Console.WriteLine(middle + " Progress: " + genProgress + "%  ");
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

        static void DrawStack(int y, Trainer trainer) // P1: y=8, P2: y=9
        {
            var b = trainer.Game.Board;
            for (int s = 0; s < 8; ++s)
            {
                if (s == 4) Console.Write(" ");
                var card = b[s, y].Card as OnlineCard;
                if (card == null)
                {
                    Console.Write("_");
                    continue;
                }

                var oldCol = Console.ForegroundColor;
                Console.ForegroundColor = card.Owner.PlayerNumber == 1 ? ConsoleColor.Cyan : ConsoleColor.Red;

                if (card.Type == OnlineCardType.Unknown) Console.Write("X");
                else if (card.Type == OnlineCardType.Link) Console.Write("L");
                else if (card.Type == OnlineCardType.Virus) Console.Write("V");
                else Console.Write("?");

                Console.ForegroundColor = oldCol;
            }
            if (y == 8)
            {
                Console.Write(" Nou: " + nouWins + " wins");
            }
            else if (y == 9)
            {
                Console.Write(" Mia: " + miaWins + " wins");
            }
        }
    }
}
