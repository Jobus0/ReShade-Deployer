using System.Globalization;
using System.Windows.Data;
using ReShadeDeployer.Converters;

namespace ReShadeDeployer.Tests.Converters;

public class EnumToBoolConverterTests
{
    private readonly EnumToBoolConverter _converter = new();
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    [Fact]
    public void Convert_ReturnsTrue_WhenValueEqualsParameter()
    {
        // Arrange
        TestEnum value = TestEnum.OptionA;
        TestEnum parameter = TestEnum.OptionA;

        // Act
        var result = _converter.Convert(value, typeof(bool), parameter, _culture);

        // Assert
        Assert.True((bool)result);
    }

    [Fact]
    public void Convert_ReturnsFalse_WhenValueDoesNotEqualParameter()
    {
        // Arrange
        TestEnum value = TestEnum.OptionA;
        TestEnum parameter = TestEnum.OptionB;

        // Act
        var result = _converter.Convert(value, typeof(bool), parameter, _culture);

        // Assert
        Assert.False((bool)result);
    }

    [Fact]
    public void Convert_ReturnsFalse_WhenValueIsNull()
    {
        // Arrange
        TestEnum? value = null;
        TestEnum parameter = TestEnum.OptionA;

        // Act
        var result = _converter.Convert(value, typeof(bool), parameter, _culture);

        // Assert
        Assert.False((bool)result);
    }

    [Fact]
    public void Convert_ReturnsFalse_WhenParameterIsNull()
    {
        // Arrange
        TestEnum value = TestEnum.OptionA;
        object? parameter = null;

        // Act
        var result = _converter.Convert(value, typeof(bool), parameter, _culture);

        // Assert
        Assert.False((bool)result);
    }

    [Fact]
    public void ConvertBack_ReturnsParameter_WhenValueIsTrue()
    {
        // Arrange
        bool value = true;
        TestEnum parameter = TestEnum.OptionA;

        // Act
        var result = _converter.ConvertBack(value, typeof(TestEnum), parameter, _culture);

        // Assert
        Assert.Equal(parameter, result);
    }

    [Fact]
    public void ConvertBack_ReturnsBindingDoNothing_WhenValueIsFalse()
    {
        // Arrange
        bool value = false;
        TestEnum parameter = TestEnum.OptionA;

        // Act
        var result = _converter.ConvertBack(value, typeof(TestEnum), parameter, _culture);

        // Assert
        Assert.Equal(Binding.DoNothing, result);
    }

    [Fact]
    public void ConvertBack_ReturnsBindingDoNothing_WhenValueIsNotBool()
    {
        // Arrange
        string value = "not a bool";
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
