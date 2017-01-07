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
        // TODO: Style so that a button can be used

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
        public void Initialize(StoryboardAsyncWrapper blinkStoryboard)
        {
            _blinkStoryboard = blinkStoryboard;
        }
        StoryboardAsyncWrapper _blinkStoryboard;
        ColorAnimation _blinkAnimation;
        // MainWindow Reference required to synchronize storyboard
        void UpdateBlinkState()
        {
            _blinkStoryboard.RemoveAnimation(_blinkAnimation);

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

                _blinkStoryboard.AddAnimation(_blinkAnimation);
            }
        }        
    }
}
