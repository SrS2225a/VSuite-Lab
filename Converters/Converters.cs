using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace VSuiteLab.Converters
{

    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is int i && i > 0;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    // Converts null => false, not null => true
    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value != null;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
    
    public class ColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string hex)
            {
                return Color.Parse(hex);
            }
            return Colors.White;
        }
    
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return color.ToString();
            }
            return Colors.White.ToString();
        }
    }
    
    public class DateTimeToLocalizedStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not DateTimeOffset dt)
                return string.Empty;

            var datePart = dt.ToString("MMM dd, yyyy", culture);
            var timePart = dt.ToString("t", culture);

            return $"{datePart} • {timePart}";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}