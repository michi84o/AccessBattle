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
            StopFlashing();
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

        #region Multi-Overlay Opacity Flash

        Storyboard _multiOverlayFlashingStoryboard;

        public void StopMultiOverlayFlashing()
        {
            if (_multiOverlayFlashingStoryboard != null)
            {
                _multiOverlayFlashingStoryboard.Stop(this);
                _multiOverlayFlashingStoryboard = null;
            }
        }

        public void StartMultiOverlayFlashing()
        {
            StopMultiOverlayFlashing();
            var animation1 = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(1))
            {
                BeginTime= TimeSpan.FromSeconds(0)
            };
            var animation2 = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(1))
            {
                BeginTime = TimeSpan.FromSeconds(2)
            };
            _multiOverlayFlashingStoryboard = new Storyboard
            {
                Duration = TimeSpan.FromSeconds(6),
                RepeatBehavior = RepeatBehavior.Forever
            };
            Storyboard.SetTarget(animation1, this);
            Storyboard.SetTargetProperty(animation1, new PropertyPath(nameof(MultiOverlayOpacity)));
            Storyboard.SetTarget(animation2, this);
            Storyboard.SetTargetProperty(animation2, new PropertyPath(nameof(MultiOverlayOpacity)));
            _multiOverlayFlashingStoryboard.Children.Add(animation1);
            _multiOverlayFlashingStoryboard.Children.Add(animation2);
            _multiOverlayFlashingStoryboard.Begin(this, true);
        }

        public double MultiOverlayOpacity
        {
            get { return (double)GetValue(MultiOverlayOpacityProperty); }
            set { SetValue(MultiOverlayOpacityProperty, value); }
        }

        public static readonly DependencyProperty MultiOverlayOpacityProperty = DependencyProperty.Register(
            nameof(MultiOverlayOpacity), typeof(double), typeof(UiGlobals), new PropertyMetadata(0.0));

        #endregion
    }
}
