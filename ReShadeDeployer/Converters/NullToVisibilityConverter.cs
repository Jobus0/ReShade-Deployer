using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ReShadeDeployer.Converters;

/// <summary>
/// Converter for binding control visibility based on whether a value is null.
/// Set ConverterParameter to "Invert" to make the control visible when the value is null (default is invisible when null).
/// </summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool invert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;
        
        bool isNull = value == null;
        bool shouldBeVisible = invert ? isNull : !isNull;
        
        return shouldBeVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
