using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace AvaloniaManager.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool IsInverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return IsInverse ? !boolValue : boolValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}