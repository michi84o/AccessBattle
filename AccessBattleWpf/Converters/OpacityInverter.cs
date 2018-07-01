using System;
using System.Globalization;
using System.Windows.Data;

namespace AccessBattle.Wpf.Converters
{
    public class OpacityInverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double)
            {
                return 1 - (double)value;
            }
            return 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double)
            {
                return 1 - (double)value;
            }
            return 1;
        }
    }
}
