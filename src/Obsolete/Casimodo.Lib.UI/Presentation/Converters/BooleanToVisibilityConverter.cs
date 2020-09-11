using System;
using System.Windows.Data;
using System.Windows;

namespace Casimodo.Lib.Presentation.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool effectiveValue = (value != null && (bool)value);

            Visibility notVisible = Visibility.Collapsed;

            if (parameter != null && parameter is string)
            {
                string param = (string)parameter;
                if (param == "Not")
                    effectiveValue = !effectiveValue;
                else if (param == "Hidden")
                    notVisible = Visibility.Hidden;
            }

            if (effectiveValue)
                return Visibility.Visible;

            return notVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool effectiveValue = (value != null && (Visibility)value == Visibility.Visible);

            if (parameter != null && parameter is string && (parameter as string) == "Not")
                effectiveValue = !effectiveValue;

            return effectiveValue;
        }

        #endregion
    }
}
