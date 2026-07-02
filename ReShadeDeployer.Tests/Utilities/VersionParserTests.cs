namespace ReShadeDeployer.Tests.Utilities;

public class VersionParserTests
{
    [Fact]
    public void Parse_ExtractsVersionFromUrl()
    {
        // Arrange
        string url = "https://reshade.me/downloads/ReShade_Setup_5.9.2.exe";

        // Act
        string result = VersionParser.Parse(url);

        // Assert
        Assert.Equal("5.9.2", result);
    }

    [Fact]
    public void Parse_ExtractsVersionFromSimpleString()
    {
        // Arrange
        string text = "Version 1.2.3";

        // Act
        string result = VersionParser.Parse(text);

        // Assert
        Assert.Equal("1.2.3", result);
    }

    [Fact]
    public void Compare_ReturnsNewer_WhenVersion1IsHigher()
    {
        // Arrange
        string version1 = "2.0.0";
        string version2 = "1.9.9";

        // Act
        var result = VersionParser.Compare(version1, version2);

        // Assert
        Assert.Equal(VersionParser.VersionComparisonResult.Newer, result);
    }

    [Fact]
    public void Compare_ReturnsOlder_WhenVersion1IsLower()
    {
        // Arrange
        string version1 = "1.9.9";
        string version2 = "2.0.0";

        // Act
        var result = VersionParser.Compare(version1, version2);

        // Assert
        Assert.Equal(VersionParser.VersionComparisonResult.Older, result);
    }

    [Fact]
    public void Compare_ReturnsEqual_WhenVersionsAreSame()
    {
        // Arrange
        string version1 = "1.2.3";
        string version2 = "1.2.3";

        // Act
        var result = VersionParser.Compare(version1, version2);

        // Assert
        Assert.Equal(VersionParser.VersionComparisonResult.Equal, result);
    }

    [Fact]
    public void Compare_ReturnsNewer_WhenVersion1HasMoreParts()
    {
        // Arrange
        string version1 = "1.2.3.4";
        string version2 = "1.2.3";

        // Act
        var result = VersionParser.Compare(version1, version2);

        // Assert
        Assert.Equal(VersionParser.VersionComparisonResult.Newer, result);
    }

    [Fact]
    public void Compare_ReturnsOlder_WhenVersion2HasMoreParts()
    {
        // Arrange
        string version1 = "1.2.3";
        string version2 = "1.2.3.4";

        // Act
        var result = VersionParser.Compare(version1, version2);

        // Assert
        Assert.Equal(VersionParser.VersionComparisonResult.Older, result);
    }

    [Fact]
    public void Compare_ReturnsEqual_WhenVersionsHaveDifferentLengthsButSameValue()
    {
        // Arrange
        string version1 = "1.2.0";
        string version2 = "1.2";

        // Act
        var result = VersionParser.Compare(version1, version2);

        // Assert
        Assert.Equal(VersionParser.VersionComparisonResult.Equal, result);
    }

    [Fact]
    public void Compare_ReturnsOlder_WhenVersion1IsEmpty()
    {
        // Arrange
        string version1 = "";
        string version2 = "1.0.0";

        // Act
        var result = VersionParser.Compare(version1, version2);

        // Assert
        Assert.Equal(VersionParser.VersionComparisonResult.Older, result);
    }

    [Fact]
    public void Compare_ReturnsOlder_WhenVersion1IsNull()
    {
        // Arrange
        string version1 = null;
        string version2 = "1.0.0";

        // Act
        var result = VersionParser.Compare(version1, version2);

        // Assert
        Assert.Equal(VersionParser.VersionComparisonResult.Older, result);
    }

    [Fact]
    public void Compare_ReturnsNewer_WhenVersion2IsEmpty()
    {
        // Arrange
        string version1 = "1.0.0";
        string version2 = "";

        // Act
        var result = VersionParser.Compare(version1, version2);

        // Assert
        Assert.Equal(VersionParser.VersionComparisonResult.Newer, result);
    }

    [Fact]
    public void Compare_ReturnsNewer_WhenVersion2IsNull()
    {
        // Arrange
        string version1 = "1.0.0";
        string version2 = null;

        // Act
        var result = VersionParser.Compare(version1, version2);

        // Assert
        Assert.Equal(VersionParser.VersionComparisonResult.Newer, result);
    }

    [Fact]
    public void Compare_ReturnsNewer_WhenMajorVersionIsHigher()
    {
        // Arrange
        string version1 = "2.0.0";
        string version2 = "1.9.9";

        // Act
        var result = VersionParser.Compare(version1, version2);

        // Assert
        Assert.Equal(VersionParser.VersionComparisonResult.Newer, result);
    }

    [Fact]
    public void Compare_ReturnsNewer_WhenMinorVersionIsHigher()
    {
        // Arrange
        string version1 = "1.9.0";
        string version2 = "1.8.9";

        // Act
        var result = VersionParser.Compare(version1, version2);

        // Assert
        Assert.Equal(VersionParser.VersionComparisonResult.Newer, result);
    }

    [Fact]
    public void Compare_ReturnsNewer_WhenPatchVersionIsHigher()
    {
        // Arrange
        string version1 = "1.8.9";
        string version2 = "1.8.8";

        // Act
        var result = VersionParser.Compare(version1, version2);

        // Assert
        Assert.Equal(VersionParser.VersionComparisonResult.Newer, result);
    }

    [Fact]
    public void IsNewerThan_ReturnsTrue_WhenVersion1IsNewer()
    {
        // Arrange
        string version1 = "2.0.0";
        string version2 = "1.0.0";

        // Act
        bool result = VersionParser.IsNewerThan(version1, version2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsNewerThan_ReturnsFalse_WhenVersion1IsOlder()
    {
        // Arrange
        string version1 = "1.0.0";
        string version2 = "2.0.0";

        // Act
        bool result = VersionParser.IsNewerThan(version1, version2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsNewerThan_ReturnsFalse_WhenVersionsAreEqual()
    {
        // Arrange
        string version1 = "1.0.0";
        string version2 = "1.0.0";

        // Act
        bool result = VersionParser.IsNewerThan(version1, version2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsNewerThan_ReturnsFalse_WhenVersion1IsEmpty()
    {
        // Arrange
        string version1 = "";
        string version2 = "1.0.0";

        // Act
        bool result = VersionParser.IsNewerThan(version1, version2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsNewerThan_ReturnsTrue_WhenVersion2IsEmpty()
    {
        // Arrange
        string version1 = "1.0.0";
        string version2 = "";

        // Act
        bool result = VersionParser.IsNewerThan(version1, version2);

        // Assert
        Assert.True(result);
    }
}
