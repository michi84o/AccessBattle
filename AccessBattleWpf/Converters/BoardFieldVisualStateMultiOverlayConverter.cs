using System;
using System.Globalization;
using System.Windows.Data;

namespace AccessBattle.Wpf.Converters
{
    public class BoardFieldVisualStateMultiOverlayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BoardFieldVisualState)
            {
                var val = (int)(BoardFieldVisualState)value;
                if ((val & (val - 1)) != 0)
                    return true;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
