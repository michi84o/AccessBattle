using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
