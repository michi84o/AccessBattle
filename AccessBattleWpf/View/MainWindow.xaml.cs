using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

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
            //(new TextBox).Valoda

            //Loaded += (s, a) => { Task.Delay(3000).ContinueWith(o => ShowError()); };
            //ViewModel.CurrentMenu = MenuType.None;
            //Loaded += (s, a) => {
            //    Task.Delay(3000).ContinueWith(o =>
            //    {
            //        for (int i = 0; i < 80; ++i)
            //        {
            //            Application.Current.Dispatcher.Invoke(() =>
            //            {
            //                ViewModel.BoardFieldList[i].IsVisibleToOpponent = true;
            //                if (i > 0)
            //                    ViewModel.BoardFieldList[i - 1].IsVisibleToOpponent = false;
            //            });
            //            Task.Delay(250).Wait();
            //        }
            //    });
            //};
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
