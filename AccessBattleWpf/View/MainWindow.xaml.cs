using AccessBattle.Wpf.ViewModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AccessBattle.Wpf.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const double X0Main = 5;
        const double Y0Main = 106;
        const double X1Main = 403;
        const double Y1Main = 502;

        double XOf(int fieldX)
        {
            double w = (X1Main - X0Main);
            return fieldX * w / 8 + X0Main;
        }
        double YOf(int fieldY)
        {
            double h = (Y1Main - Y0Main);
            return fieldY * h / 8 + Y0Main;
        }

        public MainWindow()
        {
            InitializeComponent();
            ViewModel.ShowError = ShowError;
            ////Loaded += (s, a) => { Task.Delay(3000).ContinueWith(o => ShowError()); };
            ////ViewModel.CurrentMenu = MenuType.None;
            Loaded += (s, a) =>
            {
                Task.Delay(500).ContinueWith(o =>
                {
                    //AnimateMovement(1, 1, 1, 9);

                    //        Application.Current.Dispatcher.BeginInvoke((Action)(async () =>
                    //        {
                    //            var line = new Line
                    //            {
                    //                HorizontalAlignment = HorizontalAlignment.Left,
                    //                VerticalAlignment = VerticalAlignment.Top,
                    //                StrokeThickness = 1,
                    //                Stroke = Brushes.Red,
                    //            };
                    //            Grid.SetColumnSpan(line, 12);
                    //            Grid.SetRowSpan(line, 16);
                    //            MainGrid.Children.Add(line);

                    //            for (int i = 0; i < 8; ++i)
                    //            {
                    //                line.X1 = XOf(i);
                    //                line.X2 = XOf(i+1);
                    //                line.Y1 = YOf(i);
                    //                line.Y2 = YOf(i + 1);
                    //                await Task.Delay(2500);
                    //            }

                    //            await Task.Delay(5000);

                    //            MainGrid.Children.Remove(line);
                    //        }));
                });
            };

        ViewModel.Game.PropertyChanged += Game_PropertyChanged;

        }

        private void Game_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ViewModel.Game.LastExecutedCommand)) return;

            // Code for displaying last played move

            var cmd = ViewModel.Game.LastExecutedCommand;
            if (string.IsNullOrEmpty(cmd)) return;
            if (cmd.Length < 5) return;
            var playernum = ViewModel.IsPlayerHost ? '1' : '2';
            if (cmd[0] == playernum) return;
            // Trigger animation here
            var move = cmd.Substring(2);

            if (move.StartsWith("mv"))
            {
                var command = move.Substring(3).Trim();
                var split = command.Split(new[] { ',' });
                if (split.Length != 4) return;
                Game.ReplaceLettersWithNumbers(ref split);
                int x1, x2, y1, y2;
                if (!int.TryParse(split[0], out x1) ||
                    !int.TryParse(split[1], out y1) ||
                    !int.TryParse(split[2], out x2) ||
                    !int.TryParse(split[3], out y2))
                    return;
                // Convert to zero based index:
                --x1; --x2; --y1; --y2;

                // Invert Y
                if (ViewModel.IsPlayerHost)
                {
                    y1 = 7 - y1;

                    if (y2 == 10)
                        y2 = 8;
                    else
                        y2 = 7 - y2;
                }
                else // Invert X
                {
                    x1 = 7 - x1;
                    x2 = 7 - x2;

                    if (y2 == 10) y2 = 8;
                }

                AnimateMovement(x1, y1, x2, y2);

            }

        }

        // TODO: Handle Stack
        void AnimateMovement(int x1, int y1, int x2, int y2)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(async () =>
            {
                var line = new Line
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    StrokeThickness = 5,
                    Stroke = Brushes.White,
                };
                Grid.SetColumnSpan(line, 12);
                Grid.SetRowSpan(line, 16);

                line.X1 = (XOf(x1) + XOf(x1 + 1)) / 2;
                line.Y1 = (YOf(y1) + YOf(y1 + 1)) / 2;
                line.X2 = line.X1;
                line.Y2 = line.Y1;

                double targetX = (XOf(x2) + XOf(x2 + 1)) / 2;
                double targetY = (YOf(y2) + YOf(y2 + 1)) / 2;

                if (y2 < 0 || y2 > 7)
                    targetX = (int)((X0Main + X1Main) / 2 + .5);

                double dx = targetX - line.X1;
                double dy = targetY - line.Y1;

                MainGrid.Children.Add(line);

                for (int i = 0; i <= 100; i+=5)
                {
                    line.X2 = line.X1 + dx * i / 100.0;
                    line.Y2 = line.Y1 + dy * i / 100.0;
                    await Task.Delay(50);
                }
                for (int i = 100; i >= 0; i-=5)
                {
                    line.Opacity = i / 100.0;
                    await Task.Delay(50);
                }

                await Task.Delay(2500);
                MainGrid.Children.Remove(line);
            }));
        }


        #region Error Adorner

        void ShowError(string message = "Error")
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                var layer = AdornerLayer.GetAdornerLayer(MainGrid);
                if (layer == null) return;

                var adorner = new CenteredAdorner(MainGrid);
                var notification = new ErrorNotification();
                notification.TextBlock.Text = message;
                adorner.Child = notification;

                layer.Add(adorner);

                notification.Loaded += (s, a) =>
                {
                    Task.Delay(2500).ContinueWith(o =>
                    {
                        Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            layer.Remove(adorner);
                        }));
                    });
                };
            }), null);

        }
        #endregion
    }
}
