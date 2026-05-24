using Microsoft.Win32;
using Testably.Abstractions.Testing;

namespace ReShadeDeployer.Tests.Deployers;

public class VulkanSystemWideDeployerTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly MockRegistry _mockRegistry;
    private readonly VulkanSystemWideDeployer _deployer;

    public VulkanSystemWideDeployerTests()
    {
        _fileSystem = new MockFileSystem();
        _mockRegistry = new MockRegistry();
        _deployer = new VulkanSystemWideDeployer(_fileSystem, _mockRegistry);

        // Pre-create standard directories used in Paths
        _fileSystem.Directory.CreateDirectory(Paths.Root);
        _fileSystem.Directory.CreateDirectory(Paths.CommonPathLocal);
        _fileSystem.Directory.CreateDirectory(Paths.AddonDlls);
    }

    [Fact]
    public void DeployVulkanGlobally_CreatesSymlinksAndRegistryKeys()
    {
        // Arrange
        string sourceDllPath = _fileSystem.Path.Combine(Paths.Root, "SourceDlls");
        _fileSystem.Directory.CreateDirectory(sourceDllPath);
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(sourceDllPath, "ReShade64.dll"), "64bit dll");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(sourceDllPath, "ReShade64.json"), "64bit json");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(sourceDllPath, "ReShade32.dll"), "32bit dll");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(sourceDllPath, "ReShade32.json"), "32bit json");

        // Act
        _deployer.DeployVulkanGlobally(sourceDllPath);

        // Assert files are created/symlinked
        string expected64Dll = _fileSystem.Path.Combine(Paths.CommonPathLocal, "ReShade64.dll");
        string expected64Json = _fileSystem.Path.Combine(Paths.CommonPathLocal, "ReShade64.json");
        string expected32Dll = _fileSystem.Path.Combine(Paths.CommonPathLocal, "ReShade32.dll");
        string expected32Json = _fileSystem.Path.Combine(Paths.CommonPathLocal, "ReShade32.json");

        Assert.True(_fileSystem.File.Exists(expected64Dll));
        Assert.True(_fileSystem.File.Exists(expected64Json));
        Assert.True(_fileSystem.File.Exists(expected32Dll));
        Assert.True(_fileSystem.File.Exists(expected32Json));

        // Assert registry keys are set
        string key64Path = @"Software\Khronos\Vulkan\ImplicitLayers";
        string key32Path = Environment.Is64BitOperatingSystem
            ? @"Software\Wow6432Node\Khronos\Vulkan\ImplicitLayers"
            : @"Software\Khronos\Vulkan\ImplicitLayers";

        var key64 = _mockRegistry.LocalMachine.OpenSubKey(key64Path) as MockRegistryKey;
        Assert.NotNull(key64);
        Assert.Equal(0, key64.GetValue(expected64Json));

        var key32 = _mockRegistry.LocalMachine.OpenSubKey(key32Path) as MockRegistryKey;
        Assert.NotNull(key32);
        Assert.Equal(0, key32.GetValue(expected32Json));
    }

    [Fact]
    public void RemoveVulkanGlobally_DeletesSymlinksAndRegistryValues()
    {
        // Arrange
        // Create files first
        string dll64Path = _fileSystem.Path.Combine(Paths.CommonPathLocal, "ReShade64.dll");
        string json64Path = _fileSystem.Path.Combine(Paths.CommonPathLocal, "ReShade64.json");
        string dll32Path = _fileSystem.Path.Combine(Paths.CommonPathLocal, "ReShade32.dll");
        string json32Path = _fileSystem.Path.Combine(Paths.CommonPathLocal, "ReShade32.json");

        _fileSystem.File.WriteAllText(dll64Path, "data");
        _fileSystem.File.WriteAllText(json64Path, "data");
        _fileSystem.File.WriteAllText(dll32Path, "data");
        _fileSystem.File.WriteAllText(json32Path, "data");

        // Set registry values
        string key64Path = @"Software\Khronos\Vulkan\ImplicitLayers";
        string key32Path = Environment.Is64BitOperatingSystem
            ? @"Software\Wow6432Node\Khronos\Vulkan\ImplicitLayers"
            : @"Software\Khronos\Vulkan\ImplicitLayers";

        _mockRegistry.LocalMachine.CreateSubKey(key64Path).SetValue(json64Path, 0, RegistryValueKind.DWord);
        _mockRegistry.LocalMachine.CreateSubKey(key32Path).SetValue(json32Path, 0, RegistryValueKind.DWord);

        // Act
        _deployer.RemoveVulkanGlobally();

        // Assert files are deleted
        Assert.False(_fileSystem.File.Exists(dll64Path));
        Assert.False(_fileSystem.File.Exists(json64Path));
        Assert.False(_fileSystem.File.Exists(dll32Path));
        Assert.False(_fileSystem.File.Exists(json32Path));

        // Assert registry values are deleted
        var key64 = _mockRegistry.LocalMachine.OpenSubKey(key64Path) as MockRegistryKey;
        Assert.Null(key64?.GetValue(json64Path));

        var key32 = _mockRegistry.LocalMachine.OpenSubKey(key32Path) as MockRegistryKey;
        Assert.Null(key32?.GetValue(json32Path));
    }

    [Fact]
    public void IsVulkanDeployedWithAddonSupport_ReturnsTrue_WhenRegistryMatchesAddonDllPath()
    {
        // Arrange
        string registryValueName = _fileSystem.Path.Combine(Paths.CommonPathLocal, "ReShade32.json");
        string targetPath = _fileSystem.Path.Combine(Paths.AddonDlls, "ReShade32.json");

        // Write targets
        _fileSystem.File.WriteAllText(targetPath, "addon support config");
        _fileSystem.File.CreateSymbolicLink(registryValueName, targetPath);

        string key32Path = Environment.Is64BitOperatingSystem
            ? @"Software\Wow6432Node\Khronos\Vulkan\ImplicitLayers"
            : @"Software\Khronos\Vulkan\ImplicitLayers";

        _mockRegistry.LocalMachine.CreateSubKey(key32Path).SetValue(registryValueName, 0, RegistryValueKind.DWord);

        // Act
        bool isDeployed = _deployer.IsVulkanDeployedWithAddonSupport();

        // Assert
        Assert.True(isDeployed);
    }
}
