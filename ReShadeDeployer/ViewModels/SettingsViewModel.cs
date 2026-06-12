using System;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Common;

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

    public SettingsViewModel(IConfig config, RegistryHelper registryHelper, IMessageBox messageBox, VulkanSystemWideDeployer vulkanSystemWideDeployer)
    {
        _config = config;
        _registryHelper = registryHelper;
        _messageBox = messageBox;
        _vulkanSystemWideDeployer = vulkanSystemWideDeployer;

        IsAlwaysExitOnDeploy = _config.AlwaysExitOnDeploy;
        IsContextMenuRegistered = _registryHelper.IsContextMenuActionRegistered("Deploy ReShade");
        AssemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3);
    }

    partial void OnIsContextMenuRegisteredChanged(bool value)
    {
        if (value)
            _registryHelper.RegisterContextMenuAction("Deploy ReShade",
                Environment.ProcessPath! + " \"%1\"",
                Environment.ProcessPath! + ",0");
        else
            _registryHelper.UnregisterContextMenuAction("Deploy ReShade");
    }

    partial void OnIsAlwaysExitOnDeployChanged(bool value)
    {
        _config.AlwaysExitOnDeploy = value;
    }

    [RelayCommand]
    private void ShowAbout()
    {
        _messageBox.Show(string.Format(UIStrings.About_Info, AssemblyVersion), UIStrings.About);
    }

    [RelayCommand]
    private void Uninstall()
    {
        var result = _messageBox.Show(
            UIStrings.UninstallDeployer_Content,
            UIStrings.UninstallDeployer,
            UIStrings.UninstallDeployer,
            UIStrings.Cancel,
            ControlAppearance.Caution);

        if (result == IMessageBox.Result.Primary)
        {
            if (_registryHelper.IsContextMenuActionRegistered("Deploy ReShade"))
                _registryHelper.UnregisterContextMenuAction("Deploy ReShade");

            _vulkanSystemWideDeployer.RemoveVulkanGlobally();
            
            _messageBox.Show(UIStrings.UninstallDeployer_Success, UIStrings.Success);
        }
    }
}