using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AccessBattleWpf
{
    public class StretchConverter : IValueConverter
    {
        public double StretchFactor { get; set; }

        public StretchConverter()
        {
            StretchFactor = 1;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double && targetType == typeof(double))
            {
                var val = (double)value * StretchFactor;
                if (val < 1) val = 1;
                return val;
            }
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double && targetType == typeof(double))
            {
                return (double)value / StretchFactor;
            }
            throw new NotImplementedException();
        }
    }
}
