using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            UiGlobals.Instance.StartFlashing();
            Task.Delay(5000).ContinueWith(o => 
            {
                Application.Current.Dispatcher.Invoke(() => { UiGlobals.Instance.StopFlashing(); });
            });
        }
    }

    public class UiGlobals : UserControl, INotifyPropertyChanged
    {
        private UiGlobals() { }
        static UiGlobals _instance;
        public static UiGlobals Instance
        {
            get
            {
                return _instance ?? (_instance = new UiGlobals());
            }
        }

        double _flashOpacity = 0;
        public double FlashOpacity
        {
            get { return (double)GetValue(FlashOpacityProperty); }
            set { SetValue(FlashOpacityProperty, value); }
        }

        public void StartFlashing()
        {
            var animation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(1))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            BeginAnimation(FlashOpacityProperty, animation);
        }

        public void StopFlashing()
        {
            BeginAnimation(FlashOpacityProperty, null);
            FlashOpacity = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty FlashOpacityProperty = DependencyProperty.Register(
            "FlashOpacity", typeof(double), typeof(UiGlobals), new PropertyMetadata(0.0));
    }

}
