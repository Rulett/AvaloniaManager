﻿using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace AvaloniaManager.Converters
{
    public class IsNullConverter : IValueConverter
    {
        public static readonly IsNullConverter Instance = new IsNullConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}