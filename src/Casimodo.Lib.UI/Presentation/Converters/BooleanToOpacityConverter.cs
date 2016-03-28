using System;
using System.Globalization;
using System.Windows.Data;

namespace Casimodo.Lib.Presentation.Converters
{

    public class BooleanToOpacityConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return (double)0.0;

            if (false == (bool)value)
                return (double)0.0;

            return (double)1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            return ((double)value != 0.0);
        }

        #endregion
    }
}
