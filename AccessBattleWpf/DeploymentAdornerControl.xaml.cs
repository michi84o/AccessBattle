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
    public partial class DeploymentAdornerControl : UserControl
    {
        Storyboard _linkStoryBoard;
        Storyboard _virusStoryBoard;

        static void AddBlinkAnimationForTextBlock(TextBlock block, Storyboard board, Color from, Color to)
        {
            var animation1 = new ColorAnimation(from, to, TimeSpan.FromSeconds(1))
            {
                BeginTime = TimeSpan.FromSeconds(0)
            };
            Storyboard.SetTarget(animation1, block);
            Storyboard.SetTargetProperty(animation1, new PropertyPath("Foreground.Color"));
            board.Children.Add(animation1);
            var animation2 = new ColorAnimation(to, from, TimeSpan.FromSeconds(1))
            {
                BeginTime = TimeSpan.FromSeconds(1)
            };
            Storyboard.SetTarget(animation2, block);
            Storyboard.SetTargetProperty(animation2, new PropertyPath("Foreground.Color"));
            board.Children.Add(animation2);
        }

        public DeploymentAdornerControl()
        {
            InitializeComponent();

            _linkStoryBoard = new Storyboard
            {
                Duration = TimeSpan.FromSeconds(2),
                RepeatBehavior = RepeatBehavior.Forever
            };
            _virusStoryBoard = new Storyboard
            {
                Duration = TimeSpan.FromSeconds(2),
                RepeatBehavior = RepeatBehavior.Forever
            };

            var from = Color.FromArgb(255, 10, 10, 10);
            var to = Colors.DarkGray;
            AddBlinkAnimationForTextBlock(LinkText, _linkStoryBoard, from, to);
            AddBlinkAnimationForTextBlock(LinkNumber, _linkStoryBoard, from, to);
            AddBlinkAnimationForTextBlock(VirusText, _virusStoryBoard, from, to);
            AddBlinkAnimationForTextBlock(VirusNumber, _virusStoryBoard, from, to);

            var blinkDesc = DependencyPropertyDescriptor.FromProperty(IsLinkBlinkingProperty, typeof(DeploymentAdornerControl));
            if (blinkDesc != null)
            {
                // TODO: People keep telling this could be a memory leak
                blinkDesc.AddValueChanged(this, (s, e) =>
                {
                    if (IsLinkBlinking != _isLinkBliking)
                    {
                        _isLinkBliking = IsLinkBlinking;
                        BlinkLink(_isLinkBliking);
                    }
                });
            }
            blinkDesc = DependencyPropertyDescriptor.FromProperty(IsVirusBlinkingProperty, typeof(DeploymentAdornerControl));
            if (blinkDesc != null)
            {
                // TODO: People keep telling this could be a memory leak
                blinkDesc.AddValueChanged(this, (s, e) =>
                {
                    if (IsVirusBlinking != _isVirusBliking)
                    {
                        _isVirusBliking = IsVirusBlinking;
                        BlinkVirus(_isVirusBliking);
                    }
                });
            }

        }

        void BlinkLink(bool on = true)
        {
            if (on)
                _linkStoryBoard.Begin(this, true);
            else
                _linkStoryBoard.Stop(this);
            _linkStoryBoard.Seek(TimeSpan.Zero);
        }

        void BlinkVirus(bool on = true)
        {
            if (on)
                _virusStoryBoard.Begin(this, true);
            else
                _virusStoryBoard.Stop(this);
            _virusStoryBoard.Seek(TimeSpan.Zero);
        }

        public static readonly DependencyProperty IsLinkBlinkingProperty =
                DependencyProperty.Register("IsLinkBlinking", typeof(bool),
                typeof(DeploymentAdornerControl), new FrameworkPropertyMetadata(false));

        bool _isLinkBliking;
        public bool IsLinkBlinking
        {
            get { return (bool)GetValue(IsLinkBlinkingProperty); }
            set { SetValue(IsLinkBlinkingProperty, value); }
        }

        public static readonly DependencyProperty IsVirusBlinkingProperty =
                DependencyProperty.Register("IsVirusBlinking", typeof(bool),
                typeof(DeploymentAdornerControl), new FrameworkPropertyMetadata(false));

        bool _isVirusBliking;
        public bool IsVirusBlinking
        {
            get { return (bool)GetValue(IsVirusBlinkingProperty); }
            set { SetValue(IsVirusBlinkingProperty, value); }
        }
    }
}
