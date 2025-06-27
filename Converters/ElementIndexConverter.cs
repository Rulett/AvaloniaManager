using Avalonia.Controls;
using Avalonia.Data.Converters;
using AvaloniaManager.Models;
using System.Globalization;
using System;

namespace AvaloniaManager.Converters
{
    public class ElementIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DataGridRow row)
            {
                var index = row.GetIndex() + 1; // +1 для нумерации с 1
                return index.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
