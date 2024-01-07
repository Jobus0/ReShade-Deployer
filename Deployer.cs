using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ReShadeDeployer;

/// <summary>
/// Provides methods for deploying ReShade for an executable: DLLs, INI files, and presets.
/// </summary>
public static class Deployer
{
    /// <summary>
    /// Opens a file dialog for selecting an executable, then deploys ReShade for that executable.
    /// </summary>
    /// <param name="api">Target API name (dxgi, d3d9, opengl32, vulkan).</param>
    /// <param name="addonSupport">Whether to deploy the addon supported DLL instead of the normal one.</param>
    public static void SelectExecutableAndDeployReShade(string api, bool addonSupport)
    {
        if (TrySelectExecutable(out string executablePath))
        {
            DeployReShadeForExecutable(api, addonSupport, executablePath);
        }
    }

    /// <summary>
    /// Deploys ReShade for a specified executable.
    /// </summary>
    /// <param name="api">Target API name (dxgi, d3d9, opengl32, vulkan).</param>
    /// <param name="addonSupport">Whether to deploy the addon supported DLL instead of the normal one.</param>
    /// <param name="executablePath">Path of the game executable.</param>
    public static void DeployReShadeForExecutable(string api, bool addonSupport, string executablePath)
    {
        string dllPath = addonSupport ? Paths.AddonDlls : Paths.Dlls;
        string directoryPath = Path.GetDirectoryName(executablePath)!;

        DllDeployer.Deploy(directoryPath, executablePath, dllPath, api);
        IniDeployer.Deploy(directoryPath);
                
        if (File.Exists(Paths.ReShadePresetIni))
            PresetDeployer.Deploy(directoryPath);
                
        string message = """
                ReShade was successfully deployed!
                """;
            
        var messageBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = "Success",
            Content = new TextBlock {Text = message, TextWrapping = TextWrapping.Wrap},
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.Height,
            ButtonLeftName = "Continue",
            ButtonRightName = "Exit",
            Width = 260
        };
        messageBox.ButtonLeftClick += (_, _) => messageBox.Close();
        messageBox.ButtonRightClick += (_, _) => Application.Current.Shutdown();
        messageBox.ShowDialog();
    }

    /// <summary>
    /// Opens a file dialog for selecting an executable.
    /// </summary>
    /// <param name="executablePath">Path of the selected game executable.</param>
    /// <returns>True if a file was selected, false if cancelled.</returns>
    private static bool TrySelectExecutable(out string executablePath)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Title = "Select the game's runtime executable.";
        openFileDialog.Filter = "Select EXE|*.exe";

        bool result = openFileDialog.ShowDialog() ?? false;

        executablePath = openFileDialog.FileName;
        return result;
    }
}