using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReShadeDeployer.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfig _config;

    [ObservableProperty]
    private bool _isContextMenuRegistered;

    [ObservableProperty]
    private bool _isAlwaysExitOnDeploy;

    public SettingsViewModel(IConfig config)
    {
        _config = config;

        IsAlwaysExitOnDeploy = _config.AlwaysExitOnDeploy;
        IsContextMenuRegistered = RegistryHelper.IsContextMenuActionRegistered("Deploy ReShade");
    }

    partial void OnIsContextMenuRegisteredChanged(bool value)
    {
        if (value)
            RegistryHelper.RegisterContextMenuAction("Deploy ReShade",
                Environment.ProcessPath! + " \"%1\"",
                Environment.ProcessPath! + ",0");
        else
            RegistryHelper.UnregisterContextMenuAction("Deploy ReShade");
    }

    partial void OnIsAlwaysExitOnDeployChanged(bool value)
    {
        _config.AlwaysExitOnDeploy = value;
    }
}
