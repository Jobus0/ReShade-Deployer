using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ReShadeDeployer.Converters;

/// <summary>
/// Converter for binding control visibility based on whether an enum value matches a specific enum value.
/// Set ConverterParameter to the enum value to compare against.
/// Set ConverterParameter to "Invert" to make the control visible when the values don't match (default is visible when they match).
/// </summary>
[ValueConversion(typeof(Enum), typeof(Visibility))]
public class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool invert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;
        
        bool matches = value?.Equals(parameter) == true;
        bool shouldBeVisible = invert ? !matches : matches;
        
        return shouldBeVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
