using Testably.Abstractions.Testing;
using static ReShadeDeployer.Tests.TestHelpers;

namespace ReShadeDeployer.Tests.Deployers;

public class AddonsDeployerTests
{
    private readonly ExecutableContext _executableContext;
    private readonly MockFileSystem _fileSystem;
    private readonly FileHelper _fileHelper;
    private readonly AddonsDeployer _deployer;

    public AddonsDeployerTests()
    {
        _executableContext = CreateExecutableContext();
        _fileSystem = CreateMockFileSystem(_executableContext.DirectoryPath);
        _fileHelper = new FileHelper(_fileSystem);
        _deployer = new AddonsDeployer(_fileSystem, _fileHelper);

        // Pre-create standard directories
        _fileSystem.Directory.CreateDirectory(Paths.Root);
        _fileSystem.Directory.CreateDirectory(Paths.Addons);
    }

    [Fact]
    public void GetAvailableAddons_DirectoryDoesNotExist_ReturnsEmpty()
    {
        // Arrange
        _fileSystem.Directory.Delete(Paths.Addons);

        // Act
        var result = _deployer.GetAvailableAddons();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAvailableAddons_TopLevelFiles_DiscoversAndGroupsAddons()
    {
        // Arrange
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(Paths.Addons, "AddonA.addon32"), "binary32");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(Paths.Addons, "AddonA.addon64"), "binary64");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(Paths.Addons, "AddonB.addon64"), "binary64");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(Paths.Addons, "IgnoreMe.txt"), "text");

        // Act
        var result = _deployer.GetAvailableAddons();

        // Assert
        Assert.Equal(2, result.Count);

        var addonA = result[0];
        Assert.Equal("AddonA", addonA.Name);
        Assert.Single(addonA.X32Paths);
        Assert.Contains("AddonA.addon32", addonA.X32Paths[0]);
        Assert.Single(addonA.X64Paths);
        Assert.Contains("AddonA.addon64", addonA.X64Paths[0]);

        var addonB = result[1];
        Assert.Equal("AddonB", addonB.Name);
        Assert.Empty(addonB.X32Paths);
        Assert.Single(addonB.X64Paths);
        Assert.Contains("AddonB.addon64", addonB.X64Paths[0]);
    }

    [Fact]
    public void GetAvailableAddons_FolderBasedAddons_ParsesDirectoriesCorrectly()
    {
        // Arrange
        string addonDir = _fileSystem.Path.Combine(Paths.Addons, "AddonC");
        _fileSystem.Directory.CreateDirectory(addonDir);

        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(addonDir, "AddonC.addon64"), "binary64");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(addonDir, "config.json"), "{}");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(addonDir, "readme.txt"), "text");

        string shadersDir = _fileSystem.Path.Combine(addonDir, "Shaders");
        _fileSystem.Directory.CreateDirectory(shadersDir);
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(shadersDir, "Shader.fx"), "shader content");

        string texturesDir = _fileSystem.Path.Combine(addonDir, "Textures");
        _fileSystem.Directory.CreateDirectory(texturesDir);
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(texturesDir, "Texture.png"), "texture content");

        // Act
        var result = _deployer.GetAvailableAddons();

        // Assert
        Assert.Single(result);
        var addonC = result[0];
        Assert.Equal("AddonC", addonC.Name);
        Assert.Single(addonC.X64Paths);
        Assert.Equal(_fileSystem.Path.Combine(addonDir, "AddonC.addon64"), addonC.X64Paths[0]);
        Assert.Equal(shadersDir, addonC.ShadersPath);
        Assert.Equal(texturesDir, addonC.TexturesPath);

        Assert.NotNull(addonC.AdditionalConfigFiles);
        Assert.Single(addonC.AdditionalConfigFiles);
        Assert.Equal(_fileSystem.Path.Combine(addonDir, "config.json"), addonC.AdditionalConfigFiles[0]);

        Assert.NotNull(addonC.AdditionalFiles);
        Assert.Single(addonC.AdditionalFiles);
        Assert.Equal(_fileSystem.Path.Combine(addonDir, "readme.txt"), addonC.AdditionalFiles[0]);
    }

    [Theory]
    [InlineData("AlwaysCopy", OverrideCopyMode.AlwaysCopy)]
    [InlineData("AlwaysSymlinks", OverrideCopyMode.AlwaysSymlinks)]
    [InlineData("Default", OverrideCopyMode.Default)]
    [InlineData("UnknownValue", OverrideCopyMode.Default)]
    public void GetAvailableAddons_FolderBasedAddonWithConfig_ParsesConfigCorrectly(string overrideCopyText, OverrideCopyMode expectedMode)
    {
        // Arrange
        string addonDir = _fileSystem.Path.Combine(Paths.Addons, "AddonD");
        _fileSystem.Directory.CreateDirectory(addonDir);

        string configContent = $"""
                                SetupFile = setup.bat
                                ReShadeDllNameOverride = MyCustomReShade.dll
                                OverrideCopy = {overrideCopyText}
                                """;
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(addonDir, "Add-on.ini"), configContent);

        // Act
        var result = _deployer.GetAvailableAddons();

        // Assert
        Assert.Single(result);
        var addonD = result[0];
        Assert.Equal("AddonD", addonD.Name);
        Assert.Equal("setup.bat", addonD.SetupFile);
        Assert.Equal("MyCustomReShade.dll", addonD.ReShadeDllNameOverride);
        Assert.Equal(expectedMode, addonD.OverrideCopy);
    }

    [Theory]
    [InlineData(true, "AddonA.addon64")]
    [InlineData(false, "AddonA.addon32")]
    public void Deploy_SymlinksBinaryByArchitecture(bool isX64, string expectedFileName)
    {
        // Arrange
        var context = CreateExecutableContext(isX64: isX64);
        var addon = new AddonItem
        {
            Name = "AddonA"
        };
        addon.X32Paths.Add(@"C:\Addons\AddonA\AddonA.addon32");
        addon.X64Paths.Add(@"C:\Addons\AddonA\AddonA.addon64");

        // Pre-create the source files
        _fileSystem.Directory.CreateDirectory(@"C:\Addons\AddonA");
        _fileSystem.File.WriteAllText(@"C:\Addons\AddonA\AddonA.addon32", "32bit");
        _fileSystem.File.WriteAllText(@"C:\Addons\AddonA\AddonA.addon64", "64bit");

        // Act
        _deployer.Deploy(context, new List<AddonItem> { addon });

        // Assert
        string expectedDest = _fileSystem.Path.Combine(context.DirectoryPath, expectedFileName);
        Assert.True(_fileSystem.File.Exists(expectedDest));
        var target = _fileSystem.File.ResolveLinkTarget(expectedDest, returnFinalTarget: true);
        Assert.Equal(_fileSystem.Path.Combine(@"C:\Addons\AddonA", expectedFileName), target?.FullName);
    }

    [Fact]
    public void Deploy_AlwaysCopy_CopiesBinaryInsteadOfSymlink()
    {
        // Arrange
        var addon = new AddonItem
        {
            Name = "AddonA",
            OverrideCopy = OverrideCopyMode.AlwaysCopy
        };
        addon.X64Paths.Add(@"C:\Addons\AddonA\AddonA.addon64");

        _fileSystem.Directory.CreateDirectory(@"C:\Addons\AddonA");
        _fileSystem.File.WriteAllText(@"C:\Addons\AddonA\AddonA.addon64", "64bit binary content");

        // Act
        _deployer.Deploy(_executableContext, new List<AddonItem> { addon });

        // Assert
        string expectedDest = _fileSystem.Path.Combine(_executableContext.DirectoryPath, "AddonA.addon64");
        Assert.True(_fileSystem.File.Exists(expectedDest));
        
        // Assert it is a real file (not a symlink) and has the copied content
        var target = _fileSystem.File.ResolveLinkTarget(expectedDest, returnFinalTarget: true);
        Assert.Null(target);
        Assert.Equal("64bit binary content", _fileSystem.File.ReadAllText(expectedDest));
    }

    [Fact]
    public void Deploy_AdditionalFiles_CreatesSymlinksAndSkipsFolders()
    {
        // Arrange
        var addon = new AddonItem
        {
            Name = "AddonA",
            AdditionalFiles = new List<string>
            {
                @"C:\Addons\AddonA\somefile.txt",
                @"C:\Addons\AddonA\Shaders",
                @"C:\Addons\AddonA\Textures"
            }
        };

        _fileSystem.Directory.CreateDirectory(@"C:\Addons\AddonA");
        _fileSystem.File.WriteAllText(@"C:\Addons\AddonA\somefile.txt", "file content");
        
        // Setup file attributes for folders in mock filesystem so GetAttributes works
        _fileSystem.Directory.CreateDirectory(@"C:\Addons\AddonA\Shaders");
        _fileSystem.Directory.CreateDirectory(@"C:\Addons\AddonA\Textures");

        // Act
        _deployer.Deploy(_executableContext, new List<AddonItem> { addon });

        // Assert
        string expectedDestFile = _fileSystem.Path.Combine(_executableContext.DirectoryPath, "somefile.txt");
        Assert.True(_fileSystem.File.Exists(expectedDestFile));
        var target = _fileSystem.File.ResolveLinkTarget(expectedDestFile, returnFinalTarget: true);
        Assert.Equal(@"C:\Addons\AddonA\somefile.txt", target?.FullName);

        // Shaders and Textures folders are explicitly skipped in AdditionalFiles loop
        Assert.False(_fileSystem.Directory.Exists(_fileSystem.Path.Combine(_executableContext.DirectoryPath, "Shaders")));
        Assert.False(_fileSystem.Directory.Exists(_fileSystem.Path.Combine(_executableContext.DirectoryPath, "Textures")));
    }

    [Fact]
    public void Deploy_AdditionalFilesConflict_ThrowsIOException()
    {
        // Arrange
        var addon = new AddonItem
        {
            Name = "AddonA",
            AdditionalFiles = new List<string> { @"C:\Addons\AddonA\somefile.txt" }
        };

        _fileSystem.Directory.CreateDirectory(@"C:\Addons\AddonA");
        _fileSystem.File.WriteAllText(@"C:\Addons\AddonA\somefile.txt", "new content");

        string destPath = _fileSystem.Path.Combine(_executableContext.DirectoryPath, "somefile.txt");
        _fileSystem.File.WriteAllText(destPath, "pre-existing content");

        // Act & Assert
        var exception = Assert.Throws<IOException>(() =>
            _deployer.Deploy(_executableContext, new List<AddonItem> { addon }));
        Assert.Contains("already exists in the target game's directory", exception.Message);

        // Verify that if it is already a symlink to the same source path, it skips it without exception
        _fileSystem.File.Delete(destPath);
        _fileSystem.File.CreateSymbolicLink(destPath, @"C:\Addons\AddonA\somefile.txt");

        // Act & Assert (Should not throw now)
        _deployer.Deploy(_executableContext, new List<AddonItem> { addon });
    }

    [Theory]
    [InlineData(OverrideCopyMode.Default, false)]      // Default copy config files
    [InlineData(OverrideCopyMode.AlwaysCopy, false)]   // AlwaysCopy copies config files
    [InlineData(OverrideCopyMode.AlwaysSymlinks, true)] // AlwaysSymlinks symlinks config files
    public void Deploy_AdditionalConfigFiles_HandlesCopyAndSymlinks(OverrideCopyMode overrideCopy, bool expectSymlink)
    {
        // Arrange
        var addon = new AddonItem
        {
            Name = "AddonA",
            AdditionalConfigFiles = new List<string> { @"C:\Addons\AddonA\config.ini" },
            OverrideCopy = overrideCopy
        };

        _fileSystem.Directory.CreateDirectory(@"C:\Addons\AddonA");
        _fileSystem.File.WriteAllText(@"C:\Addons\AddonA\config.ini", "config content");

        // Act
        _deployer.Deploy(_executableContext, new List<AddonItem> { addon });

        // Assert
        string expectedDest = _fileSystem.Path.Combine(_executableContext.DirectoryPath, "config.ini");
        Assert.True(_fileSystem.File.Exists(expectedDest));

        var target = _fileSystem.File.ResolveLinkTarget(expectedDest, returnFinalTarget: true);
        if (expectSymlink)
        {
            Assert.Equal(@"C:\Addons\AddonA\config.ini", target?.FullName);
        }
        else
        {
            Assert.Null(target);
            Assert.Equal("config content", _fileSystem.File.ReadAllText(expectedDest));
        }
    }

    [Fact]
    public void Deploy_AdditionalConfigFiles_DoesNotOverwritePreExisting()
    {
        // Arrange
        var addon = new AddonItem
        {
            Name = "AddonA",
            AdditionalConfigFiles = new List<string> { @"C:\Addons\AddonA\config.ini" }
        };

        _fileSystem.Directory.CreateDirectory(@"C:\Addons\AddonA");
        _fileSystem.File.WriteAllText(@"C:\Addons\AddonA\config.ini", "new config content");

        string destPath = _fileSystem.Path.Combine(_executableContext.DirectoryPath, "config.ini");
        _fileSystem.File.WriteAllText(destPath, "pre-existing config content");

        // Act
        _deployer.Deploy(_executableContext, new List<AddonItem> { addon });

        // Assert
        Assert.Equal("pre-existing config content", _fileSystem.File.ReadAllText(destPath));
    }

    [Fact]
    public void Deploy_SetupFileSpecifiedButNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var addon = new AddonItem
        {
            Name = "AddonA",
            SetupFile = "setup.bat"
        };

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() =>
            _deployer.Deploy(_executableContext, new List<AddonItem> { addon }));
    }
}
