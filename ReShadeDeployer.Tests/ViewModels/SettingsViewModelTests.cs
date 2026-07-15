using Testably.Abstractions.Testing;
using ReShadeDeployer.ViewModels;
using Wpf.Ui.Controls;

namespace ReShadeDeployer.Tests.ViewModels;

public class SettingsViewModelTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly MockRegistry _registry;
    private readonly RegistryHelper _registryHelper;
    private readonly MockMessageBox _messageBox;
    private readonly MockConfig _config;
    private readonly VulkanSystemWideDeployer _vulkanDeployer;

    public SettingsViewModelTests()
    {
        _fileSystem = new MockFileSystem();
        _registry = new MockRegistry();
        _registryHelper = new RegistryHelper(_registry);
        _messageBox = new MockMessageBox();
        _config = new MockConfig();
        _vulkanDeployer = new VulkanSystemWideDeployer(_fileSystem, _registry);
    }

    [Fact]
    public void Constructor_InitializesPropertiesFromConfigAndRegistry()
    {
        // Arrange
        _config.AlwaysExitOnDeploy = true;
        
        // Register context menu to simulate registered state
        _registryHelper.RegisterContextMenuAction("Deploy ReShade", "command");

        // Act
        var viewModel = new SettingsViewModel(_config, _registryHelper, _messageBox, _vulkanDeployer);

        // Assert
        Assert.True(viewModel.IsAlwaysExitOnDeploy);
        Assert.True(viewModel.IsContextMenuRegistered);
    }

    [Fact]
    public void SettingIsAlwaysExitOnDeploy_UpdatesConfigValue()
    {
        // Arrange
        _config.AlwaysExitOnDeploy = false;
        var viewModel = new SettingsViewModel(_config, _registryHelper, _messageBox, _vulkanDeployer);

        // Act
        viewModel.IsAlwaysExitOnDeploy = true;

        // Assert
        Assert.True(_config.AlwaysExitOnDeploy);
    }

    [Fact]
    public void SettingIsContextMenuRegistered_RegistersOrUnregisters_InRegistry()
    {
        // Arrange
        var viewModel = new SettingsViewModel(_config, _registryHelper, _messageBox, _vulkanDeployer);
        Assert.False(_registryHelper.IsContextMenuActionRegistered("Deploy ReShade"));

        // Act - Register
        viewModel.IsContextMenuRegistered = true;

        // Assert
        Assert.True(_registryHelper.IsContextMenuActionRegistered("Deploy ReShade"));

        // Act - Unregister
        viewModel.IsContextMenuRegistered = false;

        // Assert
        Assert.False(_registryHelper.IsContextMenuActionRegistered("Deploy ReShade"));
    }

    [Fact]
    public void ShowAboutCommand_ShowsMessageBoxWithVersion()
    {
        // Arrange
        var viewModel = new SettingsViewModel(_config, _registryHelper, _messageBox, _vulkanDeployer);

        // Act
        viewModel.ShowAboutCommand.Execute(null);

        // Assert
        Assert.Single(_messageBox.ShowCalls);
        Assert.Contains(UIStrings.About, _messageBox.ShowCalls[0].Title);
    }

    [Fact]
    public void UninstallCommand_UserCancels_DoesNotUninstall()
    {
        // Arrange
        _messageBox.NextResults.Enqueue(MessageBoxResult.Secondary); // User Cancel
        _registryHelper.RegisterContextMenuAction("Deploy ReShade", "command");
        
        var viewModel = new SettingsViewModel(_config, _registryHelper, _messageBox, _vulkanDeployer);

        // Act
        viewModel.UninstallCommand.Execute(null);

        // Assert
        // Prompt shown
        Assert.Single(_messageBox.ShowCalls);
        Assert.Contains(UIStrings.UninstallDeployer_Content, _messageBox.ShowCalls[0].ContentText);
        // Action not taken (context menu still exists)
        Assert.True(_registryHelper.IsContextMenuActionRegistered("Deploy ReShade"));
    }

    [Fact]
    public void UninstallCommand_UserConfirms_UnregistersContextMenuAndRemovesVulkan()
    {
        // Arrange
        _messageBox.NextResults.Enqueue(MessageBoxResult.Primary); // User confirms
        _registryHelper.RegisterContextMenuAction("Deploy ReShade", "command");

        // Write dummy files for Vulkan so we can verify they get cleaned up
        _fileSystem.Directory.CreateDirectory(Paths.CommonPathLocal);
        string sym64 = _fileSystem.Path.Combine(Paths.CommonPathLocal, "ReShade64.dll");
        _fileSystem.File.WriteAllText(sym64, "vulkan dll");
        
        var viewModel = new SettingsViewModel(_config, _registryHelper, _messageBox, _vulkanDeployer);

        // Act
        viewModel.UninstallCommand.Execute(null);

        // Assert
        // Context menu is removed
        Assert.False(_registryHelper.IsContextMenuActionRegistered("Deploy ReShade"));
        // Vulkan files cleaned up
        Assert.False(_fileSystem.File.Exists(sym64));
        // Success dialog shown
        Assert.Contains(_messageBox.ShowCalls, call => call.Title == UIStrings.Success);
    }
}
