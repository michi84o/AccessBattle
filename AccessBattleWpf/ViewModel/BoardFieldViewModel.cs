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
        bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProp(ref _isSelected, value); }
        }

        BoardField _field;
        public BoardField Field => _field;

        bool _isVisibleToOpponent;
        public bool IsVisibleToOpponent
        {
            get { return _isVisibleToOpponent; }
            set { SetProp(ref _isVisibleToOpponent, value); }
        }

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

        public bool HasCard => _field?.Card != null;
        public bool IsDeploymentField(int playerIndex)
        {
            if (_field == null) return false;
            int y1 = playerIndex == 1 ? 0 : 7;
            int y2 = playerIndex == 1 ? 1 : 6;

            return (Field.Y == y1 && Field.X != 3 && Field.X != 4 ||
                    Field.Y == y2 && (Field.X == 3 || Field.X == 4));
        }

        bool _isHighlighted;
        public bool IsHighlighted
        {
            get { return _isHighlighted; }
            set
            {
                if (SetProp(ref _isHighlighted, value))
                {
                    if (value && !UiGlobals.Instance.IsFlashing)
                        UiGlobals.Instance.StartFlashing();
                }
            }
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

                OnPropertyChanged(nameof(Field));
                Field_PropertyChanged(_field, new PropertyChangedEventArgs(""));
            }
        }

        private void Field_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var s = sender as BoardField;
            if (s == null || s != _field) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(s.Card))
                {
                    OnPropertyChanged(nameof(HasCard));

                    if (s.Card == null)
                    {
                        VisualState = BoardFieldVisualState.Empty;
                        CardVisualState = BoardFieldCardVisualState.Empty;
                        IsVisibleToOpponent = false;
                    }
                    else if (s.Card.Owner != null)
                    {
                        if (s.Card is OnlineCard)
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

                            IsVisibleToOpponent = card.IsFaceUp;
                        }
                        else if (s.Card is FirewallCard)
                        {
                            VisualState = BoardFieldVisualState.Firewall;
                        }
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
