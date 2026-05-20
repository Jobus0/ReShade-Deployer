using static ReShadeDeployer.Tests.TestHelpers;

namespace ReShadeDeployer.Tests.Deployers;

public class IniDeployerTests
{
    [Fact]
    public void Deploy_CreatesReShadeIniWithCorrectContent()
    {
        // Arrange
        var executableContext = CreateExecutableContext();
        var fileSystem = CreateMockFileSystem(executableContext.DirectoryPath);

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

        var deployer = new IniDeployer(fileSystem);

        // Act
        deployer.Deploy(executableContext, addons);

        // Assert
        string expectedPath = @"C:\Games\MyGame\ReShade.ini";
        Assert.True(fileSystem.File.Exists(expectedPath));
        
        string content = fileSystem.File.ReadAllText(expectedPath);
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
        var executableContext = CreateExecutableContext();
        var fileSystem = CreateMockFileSystem(executableContext.DirectoryPath);

        string existingIniPath = fileSystem.Path.Combine(executableContext.DirectoryPath, "ReShade.ini");
        fileSystem.File.WriteAllText(existingIniPath, "Old content");

        var addons = new List<AddonItem>();
        var deployer = new IniDeployer(fileSystem);

        // Act
        deployer.Deploy(executableContext, addons);

        // Assert
        string content = fileSystem.File.ReadAllText(existingIniPath);
        Assert.DoesNotContain("Old content", content);
    }
}