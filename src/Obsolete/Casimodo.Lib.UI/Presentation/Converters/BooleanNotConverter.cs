using System;
using System.Globalization;
using System.Windows.Data;

namespace Casimodo.Lib.Presentation.Converters
{
    public class BooleanNotConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            return !(bool)value;
        }
    }
}
