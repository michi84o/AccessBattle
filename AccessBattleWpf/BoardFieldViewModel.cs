using AccessBattle;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AccessBattleWpf
{
    public class BoardFieldViewModel : PropChangeNotifier
    {
        private BoardField _field;
        public BoardField Field { get { return _field; } }

        public event EventHandler CardChanged;
        Card _lastCard;

        public BoardFieldViewModel(BoardField field)
        {
            _field = field;
            WeakEventManager<BoardField, PropertyChangedEventArgs>.AddHandler(field, "PropertyChanged", Field_PropertyChanged);
        }

        void Field_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Card")
            {
                if (_lastCard != null)
                {
                    WeakEventManager<Card, PropertyChangedEventArgs>.RemoveHandler(_lastCard, "PropertyChanged", Card_PropertyChanged);
                    _lastCard = null;
                }

                OnPropertyChanged("Card");

                _lastCard = _field.Card;
                if (_lastCard != null)
                {
                    WeakEventManager<Card, PropertyChangedEventArgs>.AddHandler(_lastCard, "PropertyChanged", Card_PropertyChanged);
                }
            }
        }

        void Card_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender != _lastCard) return;
            var handler = CardChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public AccessBattle.Vector Position
        {
            get { return _field.Position; }
        }

        public Card Card
        {
            get { return _field.Card; }
        }

        public BoardFieldType Type
        {
            get { return _field.Type; }
        }
    }
}
