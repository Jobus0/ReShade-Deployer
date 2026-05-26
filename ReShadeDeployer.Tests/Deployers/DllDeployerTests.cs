using Testably.Abstractions.Testing;
using static ReShadeDeployer.Tests.TestHelpers;

namespace ReShadeDeployer.Tests.Deployers;

public class DllDeployerTests
{
    private readonly ExecutableContext _executableContext;
    private readonly MockFileSystem _fileSystem;
    private readonly DllDeployer _deployer;

    public DllDeployerTests()
    {
        _executableContext = CreateExecutableContext();
        _fileSystem = CreateMockFileSystem(_executableContext.DirectoryPath);
        var mockRegistry = new MockRegistry();
        var vulkanDeployer = new VulkanSystemWideDeployer(_fileSystem, mockRegistry);
        _deployer = new DllDeployer(_fileSystem, vulkanDeployer);

        // Pre-create standard directories used in Paths
        _fileSystem.Directory.CreateDirectory(Paths.Root);
        _fileSystem.Directory.CreateDirectory(Paths.Dlls);
        _fileSystem.Directory.CreateDirectory(Paths.AddonDlls);
        _fileSystem.Directory.CreateDirectory(Paths.CommonPathLocal);

        // Pre-create some dummy source ReShade DLLs to simulate they exist in the installation
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(Paths.Dlls, "ReShade32.dll"), "ReShade 32bit DLL");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(Paths.Dlls, "ReShade64.dll"), "ReShade 64bit DLL");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(Paths.AddonDlls, "ReShade32.dll"), "ReShade 32bit Addon DLL");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(Paths.AddonDlls, "ReShade64.dll"), "ReShade 64bit Addon DLL");
    }

    [Theory]
    [InlineData(false)] // normal DLL
    [InlineData(true)]  // addon support DLL
    public void Deploy_VulkanApi_DelegatesToVulkanSystemWideDeployerWithCorrectDllPath(bool addonSupport)
    {
        // Act
        _deployer.Deploy(_executableContext, GraphicsApi.Vulkan, addonSupport, new List<AddonItem>());

        // Assert - verify files are deployed to the common local path using the correct source directory
        string expectedSourceDir = addonSupport ? Paths.AddonDlls : Paths.Dlls;

        string expected64Dll = _fileSystem.Path.Combine(Paths.CommonPathLocal, "ReShade64.dll");
        Assert.True(_fileSystem.File.Exists(expected64Dll));
        var target64 = _fileSystem.File.ResolveLinkTarget(expected64Dll, returnFinalTarget: true);
        Assert.Equal(_fileSystem.Path.Combine(expectedSourceDir, "ReShade64.dll"), target64?.FullName);

        string expected32Dll = _fileSystem.Path.Combine(Paths.CommonPathLocal, "ReShade32.dll");
        Assert.True(_fileSystem.File.Exists(expected32Dll));
        var target32 = _fileSystem.File.ResolveLinkTarget(expected32Dll, returnFinalTarget: true);
        Assert.Equal(_fileSystem.Path.Combine(expectedSourceDir, "ReShade32.dll"), target32?.FullName);
    }

    [Fact]
    public void Deploy_MultipleAddonsWithDllNameOverride_ThrowsInvalidOperationException()
    {
        // Arrange
        var addons = new List<AddonItem>
        {
            new() { Name = "Addon1", ReShadeDllNameOverride = "Override1.dll" },
            new() { Name = "Addon2", ReShadeDllNameOverride = "Override2.dll" }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _deployer.Deploy(_executableContext, GraphicsApi.DXGI, addonSupport: false, addons));

        Assert.Contains("Multiple addons specify a ReShadeDllNameOverride", exception.Message);
        Assert.Contains("'Addon1'", exception.Message);
        Assert.Contains("'Addon2'", exception.Message);
    }

    [Theory]
    [InlineData(true, false)]  // 64-bit, normal DLL
    [InlineData(true, true)]   // 64-bit, addon support DLL
    [InlineData(false, false)] // 32-bit, normal DLL
    [InlineData(false, true)]  // 32-bit, addon support DLL
    public void Deploy_SingleAddonWithDllNameOverride_CreatesSymlinkAtOverridePath(bool isX64, bool addonSupport)
    {
        // Arrange
        var context = CreateExecutableContext(isX64: isX64);
        var addons = new List<AddonItem>
        {
            new() { Name = "AddonWithOverride", ReShadeDllNameOverride = @"CustomSubdir\override.dll" }
        };

        // Act
        _deployer.Deploy(context, GraphicsApi.DXGI, addonSupport: addonSupport, addons);

        // Assert
        string expectedSymlinkPath = _fileSystem.Path.Combine(context.DirectoryPath, @"CustomSubdir\override.dll");
        Assert.True(_fileSystem.File.Exists(expectedSymlinkPath));

        var target = _fileSystem.File.ResolveLinkTarget(expectedSymlinkPath, returnFinalTarget: true);
        string expectedSourceDir = addonSupport ? Paths.AddonDlls : Paths.Dlls;
        string expectedSourceDll = isX64 ? "ReShade64.dll" : "ReShade32.dll";
        Assert.Equal(_fileSystem.Path.Combine(expectedSourceDir, expectedSourceDll), target?.FullName);
    }

    [Theory]
    [InlineData(GraphicsApi.D3D9, "d3d9.dll", true, false)]
    [InlineData(GraphicsApi.DXGI, "dxgi.dll", true, true)]
    [InlineData(GraphicsApi.OpenGL, "opengl32.dll", false, false)]
    public void Deploy_NoDllNameOverride_CreatesSymlinkForApi(GraphicsApi api, string expectedDllName, bool isX64, bool addonSupport)
    {
        // Arrange
        var context = CreateExecutableContext(isX64: isX64);
        var addons = new List<AddonItem>();

        // Act
        _deployer.Deploy(context, api, addonSupport: addonSupport, addons);

        // Assert
        string expectedSymlinkPath = _fileSystem.Path.Combine(context.DirectoryPath, expectedDllName);
        Assert.True(_fileSystem.File.Exists(expectedSymlinkPath));

        var target = _fileSystem.File.ResolveLinkTarget(expectedSymlinkPath, returnFinalTarget: true);
        string expectedSourceDir = addonSupport ? Paths.AddonDlls : Paths.Dlls;
        string expectedSourceDll = isX64 ? "ReShade64.dll" : "ReShade32.dll";
        Assert.Equal(_fileSystem.Path.Combine(expectedSourceDir, expectedSourceDll), target?.FullName);
    }

    [Fact]
    public void Deploy_ExistingFileIsReShade_DeletesExistingFileWithoutBackup()
    {
        // Arrange
        string symlinkPath = _fileSystem.Path.Combine(_executableContext.DirectoryPath, "dxgi.dll");
        _fileSystem.File.WriteAllText(symlinkPath, "old content");
        _fileSystem.WithFileVersionInfo(symlinkPath, v => v.SetFileDescription("crosire's ReShade post-processing injector for 64-bit"));

        var addons = new List<AddonItem>();

        // Act
        _deployer.Deploy(_executableContext, GraphicsApi.DXGI, addonSupport: false, addons);

        // Assert
        Assert.True(_fileSystem.File.Exists(symlinkPath));
        var target = _fileSystem.File.ResolveLinkTarget(symlinkPath, returnFinalTarget: true);
        Assert.Equal(_fileSystem.Path.Combine(Paths.Dlls, "ReShade64.dll"), target?.FullName);

        string backupPath = symlinkPath + ".old";
        Assert.False(_fileSystem.File.Exists(backupPath));
    }

    [Fact]
    public void Deploy_ExistingFileIsNotReShade_CreatesBackup()
    {
        // Arrange
        string symlinkPath = _fileSystem.Path.Combine(_executableContext.DirectoryPath, "dxgi.dll");
        _fileSystem.File.WriteAllText(symlinkPath, "original dll content");

        var addons = new List<AddonItem>();

        // Act
        _deployer.Deploy(_executableContext, GraphicsApi.DXGI, addonSupport: false, addons);

        // Assert symlink is created
        Assert.True(_fileSystem.File.Exists(symlinkPath));
        var target = _fileSystem.File.ResolveLinkTarget(symlinkPath, returnFinalTarget: true);
        Assert.Equal(_fileSystem.Path.Combine(Paths.Dlls, "ReShade64.dll"), target?.FullName);

        // Assert backup file is created with original content
        string backupPath = symlinkPath + ".old";
        Assert.True(_fileSystem.File.Exists(backupPath));
        Assert.Equal("original dll content", _fileSystem.File.ReadAllText(backupPath));
    }

    [Fact]
    public void Deploy_ExistingFileIsNotReShadeAndBackupExists_CreatesBackupWithNumericSuffix()
    {
        // Arrange
        string symlinkPath = _fileSystem.Path.Combine(_executableContext.DirectoryPath, "dxgi.dll");
        _fileSystem.File.WriteAllText(symlinkPath, "current dll content");

        // Pre-create existing backups
        string backupPath0 = symlinkPath + ".old";
        _fileSystem.File.WriteAllText(backupPath0, "old content 0");

        var addons = new List<AddonItem>();

        // Act
        _deployer.Deploy(_executableContext, GraphicsApi.DXGI, addonSupport: false, addons);

        // Assert
        Assert.True(_fileSystem.File.Exists(symlinkPath));
        var target = _fileSystem.File.ResolveLinkTarget(symlinkPath, returnFinalTarget: true);
        Assert.Equal(_fileSystem.Path.Combine(Paths.Dlls, "ReShade64.dll"), target?.FullName);

        // Verify old backup is preserved
        Assert.True(_fileSystem.File.Exists(backupPath0));
        Assert.Equal("old content 0", _fileSystem.File.ReadAllText(backupPath0));

        // Verify new backup has suffix "1"
        string backupPath1 = symlinkPath + ".old1";
        Assert.True(_fileSystem.File.Exists(backupPath1));
        Assert.Equal("current dll content", _fileSystem.File.ReadAllText(backupPath1));
    }
}
