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
    public partial class BoardFieldControl : Border
    {
        BoardField _field;
        Color _defaultBackground;
        Color _blinkTargetColor;
        Storyboard _blinkStoryBoard;
        public bool IsExitField { get; set; }
        public bool IsStackField { get; set; }

        // Todo: Resource
        static SolidColorBrush EmptyMainBrush = new SolidColorBrush(Color.FromArgb(255, 0x1f, 0x1f, 0x1f));

        BoardFieldControlDisplayState _displayState;
        public BoardFieldControlDisplayState DisplayState
        {
            get { return _displayState; }
            set
            {
                // Reset blinking and force rebuild of storyboard                  
                IsBlinking = false;
                _initialized = false;

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
                    case BoardFieldControlDisplayState.StackLinkEmpty:
                        LinkGrid.Visibility = Visibility.Visible;
                        Background = Brushes.Black;
                        break;
                    case BoardFieldControlDisplayState.StackVirusEmpty:
                        VirusGrid.Visibility = Visibility.Visible;
                        Background = Brushes.Black;
                        break;                    
                    case BoardFieldControlDisplayState.MainLink:
                    case BoardFieldControlDisplayState.StackLink:
                        LinkGrid.Visibility = Visibility.Visible;
                        Background = playerBrush;
                        LinkPath.Stroke = Brushes.White;
                        LinkPath.Fill = Brushes.White;
                        LinkText.Foreground = Brushes.White;
                        break;
                    case BoardFieldControlDisplayState.MainVirus:
                    case BoardFieldControlDisplayState.StackVirus:
                        VirusGrid.Visibility = Visibility.Visible;
                        Background = playerBrush;
                        VirusPath.Stroke = Brushes.White;
                        VirusPath.Fill = Brushes.White;
                        VirusText.Foreground = Brushes.White;
                        break;
                    case BoardFieldControlDisplayState.Empty:
                        if (IsStackField) Background = Brushes.Black;
                        else Background = EmptyMainBrush;
                        break;
                }

                // TODO: Solve synchronization Issue
                //IsBlinking = _field != null && _field.IsHighlighted;
            }

        }

        public event EventHandler<BoardFieldClickedEventArgs> Clicked;

        public BoardFieldControl()
        {
            InitializeComponent();
            MouseDown += CardField_MouseDown;
            MouseUp += CardField_MouseUp;
            MouseLeave += CardField_MouseLeave;
            Cursor = Cursors.Hand;
            _displayState = BoardFieldControlDisplayState.Empty;
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

        // TODO: Possible MVVM pattern break?
        public void SetBoardField(BoardField field)
        {
            if (_field != null) return; // Can only be set once
            _field = field;
            if (_field == null) return;
            WeakEventManager<BoardField, PropertyChangedEventArgs>.AddHandler(field, "PropertyChanged", Field_PropertyChanged);
            UpdateField();
            Initialize();
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
            else if (e.PropertyName == "IsHighlighted")
            {
                IsBlinking = _field.IsHighlighted; // TODO: Could be done with dep-prop and databinding
            }
        }

        void UpdateField()
        {
            if (IsExitField) return;
            // Draw stuff here
            // BAD !!! Removes ViewBox fromExit fields
            if (_field.Card == null)
            {
                // TODO: Save info about stack panel type somewhere
                if (IsStackField)
                {
                    if (DisplayState == BoardFieldControlDisplayState.StackLink) DisplayState = BoardFieldControlDisplayState.StackLinkEmpty;
                    else if (DisplayState == BoardFieldControlDisplayState.StackVirus) DisplayState = BoardFieldControlDisplayState.StackVirusEmpty;
                    else
                    {
                        Trace.WriteLine("ERROR! LOST INFO ABOUT STACK PANEL TYPE");
                        DisplayState = BoardFieldControlDisplayState.Empty;
                    }
                }
                else DisplayState = BoardFieldControlDisplayState.Empty;
            }                
            if (_field.Card is VirusCard)
                DisplayState = IsStackField ? BoardFieldControlDisplayState.StackVirus : BoardFieldControlDisplayState.MainVirus;
            if (_field.Card is LinkCard)
                DisplayState = IsStackField ? BoardFieldControlDisplayState.StackLink : BoardFieldControlDisplayState.MainLink;
        }

        bool _initialized;
        void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
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

            _blinkStoryBoard = new Storyboard
            {
                Duration = TimeSpan.FromSeconds(2),
                RepeatBehavior = RepeatBehavior.Forever
            };

            var animation1 = new ColorAnimation(_defaultBackground, _blinkTargetColor, TimeSpan.FromSeconds(1))
            {
                BeginTime = TimeSpan.FromSeconds(0)
            };
            Storyboard.SetTarget(animation1, this);
            Storyboard.SetTargetProperty(animation1, new PropertyPath("Background.Color"));
            _blinkStoryBoard.Children.Add(animation1);

            var animation2 = new ColorAnimation(_blinkTargetColor, _defaultBackground, TimeSpan.FromSeconds(1))
            {
                BeginTime = TimeSpan.FromSeconds(1),
            };
            Storyboard.SetTarget(animation2, this);
            Storyboard.SetTargetProperty(animation2, new PropertyPath("Background.Color"));
            _blinkStoryBoard.Children.Add(animation2);
        }

        //public static readonly DependencyProperty IsBlinkingProperty =
        //        DependencyProperty.Register("IsBlinking", typeof(bool),
        //        typeof(CardField), new FrameworkPropertyMetadata(false));

        bool _isBlinking;
        //public bool IsBlinking
        //{
        //    get { return (bool)GetValue(IsBlinkingProperty); }
        //    set { SetValue(IsBlinkingProperty, value); }
        //}
        public bool IsBlinking
        {
            get { return _isBlinking; }
            set
            {
                if (_isBlinking == value) return;
                _isBlinking = value;
                Blink(_isBlinking);
            }
        }

        void Blink(bool on = true)
        {
            Initialize();
            if (on)
                _blinkStoryBoard.Begin(this, true);
            else
            {
                _blinkStoryBoard.Stop(this);
                _blinkStoryBoard.Seek(TimeSpan.Zero);
            }
        }
    }
}
