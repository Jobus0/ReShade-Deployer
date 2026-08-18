using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ReShadeDeployer.Converters;

namespace ReShadeDeployer.Tests.Converters;

public class NullToVisibilityConverterTests
{
    private readonly NullToVisibilityConverter _converter = new();
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    [Fact]
    public void Convert_ReturnsVisible_WhenValueIsNotNull()
    {
        // Arrange
        object value = "some value";
        object? parameter = null;

        // Act
        var result = _converter.Convert(value, typeof(Visibility), parameter, _culture);

        // Assert
        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void Convert_ReturnsCollapsed_WhenValueIsNull()
    {
        // Arrange
        object? value = null;
        object? parameter = null;

        // Act
        var result = _converter.Convert(value, typeof(Visibility), parameter, _culture);

        // Assert
        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void Convert_WithInvertParameter_ReturnsCollapsed_WhenValueIsNotNull()
    {
        // Arrange
        object value = "some value";
        string parameter = "Invert";

        // Act
        var result = _converter.Convert(value, typeof(Visibility), parameter, _culture);

        // Assert
        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void Convert_WithInvertParameter_ReturnsVisible_WhenValueIsNull()
    {
        // Arrange
        object? value = null;
        string parameter = "Invert";

        // Act
        var result = _converter.Convert(value, typeof(Visibility), parameter, _culture);

        // Assert
        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void Convert_WithInvertParameterCaseInsensitive_ReturnsCollapsed_WhenValueIsNotNull()
    {
        // Arrange
        object value = "some value";
        string parameter = "invert";

        // Act
        var result = _converter.Convert(value, typeof(Visibility), parameter, _culture);

        // Assert
        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void Convert_WithInvertParameterMixedCase_ReturnsCollapsed_WhenValueIsNotNull()
    {
        // Arrange
        object value = "some value";
        string parameter = "InVeRt";

        // Act
        var result = _converter.Convert(value, typeof(Visibility), parameter, _culture);

        // Assert
        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void Convert_WithNonInvertParameter_ReturnsVisible_WhenValueIsNotNull()
    {
        // Arrange
        object value = "some value";
        string parameter = "SomeOtherValue";

        // Act
        var result = _converter.Convert(value, typeof(Visibility), parameter, _culture);

        // Assert
        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void Convert_WithNonInvertParameter_ReturnsCollapsed_WhenValueIsNull()
    {
        // Arrange
        object? value = null;
        string parameter = "SomeOtherValue";

        // Act
        var result = _converter.Convert(value, typeof(Visibility), parameter, _culture);

        // Assert
        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void Convert_WithEmptyString_ReturnsVisible_WhenValueIsNotNull()
    {
        // Arrange
        object value = "some value";
        string parameter = "";

        // Act
        var result = _converter.Convert(value, typeof(Visibility), parameter, _culture);

        // Assert
        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void Convert_WithEmptyString_ReturnsCollapsed_WhenValueIsNull()
    {
        // Arrange
        object? value = null;
        string parameter = "";

        // Act
        var result = _converter.Convert(value, typeof(Visibility), parameter, _culture);

        // Assert
        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void ConvertBack_AlwaysReturnsBindingDoNothing()
    {
        // Arrange
        Visibility value = Visibility.Visible;
        object? parameter = null;

        // Act
        var result = _converter.ConvertBack(value, typeof(object), parameter, _culture);

        // Assert
        Assert.Equal(Binding.DoNothing, result);
    }
}
