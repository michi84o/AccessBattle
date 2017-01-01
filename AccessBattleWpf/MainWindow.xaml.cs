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

namespace AccessBattleWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CenteredAdornerContainer _newGameAdorner;

        public MainWindow()
        {
            InitializeComponent();
            ViewModel.StartingNewGame += ViewModel_StartingNewGame;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.StartNewGame();
        }

        private void ViewModel_StartingNewGame(object sender, EventArgs e)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(MainGrid);
            if (adornerLayer == null)
            {
                MessageBox.Show("Error starting new game. Adorner layer is null!");
                return;
            }
            if (_newGameAdorner == null)
            {
                _newGameAdorner = new CenteredAdornerContainer(MainGrid)
                {
                    Child = new NewGameAdornerControl()
                };
            }
            adornerLayer.Add(_newGameAdorner);
            InvalidateVisual();
        }

        void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateBoardLayout();
        }

        void Window_StateChanged(object sender, EventArgs e)
        {
            // Fix Maximize Window Glitch
            if (WindowState == WindowState.Normal)
#pragma warning disable CC0022 // Should dispose object
                (new Task(() =>
                {
                    Thread.Sleep(100);
                    Application.Current.Dispatcher.Invoke((Action)delegate () { Width += 1; }, null);
                    Application.Current.Dispatcher.Invoke((Action)delegate () { Width -= 1; }, null);
                })).Start();
#pragma warning restore CC0022 // Should dispose object
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

            Exit1P1.Height = Row1.ActualHeight * 1.2;            
            Exit2P1.Height = Row1.ActualHeight * 1.2;
            Exit1P2.Height = Row1.ActualHeight * 1.2;
            Exit2P2.Height = Row1.ActualHeight * 1.2;
            ViewBoxServerP2.Margin = new Thickness(0,Row1.ActualHeight*0.2 + 1,0,1);
            ViewBoxServerP1.Margin = new Thickness(0, 1, 0, Row1.ActualHeight * 0.2 + 1);
        }
    }
}
