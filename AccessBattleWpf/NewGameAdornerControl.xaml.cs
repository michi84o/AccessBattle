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

namespace AccessBattleWpf
{
    /// <summary>
    /// Interaction logic for NewGameAdornerControl.xaml
    /// </summary>
    public partial class NewGameAdornerControl : UserControl
    {
        public NewGameAdornerControl()
        {
            InitializeComponent();
        }

        public event EventHandler StartGameClicked;

        void Button_Click(object sender, RoutedEventArgs e)
        {
            var handler = StartGameClicked;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
