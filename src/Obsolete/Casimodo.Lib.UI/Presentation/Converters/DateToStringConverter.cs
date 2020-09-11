using System;
using System.Windows.Data;
using System.Threading;
using System.Globalization;

namespace Casimodo.Lib.Presentation.Converters
{
    public class DateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            return ((DateTime)value).Date.ToString("d", Thread.CurrentThread.CurrentUICulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Convertion of a string to DateTime is not supported.");
        }
    }

    public class HourMinuteToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            var time = (DateTime)value;

            return ((DateTime)value).ToString("HH:mm");
        }

        public object ConvertBack(object val, Type targetType, object parameter, CultureInfo culture)
        {
            string value = val as string;
            if (string.IsNullOrWhiteSpace(value))
                return null;

            DateTime result;
            if (DateTime.TryParse("01.01.2000 " + value, out result))
                return result;
            else
                return value;
        }
    }
}
