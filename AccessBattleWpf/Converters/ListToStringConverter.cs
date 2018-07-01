using System;
using System.Globalization;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;

namespace AccessBattle.Wpf.Converters
{
    class ValidationErrorsToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var errors = value as System.Collections.ObjectModel.ReadOnlyCollection<ValidationError>;
            if (errors != null)
            {
                var str = new StringBuilder();
                foreach (var s in errors)
                {
                    str.Append(s.ErrorContent + "\r\n");
                }
                return str.ToString();
            }
            else return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }

    }
}
