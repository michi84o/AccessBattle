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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AccessBattleWpf
{
    public class CardFieldClickedEventArgs : EventArgs
    {
        public BoardField Field { get; private set; }
        public CardFieldClickedEventArgs(BoardField field)
        {
            Field = field;
        }
    }

    public class CardField : Border
    {
        BoardField _field;
        Color _defaultBackground;
        Color _blinkTargetColor;
        Storyboard _blinkStoryBoard;   
        public bool IsExitField { get; set; }     

        public event EventHandler<CardFieldClickedEventArgs> Clicked;

        public CardField() : base()
        {
            MouseDown += CardField_MouseDown;
            MouseUp += CardField_MouseUp;
            MouseLeave += CardField_MouseLeave;
            Cursor = Cursors.Hand;

            //var blinkDesc = DependencyPropertyDescriptor.FromProperty(IsBlinkingProperty, typeof(CardField));
            //if (blinkDesc != null)
            //{
            //    // TODO: People keep telling this could be a memory leak
            //    blinkDesc.AddValueChanged(this, (s, e) => 
            //    {
            //        if (IsBlinking != _isBlinking)
            //        {
            //            _isBlinking = IsBlinking;
            //            Blink(_isBlinking);
            //        }
            //    });
            //}

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
                if (handler != null && _field != null) handler(this, new CardFieldClickedEventArgs(_field));
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
            if (_field.Card == null) Child = null;
            if (_field.Card is VirusCard) Child = new VirusCardControl();
            if (_field.Card is LinkCard) Child = new LinkCardControl();
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
                _blinkStoryBoard.Stop(this);
            _blinkStoryBoard.Seek(TimeSpan.Zero);
        }
    }
}
