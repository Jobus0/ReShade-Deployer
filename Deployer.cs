using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Wpf.Ui.Common;

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
    /// <param name="exitOnDeploy">Whether the application should exit after deployment.</param>
    public static void SelectExecutableAndDeployReShade(GraphicsApi api, bool addonSupport, bool exitOnDeploy)
    {
        if (TrySelectExecutable(out string executablePath))
        {
            var executableContext = new ExecutableContext(executablePath);
            DeployReShadeForExecutable(executableContext, api, addonSupport, exitOnDeploy);
        }
    }

    /// <summary>
    /// Deploys ReShade for a specified executable.
    /// </summary>
    /// <param name="executableContext">Context for the executable to deploy.</param>
    /// <param name="api">Target API name (dxgi, d3d9, opengl32, vulkan).</param>
    /// <param name="addonSupport">Whether to deploy the addon supported DLL instead of the normal one.</param>
    /// <param name="exitOnDeploy">Whether the application should exit after deployment.</param>
    public static void DeployReShadeForExecutable(ExecutableContext executableContext, GraphicsApi api, bool addonSupport, bool exitOnDeploy)
    {
        if (api == GraphicsApi.Vulkan && addonSupport)
        {
            bool cancel = false;
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = UIStrings.Warning,
                Content = new TextBlock {Text = UIStrings.Vulkan_Addon_Warning, TextWrapping = TextWrapping.Wrap},
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.Height,
                ButtonLeftName = UIStrings.Continue,
                ButtonLeftAppearance = ControlAppearance.Caution,
                ButtonRightName = UIStrings.Cancel,
                Width = 280
            };
            messageBox.ButtonLeftClick += (_, _) => messageBox.Close();
            messageBox.ButtonRightClick += (_, _) =>
            {
                cancel = true;
                messageBox.Close();
            };
            messageBox.ShowDialog();

            if (cancel)
                return;
        }
        
        DllDeployer.Deploy(executableContext, api, addonSupport);
        IniDeployer.Deploy(executableContext);
                
        if (File.Exists(Paths.ReShadePresetIni))
            PresetDeployer.Deploy(executableContext);

        if (exitOnDeploy)
        {
            Application.Current.Shutdown();
        }
        else
        {
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = UIStrings.DeploySuccess_Title,
                Content = new TextBlock {Text = UIStrings.DeploySuccess, TextWrapping = TextWrapping.Wrap},
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.Height,
                ButtonLeftName = UIStrings.Continue,
                ButtonRightName = UIStrings.Exit,
                Width = 260
            };
            messageBox.ButtonLeftClick += (_, _) => messageBox.Close();
            messageBox.ButtonRightClick += (_, _) => Application.Current.Shutdown();
            messageBox.ShowDialog();
        }
    }

    /// <summary>
    /// Opens a file dialog for selecting an executable.
    /// </summary>
    /// <param name="executablePath">Path of the selected game executable.</param>
    /// <returns>True if a file was selected, false if cancelled.</returns>
    public static bool TrySelectExecutable(out string executablePath)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Title = UIStrings.OpenFileDialog_Title;
        openFileDialog.Filter = "Select EXE|*.exe";

        bool result = openFileDialog.ShowDialog() ?? false;

        executablePath = openFileDialog.FileName;
        return result;
    }
}