using System;
using System.Windows.Data;
using System.Threading;
using System.Globalization;

namespace Casimodo.Lib.Presentation.Converters
{
    public class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            return ((DateTime)value).ToString(Thread.CurrentThread.CurrentUICulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Convertion of a string to DateTime is not supported.");
        }
    }
}
