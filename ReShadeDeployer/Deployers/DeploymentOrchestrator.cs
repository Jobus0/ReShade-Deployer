using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace ReShadeDeployer;

/// <summary>
/// Provides methods for deploying ReShade for an executable: DLLs, INI files, and presets.
/// </summary>
public partial class DeploymentOrchestrator(IFileSystem fileSystem, DllDeployer dllDeployer,
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
        if (api == GraphicsApi.D3D9 && executableContext.IsD3D8)
            await messageBox.Show(UIStrings.Notice, UIStrings.D3D8_Info);

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
                    defaultTextBoxText: await GetDefaultPresetFolderName(executableContext)
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

    private async Task<string> GetDefaultPresetFolderName(ExecutableContext executableContext)
    {
        string defaultTextBoxText = StripBuildSuffixes().Replace(executableContext.FileName, "").TrimEnd('-', '_');

        try
        {
            // Check if ReShade.ini already exists and contains PresetPath with Presets folder
            string currentReShadeIni = fileSystem.Path.Combine(executableContext.DirectoryPath, "ReShade.ini");
            if (fileSystem.File.Exists(currentReShadeIni))
            {
                var reshadeIniContent = await fileSystem.File.ReadAllTextAsync(currentReShadeIni);
                var presetPathMatch = GetPresetPathValue().Match(reshadeIniContent);
                    
                if (presetPathMatch.Success)
                {
                    string currentPresetPath = presetPathMatch.Groups[1].Value.Trim();
                    if (currentPresetPath.StartsWith(Paths.Presets, StringComparison.OrdinalIgnoreCase))
                    {
                        // Extract the inner folder name from the path
                        // Expected format: Presets\SomeFolder\ReShadePreset.ini or Presets/SomeFolder/ReShadePreset.ini
                        var relativePath = currentPresetPath.Substring(Paths.Presets.Length).TrimStart('\\', '/');
                        var pathParts = relativePath.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
                            
                        if (pathParts.Length > 0)
                        {
                            // Use the first folder name as the default
                            defaultTextBoxText = pathParts[0];
                        }
                    }
                }
            }
        }
        catch (Exception) { /* ignore */ }

        return defaultTextBoxText;
    }

    [GeneratedRegex(@"(?i)(?:[-_]?(?:Win64|Win32|x64|x86))?(?:[-_]?(?:Shipping|Release|Retail|DebugGame|Test))?(?:[-_]?(?:dx11|dx12|vulkan|vk))?(?:[-_]?(?:Steam|Epic|GOG|EGS))?(?:\.exe)?$")]
    private static partial Regex StripBuildSuffixes();
    
    [GeneratedRegex(@"^PresetPath\s?=\s?(.*)$", RegexOptions.Multiline)]
    private static partial Regex GetPresetPathValue();
}