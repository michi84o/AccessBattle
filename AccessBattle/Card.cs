using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle
{
    /// <summary>
    /// Base class for cards.
    /// </summary>
    public abstract class Card : PropChangeNotifier
    {
        PlayerState _owner;
        /// <summary>Owner of this card.</summary>
        public PlayerState Owner
        {
            get { return _owner; }
            set { SetProp(ref _owner, value); }
        }

        //// TODO Not required. Board fields must know which card is on them
        ///// <summary>Board field this card is currently on.</summary>
        //public BoardField Location { get; set; }
    }

    /// <summary>
    /// Online card, either link or virus.
    /// </summary>
    public class OnlineCard : Card
    {
        bool _isFaceUp, _hasBoost;
        OnlineCardType _type;

        /// <summary>
        /// If true, the card is visible to the opponent.
        /// </summary>
        public bool IsFaceUp
        {
            get { return _isFaceUp; }
            set { SetProp(ref _isFaceUp, value); }
        }

        public OnlineCardType Type
        {
            get { return _type; }
            set { SetProp(ref _type, value); }
        }

        /// <summary>
        /// If true, the line  boost was applied to this card.
        /// </summary>
        public bool HasBoost
        {
            get { return _hasBoost; }
            set { SetProp(ref _hasBoost, value); }
        }

        /// <summary>
        /// Gets the card type of a card if it is a online card.
        /// </summary>
        /// <param name="card">Card.</param>
        /// <returns>Card type. If card is no online card, then its FaceDown.</returns>
        public static OnlineCardType GetCardType(Card card)
        {
            var c = card as OnlineCard;
            if (c == null) return OnlineCardType.FaceDown;
            return c.Type;
        }
    }

    /// <summary>
    /// Online card type.
    /// </summary>
    public enum OnlineCardType
    {
        /// <summary>Required to display cards that are face down.</summary>
        FaceDown,
        /// <summary>Link card.</summary>
        Link,
        /// <summary>Virus card.</summary>
        Virus
    }


    /// <summary>
    /// Firewall card.
    /// </summary>
    public class FirewallCard : Card
    {

    }
}
