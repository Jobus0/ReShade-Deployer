using Testably.Abstractions.Testing;

namespace ReShadeDeployer.Tests.Config;

public class IniFileConfigTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly IniFileConfig _config;

    public IniFileConfigTests()
    {
        _fileSystem = new MockFileSystem();
        _fileSystem.Directory.CreateDirectory(AppContext.BaseDirectory);
        _config = new IniFileConfig(_fileSystem);
    }

    [Fact]
    public void LatestReShadeVersionNumber_GetSet_WritesToIni()
    {
        // Act
        _config.LatestReShadeVersionNumber = "5.0.0";
        
        // Assert
        Assert.Equal("5.0.0", _config.LatestReShadeVersionNumber);
    }

    [Fact]
    public void LatestReShadeVersionNumber_Get_ReturnsEmptyWhenNotSet()
    {
        // Assert
        Assert.Equal(string.Empty, _config.LatestReShadeVersionNumber);
    }

    [Fact]
    public void LatestReShadeVersionNumberCheckDate_GetSet_WritesToIni()
    {
        // Arrange
        var testDate = new DateTime(2024, 1, 15);
        
        // Act
        _config.LatestReShadeVersionNumberCheckDate = testDate;
        
        // Assert
        Assert.Equal(testDate, _config.LatestReShadeVersionNumberCheckDate);
    }

    [Fact]
    public void LatestReShadeVersionNumberCheckDate_Get_ReturnsDefaultWhenNotSet()
    {
        // Assert
        Assert.Equal(default, _config.LatestReShadeVersionNumberCheckDate);
    }

    [Fact]
    public void LatestDeployerVersionNumber_GetSet_WritesToIni()
    {
        // Act
        _config.LatestDeployerVersionNumber = "2.1.0";
        
        // Assert
        Assert.Equal("2.1.0", _config.LatestDeployerVersionNumber);
    }

    [Fact]
    public void LatestDeployerVersionNumberCheckDate_GetSet_WritesToIni()
    {
        // Arrange
        var testDate = new DateTime(2024, 2, 20);
        
        // Act
        _config.LatestDeployerVersionNumberCheckDate = testDate;
        
        // Assert
        Assert.Equal(testDate, _config.LatestDeployerVersionNumberCheckDate);
    }

    [Fact]
    public void AlwaysExitOnDeploy_GetSet_WritesToIni()
    {
        // Act
        _config.AlwaysExitOnDeploy = true;
        
        // Assert
        Assert.True(_config.AlwaysExitOnDeploy);
    }

    [Fact]
    public void AlwaysExitOnDeploy_Get_ReturnsFalseWhenNotSet()
    {
        // Assert
        Assert.False(_config.AlwaysExitOnDeploy);
    }

    [Fact]
    public void UseCentralizedPresets_GetSet_WritesToIni()
    {
        // Act
        _config.CentralizedPresets = true;
        
        // Assert
        Assert.True(_config.CentralizedPresets);
    }

    [Fact]
    public void LastSelectedAddons_GetSet_WritesToIni()
    {
        // Arrange
        var addons = new[] { "addon1", "addon2", "addon3" };
        
        // Act
        _config.LastSelectedAddons = addons;
        
        // Assert
        Assert.Equal(addons, _config.LastSelectedAddons);
    }

    [Fact]
    public void LastSelectedAddons_Get_ReturnsEmptyArrayWhenNotSet()
    {
        // Assert
        Assert.Empty(_config.LastSelectedAddons);
    }

    [Fact]
    public async Task MultipleRapidWrites_AllValuesPersist()
    {
        // Act
        _config.LatestReShadeVersionNumber = "1.0.0";
        _config.LatestDeployerVersionNumber = "2.0.0";
        _config.AlwaysExitOnDeploy = true;
        
        // Wait for deferred save to complete
        await Task.Delay(200);
        
        // Assert
        Assert.Equal("1.0.0", _config.LatestReShadeVersionNumber);
        Assert.Equal("2.0.0", _config.LatestDeployerVersionNumber);
        Assert.True(_config.AlwaysExitOnDeploy);
    }

    [Fact]
    public async Task ConfigIniNotExists_CreatesNewFileOnFirstWrite()
    {
        // Arrange
        var configPath = Paths.ConfigIni;
        Assert.False(_fileSystem.File.Exists(configPath));
        
        // Act
        _config.LatestReShadeVersionNumber = "1.0.0";
        
        // Wait for deferred save to complete
        await Task.Delay(200);
        
        // Assert
        Assert.True(_fileSystem.File.Exists(configPath));
    }

    [Fact]
    public void ConfigIniExists_ReadsExistingValues()
    {
        // Arrange
        var configPath = Paths.ConfigIni;
        var libPath = Path.Combine(AppContext.BaseDirectory, "lib");
        _fileSystem.Directory.CreateDirectory(libPath);
        _fileSystem.File.WriteAllText(configPath, "LatestReShadeVersionNumber=3.0.0\n");
        
        // Create a new config instance to read the existing file
        var configWithExistingFile = new IniFileConfig(_fileSystem);
        
        // Act
        var version = configWithExistingFile.LatestReShadeVersionNumber;
        
        // Assert
        Assert.Equal("3.0.0", version);
    }
}
