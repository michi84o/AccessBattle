using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RaiNet
{
    /// <summary>
    /// Board is 8x8 using Chess notation 
    /// as base for positions
    ///   a b ... g h
    /// 8             8
    /// 7             7
    /// ...         ...
    /// 2             2
    /// 1             1
    ///   a b ... g h
    /// 
    /// X is horizontal, Y is vertical
    /// (1,1) is a1, (8,8) is h8
    /// 
    /// </summary>
    public class Board
    {
        public Player Player1 { get; set; }
        public Player Player2 { get; set; }

        public List<OnlineCard> Cards;
        public BoardField[,] Fields;

        List<Vector> GetMoves(Player player, Vector position)
        {
            var list = new List<Vector>();            

            return list;
        }

        bool IsMoveAllowed(Player player, Vector position, Vector target)
        {

            return false;
        }

        bool Move(Player player, Vector position, Vector target)
        {
            if (!IsMoveAllowed(player, position, target))
                return false;
            return false;
        }

        public Board()
        {
            Fields = new BoardField[8, 8];
            for (int x = 0; x < 8; ++x)
                for (int y = 0; y < 8; ++y)
                    Fields[x, y] = new BoardField(x,y);
            Player1 = new Player() { Name = "Player 1" };
            Player2 = new Player() { Name = "Player 2" };
        }
    }

    public class Vector
    {
        int X;
        int Y;
        public Vector(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public class BoardField
    {
        Vector Position;
        OnlineCard Card;

        public BoardField(int x, int y)
        {
            Position = new Vector(x,y);
        }
    }
}
