using AccessBattle;
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

namespace AccessBattleWpf
{
    /// <summary>
    /// Interaction logic for DeploymentAdornerControl.xaml
    /// </summary>
    public partial class DeploymentControl : UserControl
    {
        public DeploymentControl()
        {
            InitializeComponent();

            var depTypeDesc = DependencyPropertyDescriptor.FromProperty(CurrentDeploymentTypeProperty, typeof(DeploymentControl));
            if (depTypeDesc != null)
            {
                depTypeDesc.AddValueChanged(this, (s, e) => UpdateBlinkState());
            }
        }

        #region Mouse Events

        bool _linkClickStarted;

        private void LinkField_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsMouseCaptured) ReleaseMouseCapture(); // Solves problems with Window not closing after click
            _linkClickStarted = false;
        }

        private void LinkField_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsMouseCaptured) ReleaseMouseCapture();
            if (_linkClickStarted)
            {
                _linkClickStarted = false;
                CurrentDeploymentType = OnlineCardType.Link;
            }
        }

        private void LinkField_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CaptureMouse())
            {
                _linkClickStarted = true;
            }
        }

        bool _virusClickStarted;

        private void VirusField_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsMouseCaptured) ReleaseMouseCapture();
            _virusClickStarted = false;
        }

        private void VirusField_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsMouseCaptured) ReleaseMouseCapture();
            if (_virusClickStarted)
            {
                _virusClickStarted = false;
                CurrentDeploymentType = OnlineCardType.Virus;
            }
        }

        private void VirusField_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CaptureMouse())
            {
                _virusClickStarted = true;
            }

        }
        #endregion

        //public static readonly DependencyProperty LinkCardsToDeployProperty =
        //        DependencyProperty.Register("LinkCardsToDeploy", typeof(int),
        //        typeof(DeploymentControl), new FrameworkPropertyMetadata(0));

        //public int LinkCardsToDeploy
        //{
        //    get { return (int)GetValue(LinkCardsToDeployProperty); }
        //    set { SetValue(LinkCardsToDeployProperty, value); }
        //}

        //public static readonly DependencyProperty VirusCardsToDeployProperty =
        //        DependencyProperty.Register("VirusCardsToDeploy", typeof(int),
        //        typeof(DeploymentControl), new FrameworkPropertyMetadata(0));

        //public int VirusCardsToDeploy
        //{
        //    get { return (int)GetValue(VirusCardsToDeployProperty); }
        //    set { SetValue(VirusCardsToDeployProperty, value); }
        //}


        public static readonly DependencyProperty CurrentDeploymentTypeProperty =
                DependencyProperty.Register("CurrentDeploymentType", typeof(OnlineCardType),
                typeof(DeploymentControl), new FrameworkPropertyMetadata(OnlineCardType.Unknown));

        public OnlineCardType CurrentDeploymentType
        {
            get { return (OnlineCardType)GetValue(CurrentDeploymentTypeProperty); }
            set { SetValue(CurrentDeploymentTypeProperty, value); }
        }

        // TODO: Possible MVVM pattern break?
        public void Initialize(Storyboard blinkStoryboard, FrameworkElement blinkStoryboardOwner)
        {
            _blinkStoryboard = blinkStoryboard;
            _blinkStoryboardOwner = blinkStoryboardOwner;
        }
        Storyboard _blinkStoryboard;
        ColorAnimation _blinkAnimation;
        bool _isAnimationInStoryboard;
        // MainWindow Reference required to synchronize storyboard
        FrameworkElement _blinkStoryboardOwner;
        void UpdateBlinkState()
        {
            // Always stop the storyboard
            // Check if it was running
            bool storyboardWasRunning;
            try
            {
                storyboardWasRunning = (_blinkStoryboard.GetCurrentState(_blinkStoryboardOwner) != ClockState.Stopped);
            }
#pragma warning disable CC0003 // Your catch maybe include some Exception
            catch { storyboardWasRunning = false; }
#pragma warning restore CC0003 // Your catch maybe include some Exception
            if (storyboardWasRunning) _blinkStoryboard.Stop(_blinkStoryboardOwner);

            // If our animation was active, remove it
            if (_isAnimationInStoryboard && _blinkAnimation != null)
            {
                _blinkStoryboard.Children.Remove(_blinkAnimation);
                _isAnimationInStoryboard = false;
            }

            if (CurrentDeploymentType != OnlineCardType.Unknown)
            {
                // Initialize
                _blinkAnimation = new ColorAnimation(Colors.Black, Colors.DarkGray, TimeSpan.FromSeconds(1))
                {
                    BeginTime = TimeSpan.FromSeconds(0),
                    AutoReverse = true,
                };
                if (CurrentDeploymentType == OnlineCardType.Link)
                    Storyboard.SetTarget(_blinkAnimation, LinkText);
                else
                    Storyboard.SetTarget(_blinkAnimation, VirusText);
                Storyboard.SetTargetProperty(_blinkAnimation, new PropertyPath("Foreground.Color"));
                _blinkStoryboard.Children.Add(_blinkAnimation);
                _isAnimationInStoryboard = true;
                _blinkStoryboard.Begin(_blinkStoryboardOwner, true);
            }
            else
            {
                // if storyboard was running, restart it
                if (storyboardWasRunning && _blinkStoryboard.Children.Count > 0)
                    _blinkStoryboard.Begin(_blinkStoryboardOwner, true);
            }

        }        
    }
}
