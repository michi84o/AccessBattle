using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace AccessBattle.Wpf
{
    public class UiGlobals : UserControl
    {
        #region Singleton
        static readonly object _instanceLock = new object();
        UiGlobals() { }
        static UiGlobals _instance;
        public static UiGlobals Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                            _instance = new UiGlobals();
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Field flashing

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
            FlashOpacity = 0.0;
        }

        public static readonly DependencyProperty FlashOpacityProperty = DependencyProperty.Register(
            nameof(FlashOpacity), typeof(double), typeof(UiGlobals), new PropertyMetadata(0.0));

        #endregion
    }
}
