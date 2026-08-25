using System;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReShadeDeployer.Registry;
using Wpf.Ui.Controls;

namespace ReShadeDeployer.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfig _config;
    private readonly RegistryHelper _registryHelper;
    private readonly IMessageBox _messageBox;
    private readonly VulkanSystemWideDeployer _vulkanSystemWideDeployer;

    public string? AssemblyVersion { get; }

    [ObservableProperty]
    private bool _isContextMenuRegistered;

    [ObservableProperty]
    private bool _isAlwaysExitOnDeploy;

    [ObservableProperty]
    private bool _centralizedPresets;

    public SettingsViewModel(IConfig config, RegistryHelper registryHelper, IMessageBox messageBox, VulkanSystemWideDeployer vulkanSystemWideDeployer)
    {
        _config = config;
        _registryHelper = registryHelper;
        _messageBox = messageBox;
        _vulkanSystemWideDeployer = vulkanSystemWideDeployer;

        IsAlwaysExitOnDeploy = _config.AlwaysExitOnDeploy;
        IsContextMenuRegistered = _registryHelper.IsContextMenuActionRegistered("Deploy ReShade");
        CentralizedPresets = _config.CentralizedPresets;
        AssemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3);
    }

    partial void OnIsContextMenuRegisteredChanged(bool value)
    {
        // Only register/unregister if the value is different from the current registry state
        bool currentRegistryState = _registryHelper.IsContextMenuActionRegistered("Deploy ReShade");
        
        if (value != currentRegistryState)
        {
            if (value)
                _registryHelper.RegisterContextMenuAction("Deploy ReShade",
                    Environment.ProcessPath! + " \"%1\"",
                    Environment.ProcessPath! + ",0");
            else
                _registryHelper.UnregisterContextMenuAction("Deploy ReShade");
        }
    }

    partial void OnIsAlwaysExitOnDeployChanged(bool value)
    {
        _config.AlwaysExitOnDeploy = value;
    }

    partial void OnCentralizedPresetsChanged(bool value)
    {
        _config.CentralizedPresets = value;
    }

    [RelayCommand]
    private async Task ShowAbout()
    {
        await _messageBox.Show(UIStrings.About, string.Format(UIStrings.About_Info, AssemblyVersion));
    }

    [RelayCommand]
    private async Task Uninstall()
    {
        var result = await _messageBox.Show(UIStrings.UninstallDeployer,
            UIStrings.UninstallDeployer_Content,
            primaryButtonText: UIStrings.UninstallDeployer, ControlAppearance.Caution,
            closeButtonText: UIStrings.Cancel);

        if (result == MessageBoxResult.Primary)
        {
            if (_registryHelper.IsContextMenuActionRegistered("Deploy ReShade"))
                _registryHelper.UnregisterContextMenuAction("Deploy ReShade");

            _vulkanSystemWideDeployer.RemoveVulkanGlobally();
            
            await _messageBox.Show(UIStrings.Success, UIStrings.UninstallDeployer_Success);
        }
    }
}