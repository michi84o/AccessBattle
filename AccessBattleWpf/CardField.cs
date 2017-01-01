using AccessBattle;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AccessBattleWpf
{
    public class CardField : Border
    {
        BoardField _field;
        Color _defaultBackground;
        Color _blinkTargetColor;
        public void SetBoardField(BoardField field)
        {
            if (_field != null) return; // Can only be set once
            _field = field;
            if (_field == null) return;
            WeakEventManager<BoardField, PropertyChangedEventArgs>.AddHandler(field, "PropertyChanged", Field_PropertyChanged);
            UpdateField();
            BackupBackground();
        }

        void Field_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Card")
            {
                UpdateField();
            }
        }

        void UpdateField()
        {

        }

        bool _backgroundSaved;
        void BackupBackground()
        {
            if (_backgroundSaved) return;
            var backCol = ((SolidColorBrush)Background).Color;
            _defaultBackground = Color.FromArgb(255, backCol.R, backCol.G, backCol.B);
            // Overwrite Background because its instance is shared between other fields.
            Background = new SolidColorBrush(_defaultBackground);
            _backgroundSaved = true;
            byte r = _defaultBackground.R;
            byte g = _defaultBackground.G;
            byte b = _defaultBackground.B;
            double h, s, v;
            ColorHelper.RgbToHsv(r, g, b, out h, out s, out v);
            if (s > 0.15)  s = .15;
            if (v < 0.95) v = .95;
            ColorHelper.HsvToRgb(h, s, v, out r, out g, out b);
            _blinkTargetColor = Color.FromArgb(255, r, g, b);
        }

        public void Blink()
        {
            BackupBackground();

            var storyboard = new Storyboard
            {
                Duration = TimeSpan.FromSeconds(2),
                RepeatBehavior = RepeatBehavior.Forever
            };

            var animation1 = new ColorAnimation(_defaultBackground, _blinkTargetColor, TimeSpan.FromSeconds(.5))
            {
                BeginTime = TimeSpan.FromSeconds(0)
            };
            Storyboard.SetTarget(animation1, this);
            Storyboard.SetTargetProperty(animation1, new PropertyPath("Background.Color"));
            storyboard.Children.Add(animation1);

            var animation2 = new ColorAnimation(_blinkTargetColor, _defaultBackground, TimeSpan.FromSeconds(1))
            {
                BeginTime = TimeSpan.FromSeconds(1), 
            };
            Storyboard.SetTarget(animation2, this);
            Storyboard.SetTargetProperty(animation2, new PropertyPath("Background.Color"));
            storyboard.Children.Add(animation2);

            BeginStoryboard(storyboard);
        }
    }
}
