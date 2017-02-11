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
    /// Interaction logic for SwitchCardsControl.xaml
    /// </summary>
    public partial class SwitchCardsControl : UserControl
    {
        public SwitchCardsControl()
        {
            InitializeComponent();
        }


        #region Mouse Events
        // TODO: Style so that a button can be used

        public event EventHandler<EventArgs> Yes;
        public event EventHandler<EventArgs> No;

        bool _YesClickStarted;

        private void YesField_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsMouseCaptured) ReleaseMouseCapture(); // Solves problems with Window not closing after click
            _YesClickStarted = false;
            YesField.Background = Brushes.Transparent;
        }

        private void YesField_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsMouseCaptured) ReleaseMouseCapture();
            if (_YesClickStarted)
            {
                _YesClickStarted = false;
                var handler = Yes;
                if (handler != null)
                    handler(this, EventArgs.Empty);
            }
        }

        private void YesField_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CaptureMouse())
            {
                _YesClickStarted = true;
            }
        }

        bool _NoClickStarted;

        private void NoField_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsMouseCaptured) ReleaseMouseCapture();
            _NoClickStarted = false;
            NoField.Background = Brushes.Transparent;
        }

        private void NoField_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsMouseCaptured) ReleaseMouseCapture();
            if (_NoClickStarted)
            {
                _NoClickStarted = false;
                var handler = No;
                if (handler != null)
                    handler(this, EventArgs.Empty);
            }
        }

        private void NoField_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CaptureMouse())
            {
                _NoClickStarted = true;
            }

        }

        Brush _darkBrush = new SolidColorBrush(Color.FromArgb(0xff, 0x60, 0x60, 0x60));
        private void YesField_MouseEnter(object sender, MouseEventArgs e)
        {
            YesField.Background = _darkBrush;
        }

        private void NoField_MouseEnter(object sender, MouseEventArgs e)
        {
            NoField.Background = _darkBrush;
        }

        #endregion

    }
}
