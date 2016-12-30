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
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateBoardLayout();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            // Fix Maximize Window Glitch
            if (WindowState == WindowState.Normal)
                (new Task(() => 
                {
                    Thread.Sleep(100);
                    App.Current.Dispatcher.Invoke((Action)delegate () { Width += 1; }, null);
                    App.Current.Dispatcher.Invoke((Action)delegate () { Width -= 1; }, null);
                })).Start();
        }

        void UpdateBoardLayout()
        {
            double width = MainGrid.ActualWidth;
            double height = MainGrid.ActualHeight;
            Title = "" + width + "x" + height;
            if (width < 1 || height < 1
                /*|| width == double.NaN || height == double.NaN*/)
                return; // TODO
            double optimalHeight = width * 12 / 8;
            double optimalWidth = height * 8 / 12;
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
