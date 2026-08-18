using System.Globalization;
using System.Windows.Data;
using ReShadeDeployer.Converters;

namespace ReShadeDeployer.Tests.Converters;

public class NullToBoolConverterTests
{
    private readonly NullToBoolConverter _converter = new();
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    [Fact]
    public void Convert_ReturnsTrue_WhenValueIsNotNull()
    {
        // Arrange
        object value = "some value";
        object? parameter = null;

        // Act
        var result = _converter.Convert(value, typeof(bool), parameter, _culture);

        // Assert
        Assert.True((bool)result);
    }

    [Fact]
    public void Convert_ReturnsFalse_WhenValueIsNull()
    {
        // Arrange
        object? value = null;
        object? parameter = null;

        // Act
        var result = _converter.Convert(value, typeof(bool), parameter, _culture);

        // Assert
        Assert.False((bool)result);
    }

    [Fact]
    public void Convert_WithInvertParameter_ReturnsFalse_WhenValueIsNotNull()
    {
        // Arrange
        object value = "some value";
        string parameter = "Invert";

        // Act
        var result = _converter.Convert(value, typeof(bool), parameter, _culture);

        // Assert
        Assert.False((bool)result);
    }

    [Fact]
    public void Convert_WithInvertParameter_ReturnsTrue_WhenValueIsNull()
    {
        // Arrange
        object? value = null;
        string parameter = "Invert";

        // Act
        var result = _converter.Convert(value, typeof(bool), parameter, _culture);

        // Assert
        Assert.True((bool)result);
    }

    [Fact]
    public void Convert_WithInvertParameterCaseInsensitive_ReturnsFalse_WhenValueIsNotNull()
    {
        // Arrange
        object value = "some value";
        string parameter = "invert";

        // Act
        var result = _converter.Convert(value, typeof(bool), parameter, _culture);

        // Assert
        Assert.False((bool)result);
    }

    [Fact]
    public void Convert_WithInvertParameterMixedCase_ReturnsFalse_WhenValueIsNotNull()
    {
        // Arrange
        object value = "some value";
        string parameter = "InVeRt";

        // Act
        var result = _converter.Convert(value, typeof(bool), parameter, _culture);

        // Assert
        Assert.False((bool)result);
    }

    [Fact]
    public void Convert_WithNonInvertParameter_ReturnsTrue_WhenValueIsNotNull()
    {
        // Arrange
        object value = "some value";
        string parameter = "SomeOtherValue";

        // Act
        var result = _converter.Convert(value, typeof(bool), parameter, _culture);

        // Assert
        Assert.True((bool)result);
    }

    [Fact]
    public void Convert_WithNonInvertParameter_ReturnsFalse_WhenValueIsNull()
    {
        // Arrange
        object? value = null;
        string parameter = "SomeOtherValue";

        // Act
        var result = _converter.Convert(value, typeof(bool), parameter, _culture);

        // Assert
        Assert.False((bool)result);
    }

    [Fact]
    public void Convert_WithEmptyString_ReturnsTrue_WhenValueIsNotNull()
    {
        // Arrange
        object value = "some value";
        string parameter = "";

        // Act
        var result = _converter.Convert(value, typeof(bool), parameter, _culture);

        // Assert
        Assert.True((bool)result);
    }

    [Fact]
    public void Convert_WithEmptyString_ReturnsFalse_WhenValueIsNull()
    {
        // Arrange
        object? value = null;
        string parameter = "";

        // Act
        var result = _converter.Convert(value, typeof(bool), parameter, _culture);

        // Assert
        Assert.False((bool)result);
    }

    [Fact]
    public void ConvertBack_AlwaysReturnsBindingDoNothing()
    {
        // Arrange
        bool value = true;
        object? parameter = null;

        // Act
        var result = _converter.ConvertBack(value, typeof(object), parameter, _culture);

        // Assert
        Assert.Equal(Binding.DoNothing, result);
    }
}
