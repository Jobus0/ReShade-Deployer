using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace ReShadeDeployer;

/// <summary>
/// Provides methods for deploying ReShade for an executable: DLLs, INI files, and presets.
/// </summary>
public class DeploymentOrchestrator(IFileSystem fileSystem, DllDeployer dllDeployer, VulkanSystemWideDeployer vulkanSystemWideDeployer,
    IniDeployer iniDeployer, PresetDeployer presetDeployer, AddonsDeployer addonsDeployer,
    IMessageBox messageBox, IConfig config, ISnackbarService snackbarService)
{
    /// <summary>
    /// Deploys ReShade for a specified executable.
    /// </summary>
    /// <param name="executableContext">Context for the executable to deploy.</param>
    /// <param name="api">Target API name (dxgi, d3d9, opengl32, vulkan).</param>
    /// <param name="addonSupport">Whether to deploy the addon supported DLL instead of the normal one.</param>
    /// <param name="addons">List of addons to deploy.</param>
    public async Task DeployReShadeForExecutable(ExecutableContext executableContext, GraphicsApi api, bool addonSupport, IList<AddonItem> addons)
    {
        if (api == GraphicsApi.Vulkan && addonSupport && !vulkanSystemWideDeployer.IsVulkanDeployedWithAddonSupport())
        {
            var result = await messageBox.Show(UIStrings.Warning,
                UIStrings.Vulkan_Addon_Warning,
                primaryButtonText: UIStrings.Continue, ControlAppearance.Caution,
                closeButtonText: UIStrings.Cancel);
            
            if (result != MessageBoxResult.Primary)
                return;
        }
        else if (api == GraphicsApi.D3D9 && executableContext.IsD3D8)
        {
            await messageBox.Show(UIStrings.Notice, UIStrings.D3D8_Info);
        }

        try
        {
            dllDeployer.Deploy(executableContext, api, addonSupport, addons);
            iniDeployer.Deploy(executableContext, addons);
            addonsDeployer.Deploy(executableContext, addons);

            // Only deploy preset if a template preset exists in the ReShade Deployer folder
            if (fileSystem.File.Exists(Paths.ReShadePresetIni))
            {
                // If the preset already exists in the target executable directory, ask to overwrite
                bool deployedPresetExists = fileSystem.File.Exists(presetDeployer.GetPath(executableContext));
                bool doDeploy = !deployedPresetExists || await messageBox.Show(UIStrings.PresetOverwrite_Title,
                    UIStrings.PresetOverwrite,
                    primaryButtonText: UIStrings.PresetOverwrite_Yes, ControlAppearance.Danger,
                    closeButtonText: UIStrings.PresetOverwrite_No) == MessageBoxResult.Primary;

                if (doDeploy)
                    presetDeployer.Deploy(executableContext);
            }
        }
        catch (Exception ex) when (ex is not DeploymentException)
        {
            throw new DeploymentException(ex);
        }

        if (config.AlwaysExitOnDeploy)
        {
            Application.Current.Shutdown();
        }
        else
        {
            snackbarService.Show(UIStrings.Success, UIStrings.DeploySuccess, ControlAppearance.Success, null, TimeSpan.FromSeconds(2.5));
        }
    }
}