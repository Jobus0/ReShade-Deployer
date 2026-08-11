using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Wpf.Ui.Controls;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace ReShadeDeployer;

/// <summary>
/// Provides methods for deploying ReShade for an executable: DLLs, INI files, and presets.
/// </summary>
public class DeploymentOrchestrator(IFileSystem fileSystem, DllDeployer dllDeployer,
    IniDeployer iniDeployer, PresetDeployer presetDeployer, AddonsDeployer addonsDeployer,
    IMessageBox messageBox)
{
    /// <summary>
    /// Deploys ReShade for a specified executable.
    /// </summary>
    /// <param name="executableContext">Context for the executable to deploy.</param>
    /// <param name="api">Target API name (dxgi, d3d9, opengl32, vulkan).</param>
    /// <param name="addonSupport">Whether to deploy the addon supported DLL instead of the normal one.</param>
    /// <param name="addons">List of addons to deploy.</param>
    /// <param name="presetName">Optional preset folder name for local presets.</param>
    public async Task DeployReShadeForExecutable(ExecutableContext executableContext, GraphicsApi api, bool addonSupport, IList<AddonItem> addons, string? presetName = null)
    {
        if (api == GraphicsApi.D3D9 && executableContext.IsD3D8)
            await messageBox.Show(UIStrings.Notice, UIStrings.D3D8_Info);
        
        presetName = presetName?.Trim();

        string? presetPath = null;
        bool doDeployPreset = false;
        bool useLocalPresets = !string.IsNullOrWhiteSpace(presetName);

        if (fileSystem.File.Exists(Paths.ReShadePresetIni) || useLocalPresets)
        {
            presetPath = presetDeployer.GetPath(executableContext, presetName);
            
            bool presetAlreadyExists = fileSystem.File.Exists(presetPath);
            doDeployPreset = !presetAlreadyExists || await messageBox.Show(UIStrings.PresetOverwrite_Title,
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
    }
}