using static ReShadeDeployer.Tests.TestHelpers;
using Testably.Abstractions.Testing;

namespace ReShadeDeployer.Tests.Deployers;

public class IniDeployerTests
{
    private readonly ExecutableContext _executableContext;
    private readonly MockFileSystem _fileSystem;
    private readonly IniDeployer _deployer;

    public IniDeployerTests()
    {
        _executableContext = CreateExecutableContext();
        _fileSystem = CreateMockFileSystem(_executableContext.DirectoryPath);
        _deployer = new IniDeployer(_fileSystem);
    }

    [Fact]
    public void Deploy_CreatesReShadeIniWithCorrectContent()
    {
        // Arrange
        var addons = new List<AddonItem>
        {
            new()
            {
                Name = "TestAddon1",
                ShadersPath = @"C:\Addons\TestAddon1\Shaders",
                TexturesPath = @"C:\Addons\TestAddon1\Textures"
            },
            new()
            {
                Name = "TestAddon2",
                ShadersPath = @"C:\Addons\TestAddon2\Shaders"
            }
        };

        // Act
        _deployer.Deploy(_executableContext, addons);

        // Assert
        string expectedPath = @"C:\Games\MyGame\ReShade.ini";
        Assert.True(_fileSystem.File.Exists(expectedPath));
        
        string content = _fileSystem.File.ReadAllText(expectedPath);
        Assert.Contains("[GENERAL]", content);
        Assert.Contains($@"EffectSearchPaths={Paths.Shaders}\**,C:\Addons\TestAddon1\Shaders\**,C:\Addons\TestAddon2\Shaders\**", content);
        Assert.Contains($@"TextureSearchPaths={Paths.Textures}\**,C:\Addons\TestAddon1\Textures\**", content);
        Assert.Contains("RESHADE_DEPTH_INPUT_IS_REVERSED=0", content);
        Assert.Contains("[OVERLAY]", content);
        Assert.Contains("TutorialProgress=4", content);
        Assert.Contains("[DEPTH]", content);
        Assert.Contains("DepthCopyBeforeClears=0", content);
        Assert.Contains("DepthCopyAtClearIndex=0", content);
        Assert.Contains("DrawStatsHeuristic=0", content);
        Assert.Contains("UseAspectRatioHeuristics=1", content);
    }

    [Fact]
    public void Deploy_DeletesExistingReShadeIniBeforeCreating()
    {
        // Arrange
        string existingIniPath = _fileSystem.Path.Combine(_executableContext.DirectoryPath, "ReShade.ini");
        _fileSystem.File.WriteAllText(existingIniPath, "Old content");

        var addons = new List<AddonItem>();

        // Act
        _deployer.Deploy(_executableContext, addons);

        // Assert
        string content = _fileSystem.File.ReadAllText(existingIniPath);
        Assert.DoesNotContain("Old content", content);
    }

    [Fact]
    public void Deploy_WithPresetPath_AddsPresetPathToIni()
    {
        // Arrange
        var addons = new List<AddonItem>();
        string presetPath = @"C:\ReShade Deployer\Presets\MyGame\ReShadePreset.ini";

        // Act
        _deployer.Deploy(_executableContext, addons, presetPath);

        // Assert
        string expectedPath = @"C:\Games\MyGame\ReShade.ini";
        Assert.True(_fileSystem.File.Exists(expectedPath));
        
        string content = _fileSystem.File.ReadAllText(expectedPath);
        Assert.Contains($@"PresetPath={presetPath}", content);
    }
}