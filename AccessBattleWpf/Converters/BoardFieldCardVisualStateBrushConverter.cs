using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace AccessBattle.Wpf.Converters
{
    class BoardFieldCardVisualStateBrushConverter : IValueConverter
    {
        public bool PathColorMode { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BoardFieldCardVisualState)
            {
                if (PathColorMode)
                {
                    switch ((BoardFieldCardVisualState)value)
                    {
                        case BoardFieldCardVisualState.Blue:
                        case BoardFieldCardVisualState.Orange:
                            return Brushes.White;
                    }
                }
                else
                {
                    switch ((BoardFieldCardVisualState)value)
                    {
                        case BoardFieldCardVisualState.Blue:
                            return Brushes.Blue;
                        case BoardFieldCardVisualState.Orange:
                            return Brushes.Goldenrod;
                    }
                }
            }
            return PathColorMode ? Brushes.DarkGray : Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = value as SolidColorBrush;
            if (v != null)
            {
                if (v == Brushes.Blue)
                    return BoardFieldCardVisualState.Blue;
                if (v == Brushes.Orange)
                    return BoardFieldCardVisualState.Orange;
            }
            return BoardFieldVisualState.Empty;
        }
    }
}
