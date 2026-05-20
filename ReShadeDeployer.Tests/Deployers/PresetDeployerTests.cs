using static ReShadeDeployer.Tests.TestHelpers;

namespace ReShadeDeployer.Tests.Deployers;

public class PresetDeployerTests
{
    [Fact]
    public void Deploy_CopiesReShadePresetIniWhenSourceExists()
    {
        // Arrange
        var executableContext = CreateExecutableContext();
        var fileSystem = CreateMockFileSystem(executableContext.DirectoryPath);
        fileSystem.File.WriteAllText(Paths.ReShadePresetIni, "Preset content");

        var deployer = new PresetDeployer(fileSystem);

        // Act
        deployer.Deploy(executableContext);

        // Assert
        string expectedPath = @"C:\Games\MyGame\ReShadePreset.ini";
        Assert.True(fileSystem.File.Exists(expectedPath));
        string content = fileSystem.File.ReadAllText(expectedPath);
        Assert.Equal("Preset content", content);
    }

    [Fact]
    public void Deploy_DoesNothingWhenSourceDoesNotExist()
    {
        // Arrange
        var executableContext = CreateExecutableContext();
        var fileSystem = CreateMockFileSystem(executableContext.DirectoryPath);

        var deployer = new PresetDeployer(fileSystem);

        // Act
        deployer.Deploy(executableContext);

        // Assert
        string expectedPath = @"C:\Games\MyGame\ReShadePreset.ini";
        Assert.False(fileSystem.File.Exists(expectedPath));
    }

    [Fact]
    public void Deploy_OverwritesExistingFile()
    {
        // Arrange
        var executableContext = CreateExecutableContext();
        var fileSystem = CreateMockFileSystem(executableContext.DirectoryPath);
        fileSystem.File.WriteAllText(Paths.ReShadePresetIni, "New preset content");
        
        string existingPresetPath = fileSystem.Path.Combine(executableContext.DirectoryPath, "ReShadePreset.ini");
        fileSystem.File.WriteAllText(existingPresetPath, "Old preset content");

        var deployer = new PresetDeployer(fileSystem);

        // Act
        deployer.Deploy(executableContext);

        // Assert
        string content = fileSystem.File.ReadAllText(existingPresetPath);
        Assert.Equal("New preset content", content);
        Assert.DoesNotContain("Old preset content", content);
    }

    [Fact]
    public void GetPath_ReturnsCorrectPath()
    {
        // Arrange
        var executableContext = CreateExecutableContext();
        var fileSystem = CreateMockFileSystem(executableContext.DirectoryPath);

        var deployer = new PresetDeployer(fileSystem);

        // Act
        string result = deployer.GetPath(executableContext);

        // Assert
        Assert.Equal(@"C:\Games\MyGame\ReShadePreset.ini", result);
    }
}

