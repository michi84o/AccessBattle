using AccessBattle;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for BoardFieldControl.xaml
    /// </summary>
    public partial class BoardFieldView : Border
    {
        BoardFieldViewModel _field;
        Color _primaryBackground;
        Color _blinkTargetColor;

        //Storyboard _lineBoostStoryBoard = new Storyboard()
        //{
        //    Duration = TimeSpan.FromSeconds(6),
        //    RepeatBehavior = RepeatBehavior.Forever
        //};
        bool _lineBoostAnimationStarted;
        List<Timeline> _lineBoostAnimations = new List<Timeline>();

        public SolidColorBrush DefaultBackground { get; set; }

        StoryboardAsyncWrapper _blinkStoryboard;
        StoryboardAsyncWrapper _lineBoostStoryboard;
        ColorAnimation _blinkAnimation;
        bool _isAnimationInStoryboard;
        bool _isAnimationInitialized;

        // Todo: Resource
        //static SolidColorBrush EmptyMainBrush = new SolidColorBrush();

        bool _flipped;
        public bool Flipped
        {
            get { return _flipped; }
            set
            {
                _flipped = value;
                if (_flipped)
                {
                    ExitTransform.ScaleX = -1;
                    ExitTransform.ScaleY = -1;
                }
                else
                {
                    ExitTransform.ScaleX = 1;
                    ExitTransform.ScaleY = 1;
                }
            }
        }

        BoardFieldViewDisplayState _displayState;
        public BoardFieldViewDisplayState DisplayState
        {
            get { return _displayState; }
            set
            {
                // Reset blinking and force rebuild of storyboard
                IsBlinking = false;
                _isAnimationInitialized = false;

                if (_displayState == value) return;
                _displayState = value;

                UpdateDisplayState();
            }
        }

        void UpdateDisplayState()
        {
            // TODO: Databinding
            LinkGrid.Visibility = Visibility.Hidden;
            VirusGrid.Visibility = Visibility.Collapsed;
            VirusCheckGrid.Visibility = Visibility.Collapsed;
            FirewallGrid.Visibility = Visibility.Collapsed;
            NotFound404Grid.Visibility = Visibility.Collapsed;
            //LineBoostGrid.Visibility = Visibility.Hidden; // Set below
            ExitBox.Visibility = Visibility.Collapsed;
            VirusPath.Stroke = Brushes.DarkGray;
            VirusPath.Fill = Brushes.DarkGray;
            LinkPath.Stroke = Brushes.DarkGray;
            LinkPath.Fill = Brushes.DarkGray;
            VirusText.Foreground = Brushes.DarkGray;
            LinkText.Foreground = Brushes.DarkGray;
            FlippedGrid.Visibility = Visibility.Collapsed;

            // TODO: Card and state should not be set separately
            // Background Color
            var playerBrush = DefaultBackground;
            if (_field != null && _field.Card != null && _field.Card.Owner != null)
            {
                if (_field.Card.Owner.PlayerNumber == 1) playerBrush = Brushes.Blue;
                else if (_field.Card.Owner.PlayerNumber == 2) playerBrush = Brushes.Gold;
            }

            var hasBoost = _field != null && _field.Card != null &&
                _field.Card is OnlineCard &&
                ((OnlineCard)_field.Card).HasBoost;

            switch (_displayState)
            {
                case BoardFieldViewDisplayState.OnlineCardFlipped:
                    FlippedGrid.Visibility = Visibility.Visible;
                    Background = playerBrush;
                    break;
                case BoardFieldViewDisplayState.StackLinkEmpty:
                    LinkGrid.Visibility = Visibility.Visible;
                    Background = Brushes.Black;
                    break;
                case BoardFieldViewDisplayState.StackVirusEmpty:
                    VirusGrid.Visibility = Visibility.Visible;
                    Background = Brushes.Black;
                    break;
                case BoardFieldViewDisplayState.MainLink:
                case BoardFieldViewDisplayState.StackLink:
                case BoardFieldViewDisplayState.ExitLink:
                    LinkGrid.Visibility = Visibility.Visible;
                    Background = playerBrush;
                    LinkPath.Stroke = Brushes.White;
                    LinkPath.Fill = Brushes.White;
                    LinkText.Foreground = Brushes.White;
                    break;
                case BoardFieldViewDisplayState.MainVirus:
                case BoardFieldViewDisplayState.StackVirus:
                case BoardFieldViewDisplayState.ExitVirus:
                    VirusGrid.Visibility = Visibility.Visible;
                    Background = playerBrush;
                    VirusPath.Stroke = Brushes.White;
                    VirusPath.Fill = Brushes.White;
                    VirusText.Foreground = Brushes.White;
                    if (hasBoost)
                    {
                        LineBoostGrid.Visibility = Visibility.Visible;
                        //LineBoostGrid.Opacity = 1; Do not set
                    }
                    break;
                case BoardFieldViewDisplayState.Empty:
                    if (_field.Type == BoardFieldType.Stack) Background = Brushes.Black;
                    else Background = DefaultBackground;
                    break;
                case BoardFieldViewDisplayState.LineBoost:
                    LineBoostGrid.Visibility = Visibility.Visible;
                    LineBoostGrid.Opacity = 1;
                    break;
                case BoardFieldViewDisplayState.Firewall:
                    Background = playerBrush;
                    FirewallGrid.Visibility = Visibility.Visible;
                    break;
                case BoardFieldViewDisplayState.VirusCheck:
                    VirusCheckGrid.Visibility = Visibility.Visible;
                    break;
                case BoardFieldViewDisplayState.NotFound404:
                    NotFound404Grid.Visibility = Visibility.Visible;
                    break;
                case BoardFieldViewDisplayState.ExitEmpty:
                    ExitBox.Visibility = Visibility.Visible;
                    Background = DefaultBackground;
                    break;
            }
        }

        ///// <summary>
        ///// Only used for Line Boost or Firewall field
        ///// </summary>
        //public bool IsSelected
        //{
        //    get; set;
        //}

        public event EventHandler<BoardFieldClickedEventArgs> Clicked;

        public BoardFieldView()
        {
            InitializeComponent();
            DefaultBackground = new SolidColorBrush(Color.FromArgb(255, 0x1f, 0x1f, 0x1f));
            MouseDown += CardField_MouseDown;
            MouseUp += CardField_MouseUp;
            MouseLeave += CardField_MouseLeave;
            Cursor = Cursors.Hand;
            _displayState = BoardFieldViewDisplayState.Empty;
            var blinkDesk = DependencyPropertyDescriptor.FromProperty(
                    IsBlinkingProperty, typeof(BoardFieldView));
            if (blinkDesk != null)
            {
                blinkDesk.AddValueChanged(this, (s, e) =>
                {
                    Blink(IsBlinking);
                });
            }
        }

        #region Mouse Events
        private void CardField_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsMouseCaptured) ReleaseMouseCapture();
            _clickStarted = false;
        }

        private void CardField_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsMouseCaptured) ReleaseMouseCapture();
            if (_clickStarted)
            {
                _clickStarted = false;
                var handler = Clicked;
                // Click makes only sense if _field is set
                if (handler != null && _field != null) handler(this, new BoardFieldClickedEventArgs(_field));
            }
        }

        bool _clickStarted;
        private void CardField_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CaptureMouse())
            {
                _clickStarted = true;
            }

        }
        #endregion

        MainWindowViewModel _context;

        // TODO: Possible MVVM pattern break?
        public void Initialize(BoardFieldViewModel field, StoryboardAsyncWrapper blinkStoryboard, StoryboardAsyncWrapper lineBoostStoryboard)
        {
            _blinkStoryboard = blinkStoryboard;
            _lineBoostStoryboard = lineBoostStoryboard;
            var b = Background as SolidColorBrush;
            if (b != null)
            {
                DefaultBackground = new SolidColorBrush(Color.FromArgb(255,
                    b.Color.R, b.Color.G, b.Color.B));
            }
            if (_field != null) return; // Can only be set once
            _field = field;
            if (_field == null) return;

            //if (_field.Type == BoardFieldType.Exit)
            //{
            //    if (_field.Position.Y == 0)
            //    {
            //        ExitTransform.ScaleX = -1;
            //        ExitTransform.ScaleY = -1;
            //    }
            //    //if (_field.Position.Y == 7)
            //    //{
            //    //    ExitTransform.ScaleX = 1;
            //    //    ExitTransform.ScaleY = 1;
            //    //}
            //}


            _context = DataContext as MainWindowViewModel;
            if (_context != null) WeakEventManager<MainWindowViewModel, BlinkChangedEventArgs>.AddHandler(_context, "BlinkStateChanged", ViewModel_BlinkStateChanged);
            WeakEventManager<BoardFieldViewModel, PropertyChangedEventArgs>.AddHandler(field, "PropertyChanged", Field_PropertyChanged);
            WeakEventManager<BoardFieldViewModel, EventArgs>.AddHandler(field, "CardChanged", Field_CardChanged);

            // Setup LineBoost Animation
            var anim1 = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(1)) { BeginTime = TimeSpan.FromSeconds(0) };
            var anim2 = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(1)) { BeginTime = TimeSpan.FromSeconds(2) };
            Storyboard.SetTarget(anim1, CardGrid);
            Storyboard.SetTargetProperty(anim1, new PropertyPath("Opacity"));
            Storyboard.SetTarget(anim2, CardGrid);
            Storyboard.SetTargetProperty(anim2, new PropertyPath("Opacity"));
            //_lineBoostStoryBoard.Children.Add(anim1);
            //_lineBoostStoryBoard.Children.Add(anim2);

            // Setup LineBoost Animation
            var anim3 = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(1)) { BeginTime = TimeSpan.FromSeconds(0) };
            var anim4 = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(1)) { BeginTime = TimeSpan.FromSeconds(2) };
            Storyboard.SetTarget(anim3, LineBoostGrid);
            Storyboard.SetTargetProperty(anim3, new PropertyPath("Opacity"));
            Storyboard.SetTarget(anim4, LineBoostGrid);
            Storyboard.SetTargetProperty(anim4, new PropertyPath("Opacity"));
            //_lineBoostStoryBoard.Children.Add(anim3);
            //_lineBoostStoryBoard.Children.Add(anim4);

            _lineBoostAnimations.Add(anim1);
            _lineBoostAnimations.Add(anim2);
            _lineBoostAnimations.Add(anim3);
            _lineBoostAnimations.Add(anim4);

            UpdateField();
        }

        void ViewModel_BlinkStateChanged(object sender, BlinkChangedEventArgs e)
        {
            if (!e.ForceAll && !e.Position.Equals(_field.Position)) return;
            IsBlinking = _context.GetBlink(_field.Position);
        }

        void Field_CardChanged(object sender, EventArgs e)
        {
            UpdateField();
        }

        /// <summary>
        /// Event handler for property changes of _field. WeakEventManager is used.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Field_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Card")
            {
                UpdateField();
            }
            // TODO: Update Background if Owner of card changes
        }

        void UpdateField()
        {
            if (_field.Position.Y < 10)
            {
                UpdateLineBoostAnimation();
                if (_field.Type == BoardFieldType.Exit)
                {
                    if (_field.Card == null)
                    {
                        ExitBox.Visibility = Visibility.Visible;
                        DisplayState = BoardFieldViewDisplayState.ExitEmpty;
                    }
                    else
                    {
                        ExitBox.Visibility = Visibility.Hidden;
                        if (_field.Card is OnlineCard && (!((OnlineCard)_field.Card).IsFaceUp && _field.Card.Owner.PlayerNumber == 2)) // TODO ??? Player specific
                            DisplayState = BoardFieldViewDisplayState.OnlineCardFlipped;
                        else if (_field.Card is OnlineCard && ((OnlineCard)_field.Card).Type == OnlineCardType.Virus)
                            DisplayState = BoardFieldViewDisplayState.MainVirus;
                        else if (_field.Card is OnlineCard && ((OnlineCard)_field.Card).Type == OnlineCardType.Link)
                            DisplayState = BoardFieldViewDisplayState.MainLink;
                    }                  

                    if (_context != null)
                        IsBlinking = _context.GetBlink(_field.Position);
                    return;
                }
                else
                {
                    ExitBox.Visibility = Visibility.Hidden; // TODO: Maybe not required
                }

                if (_field.Card == null)
                {
                    if (_field.Type == BoardFieldType.Stack)
                    {
                        if (_field.Position.X < 4) DisplayState = BoardFieldViewDisplayState.StackLinkEmpty;
                        else  DisplayState = BoardFieldViewDisplayState.StackVirusEmpty;
                    }
                    else
                        DisplayState = BoardFieldViewDisplayState.Empty;
                    if (_context != null)
                        IsBlinking = _context.GetBlink(_field.Position);
                    return;
                }
                if (_field.Card is OnlineCard && (!((OnlineCard)_field.Card).IsFaceUp && _field.Card.Owner.PlayerNumber == 2))
                    DisplayState = BoardFieldViewDisplayState.OnlineCardFlipped;
                else if (_field.Card is OnlineCard && ((OnlineCard)_field.Card).Type == OnlineCardType.Virus)
                    DisplayState = (_field.Type == BoardFieldType.Stack) ? BoardFieldViewDisplayState.StackVirus : BoardFieldViewDisplayState.MainVirus;
                else if (_field.Card is OnlineCard && ((OnlineCard)_field.Card).Type == OnlineCardType.Link)
                    DisplayState = (_field.Type == BoardFieldType.Stack) ? BoardFieldViewDisplayState.StackLink : BoardFieldViewDisplayState.MainLink;
                else if (_field.Card is FirewallCard)
                    DisplayState = BoardFieldViewDisplayState.Firewall;
            }
            if (_context != null)
                IsBlinking = _context.GetBlink(_field.Position);
        }

        void InitializeAnimation()
        {            
            var backCol = ((SolidColorBrush)Background).Color;
            _primaryBackground = Color.FromArgb(255, backCol.R, backCol.G, backCol.B);
            // Overwrite Background because its instance is shared between other fields.
            Background = new SolidColorBrush(_primaryBackground);

            byte r = _primaryBackground.R;
            byte g = _primaryBackground.G;
            byte b = _primaryBackground.B;
            double h, s, v;
            ColorHelper.RgbToHsv(r, g, b, out h, out s, out v);
            if (s > 0.15) s = .15; else if (s>0.01) s = .85;
            if (v < 0.95) v = .95; else v = .05;
            ColorHelper.HsvToRgb(h, s, v, out r, out g, out b);
            _blinkTargetColor = Color.FromArgb(255, r, g, b);
            
            _blinkAnimation = new ColorAnimation(_primaryBackground, _blinkTargetColor, TimeSpan.FromSeconds(1))
            {
                BeginTime = TimeSpan.FromSeconds(0),
                AutoReverse = true,
            };
            Storyboard.SetTarget(_blinkAnimation, this);
            Storyboard.SetTargetProperty(_blinkAnimation, new PropertyPath("Background.Color"));
            _isAnimationInitialized = true;
        }

        public static readonly DependencyProperty IsBlinkingProperty =
                DependencyProperty.Register("IsBlinking", typeof(bool),
                typeof(BoardFieldView), new FrameworkPropertyMetadata(false));

        public bool IsBlinking
        {
            get { return (bool)GetValue(IsBlinkingProperty); }
            set { SetValue(IsBlinkingProperty, value); }
        }

        void Blink(bool on = true)
        {
            // Check if blinking is really necessary
            //if (_field.Type == BoardFieldType.Exit) return;

            _blinkStoryboard.RemoveAnimation(_blinkAnimation);

            if (!_isAnimationInitialized || _blinkAnimation == null)
            {
                InitializeAnimation();
            }

            if (on)
            {
                _blinkStoryboard.AddAnimation(_blinkAnimation);
            }

            // Background gets stuck in a wrong color. Fix:
            //UpdateDisplayState();
        }

        
        void UpdateLineBoostAnimation()
        {
            bool shouldAnimate =
                _field.Card != null &&
                _field.Card is OnlineCard &&
                ((OnlineCard)_field.Card).HasBoost && 
                _field.Type == BoardFieldType.Main;

            if (_lineBoostAnimationStarted == shouldAnimate) return;

            if (_lineBoostAnimationStarted)
            {
                LineBoostGrid.Visibility = Visibility.Hidden;
                //_lineBoostStoryBoard.Stop(this);
                foreach (var tl in _lineBoostAnimations)
                    _lineBoostStoryboard.RemoveAnimation(tl);
                _lineBoostAnimationStarted = false;
                // Background gets stuck in a wrong color. Fix:
                UpdateDisplayState();
            }
            else
            {
                LineBoostGrid.Visibility = Visibility.Visible;
                //_lineBoostStoryBoard.Begin(this, true);
                foreach (var tl in _lineBoostAnimations)
                    _lineBoostStoryboard.AddAnimation(tl);
                _lineBoostAnimationStarted = true;
            }

        }
    }
}
