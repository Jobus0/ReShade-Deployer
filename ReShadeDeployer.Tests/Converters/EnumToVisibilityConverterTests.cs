using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ReShadeDeployer.Converters;

namespace ReShadeDeployer.Tests.Converters;

public class EnumToVisibilityConverterTests
{
    private readonly EnumToVisibilityConverter _converter = new();
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    [Fact]
    public void Convert_ReturnsVisible_WhenValueEqualsParameter()
    {
        // Arrange
        TestEnum value = TestEnum.OptionA;
        TestEnum parameter = TestEnum.OptionA;

        // Act
        var result = _converter.Convert(value, typeof(Visibility), parameter, _culture);

        // Assert
        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void Convert_ReturnsCollapsed_WhenValueDoesNotEqualParameter()
    {
        // Arrange
        TestEnum value = TestEnum.OptionA;
        TestEnum parameter = TestEnum.OptionB;

        // Act
        var result = _converter.Convert(value, typeof(Visibility), parameter, _culture);

        // Assert
        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void Convert_ReturnsCollapsed_WhenValueIsNull()
    {
        // Arrange
        TestEnum? value = null;
        TestEnum parameter = TestEnum.OptionA;

        // Act
        var result = _converter.Convert(value, typeof(Visibility), parameter, _culture);

        // Assert
        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void Convert_ReturnsCollapsed_WhenParameterIsNull()
    {
        // Arrange
        TestEnum value = TestEnum.OptionA;
        object? parameter = null;

        // Act
        var result = _converter.Convert(value, typeof(Visibility), parameter, _culture);

        // Assert
        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void Convert_WithInvertParameter_ReturnsVisible_WhenValueIsNotInvertString()
    {
        // Arrange
        TestEnum value = TestEnum.OptionA;
        string invertParameter = "Invert";

        // Act
        var result = _converter.Convert(value, typeof(Visibility), invertParameter, _culture);

        // Assert
        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void Convert_WithInvertParameter_ReturnsVisible_WhenValueIsNull()
    {
        // Arrange
        TestEnum? value = null;
        string invertParameter = "Invert";

        // Act
        var result = _converter.Convert(value, typeof(Visibility), invertParameter, _culture);

        // Assert
        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void Convert_WithInvertParameterCaseInsensitive_ReturnsVisible_WhenValueIsNotInvertString()
    {
        // Arrange
        TestEnum value = TestEnum.OptionA;
        string invertParameter = "invert";

        // Act
        var result = _converter.Convert(value, typeof(Visibility), invertParameter, _culture);

        // Assert
        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void Convert_WithInvertParameterMixedCase_ReturnsVisible_WhenValueIsNotInvertString()
    {
        // Arrange
        TestEnum value = TestEnum.OptionA;
        string invertParameter = "InVeRt";

        // Act
        var result = _converter.Convert(value, typeof(Visibility), invertParameter, _culture);

        // Assert
        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void Convert_WithNonInvertParameter_ReturnsCollapsed_WhenValueDoesNotEqualParameter()
    {
        // Arrange
        TestEnum value = TestEnum.OptionA;
        string otherParameter = "SomeOtherValue";

        // Act
        var result = _converter.Convert(value, typeof(Visibility), otherParameter, _culture);

        // Assert
        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void ConvertBack_AlwaysReturnsBindingDoNothing()
    {
        // Arrange
        Visibility value = Visibility.Visible;
        TestEnum parameter = TestEnum.OptionA;

        // Act
        var result = _converter.ConvertBack(value, typeof(TestEnum), parameter, _culture);

        // Assert
        Assert.Equal(Binding.DoNothing, result);
    }

    private enum TestEnum
    {
        OptionA,
        OptionB,
        OptionC
    }
}
