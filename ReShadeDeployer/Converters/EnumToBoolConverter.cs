using System;
using System.Globalization;
using System.Windows.Data;

namespace ReShadeDeployer.Converters;

/// <summary>
/// Two-way converter for binding a single enum property to a group of RadioButton.IsChecked bindings.
/// Set ConverterParameter to the enum value this radio button represents.
/// </summary>
[ValueConversion(typeof(Enum), typeof(bool))]
public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.Equals(parameter) == true;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? parameter : Binding.DoNothing;
}
