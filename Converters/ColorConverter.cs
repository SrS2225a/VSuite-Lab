using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace VSuiteLab.Converters;

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