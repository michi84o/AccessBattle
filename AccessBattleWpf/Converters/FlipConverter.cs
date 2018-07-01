using System;
using System.Globalization;
using System.Windows.Data;

namespace AccessBattle.Wpf.Converters
{
    class FlipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                if ((bool)value) return -1.0;
                return 1.0;
            }
            throw new InvalidOperationException("Must be bool value");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
