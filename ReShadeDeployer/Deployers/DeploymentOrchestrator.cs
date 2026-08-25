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
public class DeploymentOrchestrator(IFileSystem fileSystem, FileHelper fileHelper, DllDeployer dllDeployer,
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
    /// <param name="presetName">Optional preset folder name for centralized presets.</param>
    public async Task DeployReShadeForExecutable(ExecutableContext executableContext, GraphicsApi api, bool addonSupport, IList<AddonItem> addons, string? presetName = null)
    {
        if (api == GraphicsApi.D3D9 && executableContext.IsD3D8)
            await messageBox.Show(UIStrings.Notice, UIStrings.D3D8_Info);
        
        presetName = presetName?.Trim();

        string? presetPath = null;
        bool doDeployPreset = false;
        bool useCentralizedPresets = !string.IsNullOrWhiteSpace(presetName);
        bool defaultPresetExists = fileSystem.File.Exists(Paths.ReShadePresetIni);

        if (defaultPresetExists || useCentralizedPresets)
        {
            presetPath = presetDeployer.GetPath(executableContext, presetName);
            
            bool presetAlreadyExists = fileSystem.File.Exists(presetPath);
            bool isIdentical = defaultPresetExists &&
                               presetAlreadyExists &&
                               fileHelper.AreFilesEqual(Paths.ReShadePresetIni, presetPath);

            bool isExistingCentralDefault = useCentralizedPresets &&
                                           presetAlreadyExists &&
                                           IsPresetDefaultContent(presetPath);

            if (!presetAlreadyExists)
            {
                doDeployPreset = true;
            }
            else if (isIdentical)
            {
                doDeployPreset = false;
            }
            else if (isExistingCentralDefault)
            {
                doDeployPreset = defaultPresetExists;
            }
            else
            {
                doDeployPreset = await messageBox.Show(UIStrings.PresetOverwrite_Title,
                    UIStrings.PresetOverwrite,
                    primaryButtonText: UIStrings.PresetOverwrite_Yes, ControlAppearance.Danger,
                    closeButtonText: UIStrings.PresetOverwrite_No) == MessageBoxResult.Primary;
            }
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

    private bool IsPresetDefaultContent(string presetPath)
    {
        try
        {
            string content = fileSystem.File.ReadAllText(presetPath);
            return string.Equals(
                content.Replace("\r\n", "\n").Trim(),
                PresetDeployer.GetDefaultContent().Replace("\r\n", "\n").Trim(),
                StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}