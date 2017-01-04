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
    public class Board : PropChangeNotifier
    {
        public BoardField[,] Fields { get; private set; }
        public List<OnlineCard> OnlineCards { get; private set; }        
        public List<FirewallCard> Firewalls { get; private set; }

        #region Little Helpers
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
        #endregion

        public Board()
        {
            Fields = new BoardField[8, 10];
            for (ushort x = 0; x < 8; ++x)
                for (ushort y = 0; y < 10; ++y)
                    Fields[x, y] = new BoardField(x,y);

            OnlineCards = new List<OnlineCard>();
            for (int i = 0; i < 16; ++i)
                OnlineCards.Add(new OnlineCard());

            _player1DeploymentFields = new BoardField[] 
            { Fields[0,0],Fields[1,0],Fields[2,0],Fields[3,1],Fields[4,1],Fields[5,0],Fields[6,0],Fields[7,0] };
            _player1StackFields = new BoardField[]
            { Fields[0,8],Fields[1,8],Fields[2,8],Fields[3,8],Fields[4,8],Fields[5,8],Fields[6,8],Fields[7,8] };
        }


        public bool PlaceCard(ushort x, ushort y, Card card)
        {
            if (x > 7 || y > 9 || card == null)
                return false;
            var field = Fields[x, y];
            if (field.Card != null) return false;
            field.Card = card;
            card.Location = field;
            return true;
        }
        public bool PlaceCard(Vector loc, Card card)
        {
            return PlaceCard(loc.X, loc.Y, card);
        }
    }

    public class BoardField : PropChangeNotifier
    {        
        public Vector Position { get; private set; }
        Card _card;
        public Card Card
        {
            get { return _card; }
            set { SetProp(ref _card, value); }
        }

        public BoardField(ushort x, ushort y)
        {
            Position = new Vector(x,y);
        }
    }
}
