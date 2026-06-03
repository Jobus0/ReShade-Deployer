using Testably.Abstractions.Testing;

namespace ReShadeDeployer.Tests.Deployers;

public class ReShadeUndeployerTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly ReShadeUndeployer _undeployer;
    private readonly string _gameDir;

    public ReShadeUndeployerTests()
    {
        _fileSystem = new MockFileSystem();
        _undeployer = new ReShadeUndeployer(_fileSystem);
        _gameDir = @"C:\Games\TargetGame";

        // Setup common directories
        _fileSystem.Directory.CreateDirectory(_gameDir);
        _fileSystem.Directory.CreateDirectory(Paths.Addons);
    }

    [Fact]
    public void FindReShadeFiles_GameDirectoryDoesNotExist_ReturnsEmpty()
    {
        // Act
        var result = _undeployer.FindReShadeFiles(@"C:\Games\NonExistentDirectory");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindReShadeFiles_ReShadeIniExists_ReturnsIniPath()
    {
        // Arrange
        string iniPath = _fileSystem.Path.Combine(_gameDir, "ReShade.ini");
        _fileSystem.File.WriteAllText(iniPath, "[ReShade]");

        // Act
        var result = _undeployer.FindReShadeFiles(_gameDir);

        // Assert
        Assert.Contains(iniPath, result);
    }

    [Fact]
    public void FindReShadeFiles_DllWithReShadeDescription_ReturnsDllPath()
    {
        // Arrange
        string dllPath = _fileSystem.Path.Combine(_gameDir, "dxgi.dll");
        _fileSystem.File.WriteAllText(dllPath, "mock dll bytes");
        _fileSystem.WithFileVersionInfo(dllPath, v => v.SetFileDescription("crosire's ReShade post-processing injector"));

        // Act
        var result = _undeployer.FindReShadeFiles(_gameDir);

        // Assert
        Assert.Contains(dllPath, result);
    }

    [Fact]
    public void FindReShadeFiles_DllWithoutReShadeDescription_DoesNotReturnDllPath()
    {
        // Arrange
        string dllPath = _fileSystem.Path.Combine(_gameDir, "normal.dll");
        _fileSystem.File.WriteAllText(dllPath, "mock dll bytes");

        // Act
        var result = _undeployer.FindReShadeFiles(_gameDir);

        // Assert
        Assert.DoesNotContain(dllPath, result);
    }

    [Fact]
    public void FindReShadeFiles_SymlinkedAddonPointingToAddonsDir_ReturnsSymlinkPath()
    {
        // Arrange
        string targetAddonFile = _fileSystem.Path.Combine(Paths.Addons, @"someaddon\test.addon64");
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(targetAddonFile)!);
        _fileSystem.File.WriteAllText(targetAddonFile, "addon binary content");

        string symlinkPath = _fileSystem.Path.Combine(_gameDir, "test.addon64");
        _fileSystem.File.CreateSymbolicLink(symlinkPath, targetAddonFile);

        // Act
        var result = _undeployer.FindReShadeFiles(_gameDir);

        // Assert
        Assert.Contains(symlinkPath, result);
    }

    [Fact]
    public void FindReShadeFiles_NonSymlinkedAddons_ReturnsPath()
    {
        // Arrange
        string matchedFileName32 = _fileSystem.Path.Combine(_gameDir, "test.addon32");
        _fileSystem.File.WriteAllText(matchedFileName32, "addon32 content");
        string matchedFileName64 = _fileSystem.Path.Combine(_gameDir, "test.addon64");
        _fileSystem.File.WriteAllText(matchedFileName64, "addon64 content");

        // Act
        var result = _undeployer.FindReShadeFiles(_gameDir);

        // Assert
        Assert.Contains(matchedFileName32, result);
        Assert.Contains(matchedFileName64, result);
    }

    [Fact]
    public void Undeploy_RemovesAllFoundFilesAndDirectories()
    {
        // Arrange
        string iniPath = _fileSystem.Path.Combine(_gameDir, "ReShade.ini");
        _fileSystem.File.WriteAllText(iniPath, "[ReShade]");

        string dllPath = _fileSystem.Path.Combine(_gameDir, "dxgi.dll");
        _fileSystem.File.WriteAllText(dllPath, "mock dll bytes");
        _fileSystem.WithFileVersionInfo(dllPath, v => v.SetFileDescription("crosire's ReShade post-processing injector"));
        
        string addonPath = _fileSystem.Path.Combine(_gameDir, "test.addon64");
        _fileSystem.File.WriteAllText(addonPath, "addon content");

        // Act
        _undeployer.Undeploy(_gameDir);

        // Assert
        Assert.False(_fileSystem.File.Exists(iniPath));
        Assert.False(_fileSystem.File.Exists(dllPath));
        Assert.False(_fileSystem.File.Exists(addonPath));
    }
}
