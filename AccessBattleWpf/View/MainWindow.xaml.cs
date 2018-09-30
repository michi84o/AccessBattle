using AccessBattle.Wpf.ViewModel;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AccessBattle.Wpf.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ViewModel.ShowError = ShowError;
            ////Loaded += (s, a) => { Task.Delay(3000).ContinueWith(o => ShowError()); };
            ////ViewModel.CurrentMenu = MenuType.None;
            //Loaded += (s, a) =>
            //{
            //    Task.Delay(3000).ContinueWith(o =>
            //    {

            //    });
            //};

            ViewModel.Game.PropertyChanged += Game_PropertyChanged;

        }

        private void Game_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ViewModel.Game.LastExecutedCommand)) return;

            var cmd = ViewModel.Game.LastExecutedCommand;
            if (string.IsNullOrEmpty(cmd)) return;
            if (cmd.Length < 5) return;
            var playernum = ViewModel.IsPlayerHost ? '1' : '2';
            if (cmd[0] == playernum) return;
            // Trigger animation here
            var move = cmd.Substring(2);

            // TODO - WIP
            //Application.Current.Dispatcher.BeginInvoke((Action)(async () =>
            //{
            //    var tb = new TextBlock { Text = move, FontSize = 24, Visibility = Visibility.Visible };
            //    Grid.SetColumn(tb, 0);
            //    Grid.SetRow(tb, 0);
            //    Grid.SetColumn(tb, 3);
            //    Grid.SetRow(tb, 3);
            //    Grid.SetColumnSpan(tb, 12);
            //    Grid.SetRowSpan(tb, 8);
            //    MainGrid.Children.Add(tb);
            //    await Task.Delay(5000);
            //    MainGrid.Children.Remove(tb);
            //}));
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
