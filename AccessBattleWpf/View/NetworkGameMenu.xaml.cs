using AccessBattle.Wpf.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AccessBattle.Wpf.View
{
    /// <summary>
    /// Interaction logic for NetworkGameMenu.xaml
    /// </summary>
    public partial class NetworkGameMenu : UserControl
    {
        public NetworkGameMenu()
        {
            InitializeComponent();

            DataContextChanged += NetworkGameMenu_DataContextChanged;

            NetworkGameMenu_DataContextChanged(null, new DependencyPropertyChangedEventArgs());
        }

        private void NetworkGameMenu_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var ctx = DataContext as NetworkGameMenuViewModel;
            if (ctx == null) return;
            ctx.PropertyChanged += Ctx_PropertyChanged;

            Ctx_PropertyChanged(null, new System.ComponentModel.PropertyChangedEventArgs(nameof(NetworkGameMenuViewModel.AllowsRegistration)));
        }

        private void Ctx_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (e.PropertyName == nameof(NetworkGameMenuViewModel.AllowsRegistration))
                {
                    var ctx = sender as NetworkGameMenuViewModel;
                    if (ctx?.AllowsRegistration != true)
                    {
                        CreateAccountButton.Visibility = Visibility.Collapsed;
                        Grid.SetRowSpan(LoginButton, 2);
                    }
                    else
                    {
                        CreateAccountButton.Visibility = Visibility.Visible;
                        Grid.SetRowSpan(LoginButton, 1);
                    }
                }
            }));
        }

        void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var dc = DataContext as NetworkGameMenuViewModel;
            var box = sender as PasswordBox;
            if (dc == null || box ==null) return;
            dc.LoginPassword = box.SecurePassword;
        }

        void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            var dc = DataContext as NetworkGameMenuViewModel;
            if (dc == null) return;
            if (e.Key == Key.Enter)
            {
                if (dc.LoginCommand.CanExecute(null))
                    dc.LoginCommand.Execute(null);
            }
        }
    }
}
