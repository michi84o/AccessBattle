using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccessBattle
{
    public abstract class Card : PropChangeNotifier
    {
        Player _owner;
        public Player Owner
        {
            get { return _owner; }
            set { SetProp(ref _owner, value); }
        }
        public BoardField Location { get; set; }
    }

    public class OnlineCard : Card
    {
        bool _isFaceUp, _hasBoost;
        OnlineCardType _type;

        public bool IsFaceUp
        {
            get { return _isFaceUp; }
            set { SetProp(ref _isFaceUp, value); }
        }
        public bool HasBoost
        {
            get { return _hasBoost; }
            set { SetProp(ref _hasBoost, value); }
        }

        public OnlineCardType Type
        {
            get { return _type; }
            set { SetProp(ref _type, value); }
        }

        public OnlineCard(OnlineCardType type = OnlineCardType.Unknown)
        {
            _type = type;
        }

        public static OnlineCardType GetCardType(Card card)
        {
            var c = card as OnlineCard;
            if (c == null) return OnlineCardType.Unknown;
            return c.Type;
        }
    }

    public enum OnlineCardType
    {
        Unknown,
        Link,
        Virus
    }

    public class FirewallCard : Card
    {

    }
}
