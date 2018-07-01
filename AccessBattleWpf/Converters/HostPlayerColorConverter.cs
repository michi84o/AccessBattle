using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AccessBattle.Wpf.Converters
{
    public enum PlayerColorConverterMode
    {
        MainField,
        ServerField
    }

    public class HostPlayerColorConverter : IValueConverter
    {
        public PlayerColorConverterMode Mode { get; set; }
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                var val = (bool)value;
                if (Invert) val = !val;
                if (val)
                {
                    if (Mode == PlayerColorConverterMode.MainField)
                        return new SolidColorBrush(Color.FromRgb(21,21,96)); // #151560 Blue
                    if (Mode == PlayerColorConverterMode.ServerField)
                        return Brushes.Blue;

                }
                else
                {
                    if (Mode == PlayerColorConverterMode.MainField)
                        return new SolidColorBrush(Color.FromRgb(96, 96, 21)); // #606015 Orange
                    if (Mode == PlayerColorConverterMode.ServerField)
                        return Brushes.Gold;
                }
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
