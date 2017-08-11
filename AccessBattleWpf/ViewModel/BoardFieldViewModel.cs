using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AccessBattle.Wpf.ViewModel
{
    public class BoardFieldViewModel : PropChangeNotifier
    {
        BoardField _field;
        public BoardField Field => _field;

        BoardFieldVisualState _visualState = BoardFieldVisualState.Empty;
        public BoardFieldVisualState VisualState
        {
            get
            {
                if (_visualState == BoardFieldVisualState.Empty)
                    return _defaultVisualState;
                return _visualState;
            }
            set { SetProp(ref _visualState, value); }
        }

        BoardFieldVisualState _defaultVisualState = BoardFieldVisualState.Empty;
        public BoardFieldVisualState DefaultVisualState
        {
            get { return _defaultVisualState; }
            set { SetProp(ref _defaultVisualState, value); }
        }

        BoardFieldCardVisualState _cardVisualState = BoardFieldCardVisualState.Empty;
        public BoardFieldCardVisualState CardVisualState
        {
            get { return _cardVisualState; }
            set { SetProp(ref _cardVisualState, value); }
        }

        public void RegisterBoardField(BoardField field)
        {
            if (field == _field) return;
            if (_field != null)
            {
                WeakEventManager<BoardField, PropertyChangedEventArgs>.RemoveHandler(
                    _field, nameof(_field.PropertyChanged), Field_PropertyChanged);
            }
            _field = field;
            if (_field != null)
            {
                WeakEventManager<BoardField, PropertyChangedEventArgs>.AddHandler(
                _field, nameof(_field.PropertyChanged), Field_PropertyChanged);
            }
        }

        private void Field_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var s = sender as BoardField;
            if (s == null || s != _field) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.PropertyName == nameof(s.Card))
                {
                    if (s.Card == null)
                    {
                        VisualState = BoardFieldVisualState.Empty;
                        CardVisualState = BoardFieldCardVisualState.Empty;
                    }
                    else if (s.Card is OnlineCard && s.Card.Owner != null)
                    {
                        var card = s.Card as OnlineCard;
                        switch (card.Type)
                        {
                            case OnlineCardType.Link:
                                VisualState = BoardFieldVisualState.Link;
                                break;
                            case OnlineCardType.Virus:
                                VisualState = BoardFieldVisualState.Virus;
                                break;
                            default:
                                VisualState = BoardFieldVisualState.Flipped;
                                break;
                        }

                        if (card.HasBoost)
                            VisualState |= BoardFieldVisualState.LineBoost;

                        var num = s.Card.Owner.PlayerNumber;
                        if (num == 1)
                        {
                            CardVisualState = BoardFieldCardVisualState.Blue;
                        }
                        else if (num == 2)
                        {
                            CardVisualState = BoardFieldCardVisualState.Orange;
                        }
                    }
                }
            });

        }
    }
}
