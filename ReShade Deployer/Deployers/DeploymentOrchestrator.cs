using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Wpf.Ui.Common;

namespace ReShadeDeployer;

/// <summary>
/// Provides methods for deploying ReShade for an executable: DLLs, INI files, and presets.
/// </summary>
public class DeploymentOrchestrator(DllDeployer dllDeployer, VulkanSystemWideDeployer vulkanSystemWideDeployer, IniDeployer iniDeployer, PresetDeployer presetDeployer, AddonsDeployer addonsDeployer, IMessageBox messageBox, IConfig config)
{
    /// <summary>
    /// Deploys ReShade for a specified executable.
    /// </summary>
    /// <param name="executableContext">Context for the executable to deploy.</param>
    /// <param name="api">Target API name (dxgi, d3d9, opengl32, vulkan).</param>
    /// <param name="addonSupport">Whether to deploy the addon supported DLL instead of the normal one.</param>
    /// <param name="addons">List of addons to deploy.</param>
    public void DeployReShadeForExecutable(ExecutableContext executableContext, GraphicsApi api, bool addonSupport, IList<AddonItem> addons)
    {
        if (api == GraphicsApi.Vulkan && addonSupport && !vulkanSystemWideDeployer.IsVulkanDeployedWithAddonSupport())
        {
            var result = messageBox.Show(
                UIStrings.Vulkan_Addon_Warning,
                UIStrings.Warning,
                UIStrings.Continue,
                UIStrings.Cancel,
                ControlAppearance.Caution);
            
            if (result != IMessageBox.Result.Primary)
                return;
        }
        else if (api == GraphicsApi.D3D9 && executableContext.IsD3D8)
        {
            messageBox.Show(UIStrings.D3D8_Info, UIStrings.Notice);
        }

        try
        {
            dllDeployer.Deploy(executableContext, api, addonSupport);
            iniDeployer.Deploy(executableContext, addons);
            
            if (addonSupport)
                addonsDeployer.Deploy(executableContext, addons);

            // Only deploy preset if a template preset exists in the ReShade Deployer folder
            if (File.Exists(Paths.ReShadePresetIni))
            {
                // If the preset already exists in the target executable directory, ask to overwrite
                bool deployedPresetExists = File.Exists(presetDeployer.GetPath(executableContext));
                bool doDeploy = !deployedPresetExists || messageBox.Show(
                    UIStrings.PresetOverwrite,
                    UIStrings.PresetOverwrite_Title,
                    UIStrings.PresetOverwrite_Yes,
                    UIStrings.PresetOverwrite_No,
                    ControlAppearance.Danger) == IMessageBox.Result.Primary;

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
            var result = messageBox.Show(
                UIStrings.DeploySuccess,
                UIStrings.Success,
                UIStrings.Continue,
                UIStrings.Exit);
            
            if (result == IMessageBox.Result.Secondary)
                Application.Current.Shutdown();
        }
    }
}