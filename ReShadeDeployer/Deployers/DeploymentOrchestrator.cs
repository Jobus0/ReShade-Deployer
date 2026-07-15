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

        string? presetName = null;
        string? presetPath = null;
        bool doDeployPreset = false;

        if (fileSystem.File.Exists(Paths.ReShadePresetIni))
        {
            if (config.UseLocalPresets)
            {
                var (result, textBoxText) = await messageBox.ShowWithTextBox(
                    UIStrings.PresetFolderNameDialog_Title,
                    UIStrings.PresetFolderNameDialog_Content,
                    primaryButtonText: UIStrings.Continue,
                    secondaryButtonText: UIStrings.Cancel,
                    defaultTextBoxText: executableContext.FileName
                );

                if (result != MessageBoxResult.Primary)
                    return;

                presetName = textBoxText.Trim();
                
                if (string.IsNullOrEmpty(presetName))
                {
                    snackbarService.Show(UIStrings.PresetFolderNameDialog_NoName, ControlAppearance.Danger, SymbolRegular.ErrorCircle20);
                    return;
                }
            }
            
            presetPath = presetDeployer.GetPath(executableContext, presetName);
            bool deployedPresetExists = fileSystem.File.Exists(presetPath);
                
            doDeployPreset = !deployedPresetExists || await messageBox.Show(UIStrings.PresetOverwrite_Title,
                UIStrings.PresetOverwrite,
                primaryButtonText: UIStrings.PresetOverwrite_Yes, ControlAppearance.Danger,
                closeButtonText: UIStrings.PresetOverwrite_No) == MessageBoxResult.Primary;
        }

        try
        {
            dllDeployer.Deploy(executableContext, api, addonSupport, addons);
            iniDeployer.Deploy(executableContext, addons, presetPath);
            addonsDeployer.Deploy(executableContext, addons);

            if (doDeployPreset)
                presetDeployer.Deploy(executableContext, presetName);
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
            snackbarService.Show(UIStrings.Deploy_Success, ControlAppearance.Success, SymbolRegular.Rocket20);
        }
    }
}