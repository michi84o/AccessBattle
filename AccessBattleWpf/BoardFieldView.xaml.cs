using AccessBattle;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
    /// Interaction logic for BoardFieldControl.xaml
    /// </summary>
    public partial class BoardFieldView : Border
    {
        BoardField _field;
        Color _defaultBackground;
        Color _blinkTargetColor;

        Storyboard _blinkStoryboard;
        ColorAnimation _blinkAnimation;
        bool _isAnimationInStoryboard;
        bool _isAnimationInitialized;
        FrameworkElement _blinkStoryboardOwner;

        // Todo: Resource
        static SolidColorBrush EmptyMainBrush = new SolidColorBrush(Color.FromArgb(255, 0x1f, 0x1f, 0x1f));

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

                // TODO: Databinding
                LinkGrid.Visibility = Visibility.Collapsed;
                VirusGrid.Visibility = Visibility.Collapsed;                
                VirusPath.Stroke = Brushes.DarkGray;
                VirusPath.Fill = Brushes.DarkGray;
                LinkPath.Stroke = Brushes.DarkGray;
                LinkPath.Fill = Brushes.DarkGray;
                VirusText.Foreground = Brushes.DarkGray;
                LinkText.Foreground = Brushes.DarkGray;

                // TODO: Card and state should not be set separately
                // Background Color
                var playerBrush = EmptyMainBrush;
                if (_field != null && _field.Card != null && _field.Card.Owner != null)
                {
                    if (_field.Card.Owner.PlayerNumber == 1) playerBrush = Brushes.Blue;
                    else if (_field.Card.Owner.PlayerNumber == 2) playerBrush = Brushes.Gold;
                }

                switch (_displayState)
                {
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
                        LinkGrid.Visibility = Visibility.Visible;
                        Background = playerBrush;
                        LinkPath.Stroke = Brushes.White;
                        LinkPath.Fill = Brushes.White;
                        LinkText.Foreground = Brushes.White;
                        break;
                    case BoardFieldViewDisplayState.MainVirus:
                    case BoardFieldViewDisplayState.StackVirus:
                        VirusGrid.Visibility = Visibility.Visible;
                        Background = playerBrush;
                        VirusPath.Stroke = Brushes.White;
                        VirusPath.Fill = Brushes.White;
                        VirusText.Foreground = Brushes.White;
                        break;
                    case BoardFieldViewDisplayState.Empty:
                        if (_field.Type == BoardFieldType.Stack) Background = Brushes.Black;
                        else Background = EmptyMainBrush;
                        break;
                }
            }

        }

        public event EventHandler<BoardFieldClickedEventArgs> Clicked;

        public BoardFieldView()
        {
            InitializeComponent();
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
        public void Inititialize(BoardField field, Storyboard blinkStoryboard, FrameworkElement blinkStoryboardOwner)
        {
            _blinkStoryboard = blinkStoryboard;
            _blinkStoryboardOwner = blinkStoryboardOwner;

            if (_field != null) return; // Can only be set once
            _field = field;
            if (_field == null) return;
            _context = DataContext as MainWindowViewModel;
            if (_context != null) WeakEventManager<MainWindowViewModel, BlinkChangedEventArgs>.AddHandler(_context, "BlinkStateChanged", ViewModel_BlinkStateChanged);
            WeakEventManager<BoardField, PropertyChangedEventArgs>.AddHandler(field, "PropertyChanged", Field_PropertyChanged);
            UpdateField();
        }

        void ViewModel_BlinkStateChanged(object sender, BlinkChangedEventArgs e)
        {
            if (!e.Position.Equals(_field.Position)) return;
            IsBlinking = _context.GetBlink(_field.Position);
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
        }

        void UpdateField()
        {            
            if (_field.Type == BoardFieldType.Exit) return;
            if (_field.Card == null)
            {
                if (_field.Type == BoardFieldType.Stack)
                {
                    if (DisplayState == BoardFieldViewDisplayState.StackLink) DisplayState = BoardFieldViewDisplayState.StackLinkEmpty;
                    else if (DisplayState == BoardFieldViewDisplayState.StackVirus) DisplayState = BoardFieldViewDisplayState.StackVirusEmpty;
                    else
                    {
                        Trace.WriteLine("ERROR! LOST INFO ABOUT STACK PANEL TYPE");
                        DisplayState = BoardFieldViewDisplayState.Empty;
                    }
                }
                else DisplayState = BoardFieldViewDisplayState.Empty;
                return;
            }                
            if (_field.Card is OnlineCard && ((OnlineCard)_field.Card).Type == OnlineCardType.Virus)
                DisplayState = (_field.Type == BoardFieldType.Stack) ? BoardFieldViewDisplayState.StackVirus : BoardFieldViewDisplayState.MainVirus;
            if (_field.Card is OnlineCard && ((OnlineCard)_field.Card).Type == OnlineCardType.Link)
                DisplayState = (_field.Type == BoardFieldType.Stack) ? BoardFieldViewDisplayState.StackLink : BoardFieldViewDisplayState.MainLink;

            if (_context != null)
                IsBlinking = _context.GetBlink(_field.Position);
        }


        void InitializeAnimation()
        {            
            var backCol = ((SolidColorBrush)Background).Color;
            _defaultBackground = Color.FromArgb(255, backCol.R, backCol.G, backCol.B);
            // Overwrite Background because its instance is shared between other fields.
            Background = new SolidColorBrush(_defaultBackground);

            byte r = _defaultBackground.R;
            byte g = _defaultBackground.G;
            byte b = _defaultBackground.B;
            double h, s, v;
            ColorHelper.RgbToHsv(r, g, b, out h, out s, out v);
            if (s > 0.15) s = .15; else s = .85;
            if (v < 0.95) v = .95; else v = .05;
            ColorHelper.HsvToRgb(h, s, v, out r, out g, out b);
            _blinkTargetColor = Color.FromArgb(255, r, g, b);
            
            _blinkAnimation = new ColorAnimation(_defaultBackground, _blinkTargetColor, TimeSpan.FromSeconds(1))
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
            if (_field.Type == BoardFieldType.Exit) return;

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

            // If Initialization is required, initialize
            if (!_isAnimationInitialized || _blinkAnimation == null)
            {
                InitializeAnimation();
            }

            if (on)
            {
                _blinkStoryboard.Children.Add(_blinkAnimation);
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
