using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace AccessBattle.Wpf.Coverters
{
    public class BoardFieldVisualStateVisibilityConverter : IValueConverter
    {
        public BoardFieldVisualState PrimaryState { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BoardFieldVisualState)
            {
                if (((BoardFieldVisualState)value & PrimaryState) == PrimaryState)
                    return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility && (Visibility)value == Visibility.Visible)
            {
                return PrimaryState;
            }
            return BoardFieldVisualState.Empty;
        }
    }
}
