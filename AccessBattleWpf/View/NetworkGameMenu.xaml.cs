using AccessBattle.Wpf.ViewModel;
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
