using AccessBattle.Wpf.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
