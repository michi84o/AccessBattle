using System;
using System.Globalization;
using System.Windows.Data;

namespace AccessBattle.Wpf.Converters
{
    public class BoolInverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
                return !(bool)value;
            if (value is bool?)
                return !((bool?)value == true);
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
                return !(bool)value;
            if (value is bool?)
                return !((bool?)value == true);
            return true;
        }
    }
}
