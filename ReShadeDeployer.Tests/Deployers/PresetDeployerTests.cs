using static ReShadeDeployer.Tests.TestHelpers;
using Testably.Abstractions.Testing;

namespace ReShadeDeployer.Tests.Deployers;

public class PresetDeployerTests
{
    private readonly ExecutableContext _executableContext;
    private readonly MockFileSystem _fileSystem;
    private readonly PresetDeployer _deployer;

    public PresetDeployerTests()
    {
        _executableContext = CreateExecutableContext();
        _fileSystem = CreateMockFileSystem(_executableContext.DirectoryPath);
        _deployer = new PresetDeployer(_fileSystem);
    }

    [Fact]
    public void Deploy_CopiesReShadePresetIniWhenSourceExists()
    {
        // Arrange
        _fileSystem.File.WriteAllText(Paths.ReShadePresetIni, "Preset content");

        // Act
        _deployer.Deploy(_executableContext);

        // Assert
        string expectedPath = @"C:\Games\MyGame\ReShadePreset.ini";
        Assert.True(_fileSystem.File.Exists(expectedPath));
        string content = _fileSystem.File.ReadAllText(expectedPath);
        Assert.Equal("Preset content", content);
    }

    [Fact]
    public void Deploy_DoesNothingWhenSourceDoesNotExist()
    {
        // Act
        _deployer.Deploy(_executableContext);

        // Assert
        string expectedPath = @"C:\Games\MyGame\ReShadePreset.ini";
        Assert.False(_fileSystem.File.Exists(expectedPath));
    }

    [Fact]
    public void Deploy_OverwritesExistingFile()
    {
        // Arrange
        _fileSystem.File.WriteAllText(Paths.ReShadePresetIni, "New preset content");
        
        string existingPresetPath = _fileSystem.Path.Combine(_executableContext.DirectoryPath, "ReShadePreset.ini");
        _fileSystem.File.WriteAllText(existingPresetPath, "Old preset content");

        // Act
        _deployer.Deploy(_executableContext);

        // Assert
        string content = _fileSystem.File.ReadAllText(existingPresetPath);
        Assert.Equal("New preset content", content);
        Assert.DoesNotContain("Old preset content", content);
    }

    [Fact]
    public void GetPath_ReturnsCorrectPath()
    {
        // Act
        string result = _deployer.GetPath(_executableContext);

        // Assert
        Assert.Equal(@"C:\Games\MyGame\ReShadePreset.ini", result);
    }
}

