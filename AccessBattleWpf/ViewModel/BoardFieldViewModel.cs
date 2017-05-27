using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Wpf.ViewModel
{ 
    public class BoardFieldViewModel : PropChangeNotifier
    {
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
    }
}
