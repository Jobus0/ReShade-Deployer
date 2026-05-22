using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReShadeDeployer.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfig _config;
    private readonly RegistryHelper _registryHelper;

    [ObservableProperty]
    private bool _isContextMenuRegistered;

    [ObservableProperty]
    private bool _isAlwaysExitOnDeploy;

    public SettingsViewModel(IConfig config, RegistryHelper registryHelper)
    {
        _config = config;
        _registryHelper = registryHelper;

        IsAlwaysExitOnDeploy = _config.AlwaysExitOnDeploy;
        IsContextMenuRegistered = _registryHelper.IsContextMenuActionRegistered("Deploy ReShade");
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
}
