using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MedicalCenter.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Collapsed;

            bool inverse = parameter?.ToString() == "inverse";
            bool boolValue = false;

            if (value is bool b)
                boolValue = b;
            else if (value is int i)
                boolValue = i > 0;
            else if (value is double d)
                boolValue = d > 0;

            if (inverse)
                boolValue = !boolValue;

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
                return visibility == Visibility.Visible;
            return false;
        }
    }
}