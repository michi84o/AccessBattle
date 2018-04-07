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

        public abstract Sync GetSync();

        public class Sync
        {
            public int Owner;
            public bool IsFaceUp;
            public bool HasBoost;
            public OnlineCardType Type;
            public bool IsFirewall;
        }

        //BoardField _location;
        ///// <summary>
        ///// Adds a reference to a board field for finding the card faster.
        ///// The getter returns null when the set value contains another card.
        ///// </summary>
        //public BoardField Location
        //{
        //    get
        //    {
        //        if (_location?.Card != this) return null;
        //        return _location;
        //    }
        //    set { SetProp(ref _location, value); }
        //}
    }

    /// <summary>
    /// Online card, either link or virus.
    /// </summary>
    public class OnlineCard : Card
    {
        bool _isFaceUp, _hasBoost;
        OnlineCardType _type;

        private static bool? _aiDebug;

        /// <summary>
        /// Constructor
        /// </summary>
        public OnlineCard()
        {
            if (_aiDebug == null)
            {
                var args = Environment.GetCommandLineArgs();
                _aiDebug = args.Contains("-aidebug");
            }
        }

        /// <summary>
        /// If true, the card is visible to the opponent.
        /// </summary>
        public bool IsFaceUp
        {
            get { return (_aiDebug == true) || _isFaceUp; }
            set { SetProp(ref _isFaceUp, value); }
        }

        /// <summary>
        /// Type of online card.
        /// </summary>
        /// <remarks>
        /// There is no derived class for the types
        /// because the opponent should not be able to know
        /// the type of the card if it is face down.
        /// This is easier to implement.</remarks>
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
            if (c == null) return OnlineCardType.Unknown;
            return c.Type;
        }

        public override Sync GetSync()
        {
            return new Sync
            {
                Owner = Owner.PlayerNumber,
                HasBoost = HasBoost,
                IsFaceUp = IsFaceUp,
                IsFirewall = false,
                Type = Type
            };
        }
    }

    /// <summary>
    /// Online card type.
    /// </summary>
    public enum OnlineCardType
    {
        /// <summary>Required to display cards that are face down.</summary>
        Unknown,
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
        public override Sync GetSync()
        {
            return new Sync
            {
                Owner = Owner.PlayerNumber,
                HasBoost = false,
                IsFaceUp = true,
                IsFirewall = true,
                Type = OnlineCardType.Unknown
            };
        }
    }
}
