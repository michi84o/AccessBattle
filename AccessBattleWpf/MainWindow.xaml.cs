using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace AccessBattleWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CenteredAdornerContainer _newGameAdorner;
        NewGameAdornerControl _newGameAdornerControl;
        AdornerLayer _MainPanelAdornerLayer;

        CardField[,] _mainFields;

        public MainWindow()
        {
            InitializeComponent();
            ViewModel.Game.PropertyChanged += Game_PropertyChanged;
            Loaded += MainWindow_Loaded;
            _mainFields = new CardField[,] // X,Y
            {
                { A1, A2, A3, A4, A5, A6, A7, A8 }, // 0,0=A1 / 0,7=A8
                { B1, B2, B3, B4, B5, B6, B7, B8 }, // 1,0=B1 / 1,7=B8
                { C1, C2, C3, C4, C5, C6, C7, C8 }, // ...
                { D1, D2, D3, D4, D5, D6, D7, D8 },
                { E1, E2, E3, E4, E5, E6, E7, E8 },
                { F1, F2, F3, F4, F5, F6, F7, F8 },
                { G1, G2, G3, G4, G5, G6, G7, G8 },
                { H1, H2, H3, H4, H5, H6, H7, H8 },
            };
            for (int x = 0; x<8;++x)
            {
                for (int y = 0; y < 8; ++y)
                    _mainFields[x, y].SetBoardField(ViewModel.Board.Fields[x, y]);
            }
        }

        private void Game_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentPhase")
            {
                switch (ViewModel.Game.CurrentPhase)
                {
                    case AccessBattle.GamePhase.Init:
                        if (_MainPanelAdornerLayer == null) SetupNewGameAdorner();
                        _MainPanelAdornerLayer.Add(_newGameAdorner);
                        _newGameAdornerControl.PlayerName = ViewModel.Game.Player1.Name;
                        break;
                    case AccessBattle.GamePhase.Deployment:
                        break;
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.Game.CurrentPhase = AccessBattle.GamePhase.Init;
        }

        // Called once
        void SetupNewGameAdorner()
        {
            if (_MainPanelAdornerLayer == null)
                _MainPanelAdornerLayer = AdornerLayer.GetAdornerLayer(MainGrid);
            if (_MainPanelAdornerLayer == null)
            {
                MessageBox.Show("Error starting new game. Adorner layer of MainGrid is null!");
                return;
            }
            if (_newGameAdornerControl == null)
                _newGameAdornerControl = new NewGameAdornerControl();
            if (_newGameAdorner == null)
            {
                _newGameAdorner = new CenteredAdornerContainer(MainGrid)
                {
                    Child = _newGameAdornerControl
                };
                _newGameAdornerControl.StartGameClicked += GameAdornerControl_StartGameClicked;
            }
        }

        private void GameAdornerControl_StartGameClicked(object sender, EventArgs e)
        {
            _MainPanelAdornerLayer.Remove(_newGameAdorner);
            ViewModel.Game.CurrentPhase = AccessBattle.GamePhase.Deployment;
        }

        void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateBoardLayout();
        }

        void Window_StateChanged(object sender, EventArgs e)
        {
            // Fix Maximize Window Glitch
            // Width was not updated correctly and UpdateBoardLayout() got not final call
            // Several Resizes are required to fix
            if (WindowState == WindowState.Normal)
            {
                Application.Current.Dispatcher.BeginInvoke((Action)delegate () { Width += 50; }, null);
                Application.Current.Dispatcher.BeginInvoke((Action)delegate () { Width -= 50; }, null);
                Application.Current.Dispatcher.BeginInvoke((Action)delegate () { Width += 50; }, null);
                Application.Current.Dispatcher.BeginInvoke((Action)delegate () { Width -= 50; }, null);
                Application.Current.Dispatcher.BeginInvoke((Action)delegate () { UpdateBoardLayout(); }, null);
            }
        }

        void UpdateBoardLayout()
        {
            var width = MainGrid.ActualWidth;
            var height = MainGrid.ActualHeight;
            Title = "" + width + "x" + height;
            if (width < 1 || height < 1
                /*|| width == double.NaN || height == double.NaN*/)
                return; // TODO
            var optimalHeight = width * 12 / 8;
            var optimalWidth = height * 8 / 12;
            var zero = new Point();
            if (optimalHeight > height)
            {
                zero.X = (width - optimalWidth) / 2;
                zero.Y = 0;
            }
            else
            {
                zero.X = 0;
                zero.Y = (height - optimalHeight) / 2;
            }
            ColumnEdgeLeft.Width = new GridLength(zero.X);
            ColumnEdgeRight.Width = new GridLength(zero.X);
            RowEdgeUpper.Height = new GridLength(zero.Y);
            RowEdgeLower.Height = new GridLength(zero.Y);

            D8.Height = Row1.ActualHeight * 1.2;            
            E8.Height = Row1.ActualHeight * 1.2;
            D1.Height = Row1.ActualHeight * 1.2;
            E1.Height = Row1.ActualHeight * 1.2;
            ViewBoxServerP1.Margin = new Thickness(0,Row1.ActualHeight*0.2 + 1,0,1);
            ViewBoxServerP2.Margin = new Thickness(0, 1, 0, Row1.ActualHeight * 0.2 + 1);
        }
    }
}
