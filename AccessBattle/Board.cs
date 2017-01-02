using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccessBattle
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
    /// (0,0) is a1, (7,7) is h8
    /// 
    /// Stack:
    /// For simplification, 
    /// the first 4 fields are always links.
    /// Board orientation is ignored
    /// Player1: Fields (0,8) - (7,8)
    /// Player2: Fields (0,9) - (7,9)
    /// </summary>
    public class Board
    {
        // TODO: Software Design
        public Game CurrentGame { get; set; }
        
        public BoardField[,] Fields { get; private set; }

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

        BoardField[] _player1DeploymentFields;
        
        public List<BoardField> Player1DeploymentFields
        {
            get { return _player1DeploymentFields.ToList(); }
        }
        BoardField[] _player1StackFields; // First 4 are link fields
        public List<BoardField> Player1StackFields
        {
            get { return _player1StackFields.ToList(); }
        }

        public Board()
        {
            Fields = new BoardField[8, 10];
            for (int x = 0; x < 8; ++x)
                for (int y = 0; y < 10; ++y)
                    Fields[x, y] = new BoardField(x,y, this);

            _player1DeploymentFields = new BoardField[] 
            { Fields[0,0],Fields[1,0],Fields[2,0],Fields[3,1],Fields[4,1],Fields[5,0],Fields[6,0],Fields[7,0] };
            _player1StackFields = new BoardField[]
            { Fields[0,8],Fields[1,8],Fields[2,8],Fields[3,8],Fields[4,8],Fields[5,8],Fields[6,8],Fields[7,8],  };
        }

        public void DeployCard(OnlineCard card, Vector position)
        {            
            Fields[position.X, position.Y].Card = card;
        }
    }

    public class BoardField : PropChangeNotifier
    {
        public Vector Position { get; private set; }
        public Board Parent { get; private set; }
        OnlineCard _card;
        public OnlineCard Card
        {
            get { return _card; }
            set
            {
                if (_card == value) return;
                _card = value;
                if (_card != null)
                    _card.Location = this;
                OnPropertyChanged();
            }
        }

        bool _isHighlighted;
        /// <summary>
        /// Fields are highlighted when the player can interact with them
        /// </summary>
        public bool IsHighlighted
        {
            get { return _isHighlighted; }
            set
            {
                if (_isHighlighted == value) return;
                _isHighlighted = value;
                OnPropertyChanged();
            }
        }

        public BoardField(int x, int y, Board parent)
        {
            Position = new Vector(x,y);
            Parent = parent;
        }
    }
}
