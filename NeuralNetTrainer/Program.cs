using AccessBattle;
using AccessBattleAI;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using AccessBattle.Networking.Packets;

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
            Console.WriteLine("NOU AI Trainer\n");
            Console.WriteLine("Select mode: ");
            Console.WriteLine("\t1: Genetic algorithm against MIA AI. (default)");
            Console.WriteLine("\t2: Backproagation learning from MIA AI.");
            var usrInput = Console.ReadLine();

            if (usrInput == "2")
            {
                BackPropTraining();
                return;
            }
            Console.Clear();
            Console.WriteLine("NOU AI Trainer\n");

            string dir = "Nou_AI";
            string genTxt = dir + "\\gen.txt";
            string genLogTxt = dir + "\\genLog.txt";
            Func<int, int, string> netFile = (a,b) => { return dir + "\\net" + a + "." + b + ".txt"; };

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists(genTxt)) { uint.TryParse(File.ReadAllText(genTxt), out gen); }

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
            double mutationDelta = Nou.MutateDelta/3;
            var log = new Dictionary<int, TrainingLog>();
            var fac = new NouFactory(); // Loads pretrained NouAi_0.txt
            for (int i = 0; i < aiCnt; ++i)
            {
                log.Add(i, new TrainingLog { AI = (Nou)fac.CreateInstance(), ID = i });
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

            byte mutateFlags = 1; // Only train net1.

            // Cycle through all defined generations
            for (int it = 0; it < gen2Go; ++it)
            {
                genProgress = (int)(0.5 + 100.0 * ((1.0 * it) / gen2Go));

                int curGenSeed = rnd.Next();
                int nouSeed = rnd.Next();

                #region NextGen Prep
                if (it > 0)
                {
                    // Based on 50 AIs:
                    // 1. Delete the 25 least scoring nets
                    // 2. Keep the best 5 nets untouched    //  +5 ->  5
                    // 3. Mutate the remaining 20 nets      // +20 -> 25
                    // 4. Create 4 copies of the 5 best
                    //    nets and mutate them              // +20 -> 45
                    // 5. Add 5 ranom nets                  //  +5 -> 50

                    // We do not create new randoms AIs.
                    // All our progress might get lost if a random ai accidentally scores high

                    // 1. + 2.
                    // Sort by score
                    var sortedList = log.Select(o => o.Value).OrderByDescending(o => o.Score).ToList();
                    log.Clear(); // Clear list
                    for (int i = 0; i < aiCnt / 2; ++i) // Add the 25 best nets
                    {
                        sortedList[i].ID = i; // Update ID
                        log.Add(i, sortedList[i]);
                    }
                    // 3. Mutate the last 20 nets in the new list
                    for (int i = 5; i < aiCnt / 2; ++i)
                    {
                        log[i].AI.Mutate(mutationDelta, mutateFlags);
                    }
                    // 4.
                    int fillerRatio = (aiCnt / 2 - 5)/4; // =5 when aiCnt=50
                    int nId = aiCnt / 2;
                    for (int iAi = 0; iAi < 5; iAi++) // The first five AIs
                        for (int ii = 0; ii < fillerRatio; ++ii)
                        {
                            var nou = Nou.Copy(log[iAi].AI);
                            nou.Mutate(mutationDelta, mutateFlags);
                            log[nId] = new TrainingLog() { ID = nId, AI = nou};
                            ++nId;
                        }
                    // 5.
                    while (log.Count < aiCnt)
                    {
                        log.Add(log.Count, new TrainingLog { ID = log.Count, AI = (Nou)fac.CreateInstance() });
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

        static void BackPropTraining()
        {
            var mia = new Mia();
            Nou nou = (Nou)new NouFactory().CreateInstance();

            mia.IsAiHost = true;
            nou.IsAiHost = true;

            var p1 = new PlayerState(1);
            var p2 = new PlayerState(2);
            var p1Sync = new PlayerState.Sync { PlayerNumber = 1 };
            var p2Sync = new PlayerState.Sync { PlayerNumber = 2 };

            // Generate random Board states and let Nou learn what Mia would do.

            var rnd = new Random();

            Console.Write("Training....");
            var left = Console.CursorLeft;
            var top = Console.CursorTop;
            for (int i = 0; i < 10000; ++i)
            {
                Console.SetCursorPosition(left, top);
                Console.Write(i+1);

                var sync = new GameSync
                {
                    Player1 = p1Sync,
                    Player2 = p2Sync
                };
                sync.Phase = GamePhase.Player1Turn;
                sync.FieldsWithCards = new List<BoardField.Sync>();

                // Random Board
                // 8 Cards on each side, 4x Virus, 4x Link
                List<OnlineCard> cards = new List<OnlineCard>();
                for (int j = 0; j < 4; ++j)
                {
                    cards.Add(new OnlineCard { Owner = p1, Type = OnlineCardType.Link });
                    cards.Add(new OnlineCard { Owner = p2, Type = OnlineCardType.Link });
                    cards.Add(new OnlineCard { Owner = p1, Type = OnlineCardType.Virus });
                    cards.Add(new OnlineCard { Owner = p2, Type = OnlineCardType.Virus });
                }

                var board = new List<BoardField>();
                for (ushort y = 0; y < 11; ++y)
                    for (ushort x = 0; x < 8; ++x)
                        board.Add(new BoardField(x, y));

                while (cards.Count > 0)
                {
                    // 90% propability of beeing on board ( y <= 7 )
                    int ymax = rnd.NextDouble() <= .9 ? 7 : 9;

                    int boardIndex = rnd.Next(board.Count);
                    if (board[boardIndex].Y > ymax) continue;

                    var field = board[boardIndex];
                    board.RemoveAt(boardIndex);

                    field.Card = cards[0];
                    cards.RemoveAt(0);

                    sync.FieldsWithCards.Add(field.GetSync());
                }

                mia.Synchronize(sync);
                nou.Train(sync, mia.PlayTurn());

            }
            Console.WriteLine("\nFinished. Press any key to continue");
            Console.ReadKey();
        }
    }
}
