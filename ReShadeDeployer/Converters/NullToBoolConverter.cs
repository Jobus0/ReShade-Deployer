using System;
using System.Globalization;
using System.Windows.Data;

namespace ReShadeDeployer.Converters;

/// <summary>
/// Converter for binding control enabled state based on whether a value is null.
/// Set ConverterParameter to "Invert" to return false when the value is not null (default is true when not null).
/// </summary>
[ValueConversion(typeof(object), typeof(bool))]
public class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool invert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;
        
        bool isNull = value == null;
        bool shouldBeEnabled = invert ? isNull : !isNull;
        
        return shouldBeEnabled;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
