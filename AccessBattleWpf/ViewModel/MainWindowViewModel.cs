using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AccessBattle.Wpf.ViewModel
{
    public class MainWindowViewModel : PropChangeNotifier
    {

        #region Board Field Visual States

        BoardFieldViewModel[,] _boardFields = new BoardFieldViewModel[8, 11];

        /// <summary>
        /// This is a one-dimensional list that can be used in XAML code.
        /// It maps the internal two-dimensinal list and contains the items row-wise,
        /// starting from row (y=)0.
        /// item 0 is field [0,0], item 1 is field [1,0], item 8 is field [1,0].
        /// The length of this list is 88.
        /// </summary>
        public List<BoardFieldViewModel> BoardFieldList { get; private set; }

        #endregion

        public bool IsPlayerHost
        {
            get; set;
        }

        public MainWindowViewModel()
        {
            IsPlayerHost = true;

            BoardFieldList = new List<BoardFieldViewModel>();
            for (int y = 0; y < 11; ++y)
                for (int x = 0; x < 8; ++x)
                {
                    _boardFields[x, y] = new BoardFieldViewModel();
                    BoardFieldList.Add(_boardFields[x, y]);
                }

            _boardFields[3, 0].DefaultVisualState = BoardFieldVisualState.Exit;
            _boardFields[4, 0].DefaultVisualState = BoardFieldVisualState.Exit;

            _boardFields[3, 7].DefaultVisualState = BoardFieldVisualState.Exit;
            _boardFields[4, 7].DefaultVisualState = BoardFieldVisualState.Exit;
        }

    }
}
