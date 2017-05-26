﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AccessBattle
{
    public class GameAI : IPlayer
    {
        Game _game;
        int _playerIndex;
        Random rnd;

        public uint UID { get { return 0; } }

        virtual public string Name { get { return "Dummy"; } set {  } }

        public GameAI(Game game, int playerIndex = 2)
        {
            _game = game;
            rnd = new Random(Guid.NewGuid().GetHashCode());
            _playerIndex = playerIndex;
        }

        public virtual void PlayTurn()
        {
#pragma warning disable CC0022 // Should dispose object
            (new Task(() =>
            {

                if (_game.Phase == GamePhase.Deployment)
                {
                    DeployCards();
                }
                else if (_game.Phase == GamePhase.PlayerTurns)
                {
                    var myCards = _game.Board.OnlineCards.Where(
                        o => o.Owner != null && o.Owner.PlayerNumber == _playerIndex).ToList();

                    // First check if any card is on the Exit field
                    int exitY = _playerIndex > 1 ? 0 : 7;
                    ushort targetServerAreaX = (ushort)(_playerIndex > 1 ? 4 : 5);
                    var cardOnExit = myCards.FirstOrDefault(o => 
                            o.Location.Type == BoardFieldType.Exit &&
                            o.Location.Position.Y == exitY);
                    if (cardOnExit != null)
                    {
                        Thread.Sleep(500);
                        UiSyncHelper.Execute(() => 
                        {
                            _game.ExecuteCommand(_game.CreateMoveCommand(
                            cardOnExit.Location.Position, new Vector(targetServerAreaX, 10)));
                            _game.CurrentPlayer = (_playerIndex == 2) ? 1 : 2;
                        });
                        return;
                    }

                    // Just grab a random card and move forward
                    // Choose to move a virus or link
                    var ct = OnlineCardType.Link;
                    if (rnd.Next(0, 101) <= 40) // 40% Chance to pick virus
                        ct = OnlineCardType.Virus;
                    var cards = myCards.Where(o => o.Type == ct).ToList(); ;
                    // There is always at least one card of a type left. Otherwise game is over
                    var card = cards[rnd.Next(0, cards.Count)];
                    // Just in case card cannot move, reorder myCards:
                    myCards.Remove(card);
                    myCards.Insert(0, card);
                    foreach (var c in myCards)
                    {
                        var possibleMoves = _game.GetTargetFields(c.Location);
                        if (possibleMoves.Count == 0) continue;
                        // Choose one of the moves preferably on in directon of exit
                        int tx = 4;
                        if (c.Location.Position.X < 4) tx = 3;
                        int ty = 1;
                        if (_playerIndex == 1) ty = 7;
                        BoardField closestMove = possibleMoves[0];
                        double closestDistance = Distance(tx, ty, closestMove.Position);
                        for (int i = 1; i < possibleMoves.Count; ++i)
                        {
                            var dist = Distance(tx, ty, possibleMoves[i].Position);
                            if (dist < closestDistance)
                            {
                                closestDistance = dist;
                                closestMove = possibleMoves[i];
                            }
                        }
                        Thread.Sleep(500);
                        UiSyncHelper.Execute(() =>
                        {
                            _game.ExecuteCommand(_game.CreateMoveCommand(
                                c.Location.Position, closestMove.Position));
                            _game.CurrentPlayer = (_playerIndex == 2) ? 1 : 2;
                        });
                        return;
                    }
                }
                UiSyncHelper.Execute(() => { _game.CurrentPlayer = (_playerIndex == 2) ? 1 : 2; });
            })).Start();
#pragma warning restore CC0022 // Should dispose object
        }

        double Distance(int targetX, int targetY, Vector currentPosition)
        {
            var dx = targetX - currentPosition.X;
            var dy = targetY - currentPosition.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        protected virtual void DeployCards()
        {
            // Randomly deploy cards:
            // All cards must be stored in stack
            var depFields = _game.Board.GetPlayerDeploymentFields(_playerIndex);
            var stackFields = _game.Board.GetPlayerStackFields(_playerIndex);
            // TODO: Shuffle Stack fields randomly

            for (int i = 0; i < 8; ++i)
            {
                int index = rnd.Next(0, depFields.Count);
                var depField = depFields[index];
                depFields.Remove(depField);
                Thread.Sleep(250);
                UiSyncHelper.Execute(() =>
                {
                    if (!_game.ExecuteCommand(_game.CreateMoveCommand(stackFields[i].Position, depField.Position)))
                    {
                        Trace.WriteLine("AI: Could not depoly card!!!");
                    }
                });
            }
            //UiSyncHelper.Execute(() => { _game.Phase = GamePhase.PlayerTurns; });
        }
    }
}