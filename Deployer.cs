using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ReShadeInstaller;

public static class Deployer
{
    public static void InstallReShadeForExecutable(string api, bool addonSupport)
    {
        if (TrySelectExecutable(out string fileName))
        {
            string dllPath = addonSupport ? Paths.AddonDlls : Paths.Dlls;
            string directoryName = Path.GetDirectoryName(fileName)!;
                
            DllDeployer.Deploy(directoryName, dllPath, fileName, api);
            IniDeployer.Deploy(directoryName);
                
            if (File.Exists(".\\ReShadePreset.ini"))
                PresetDeployer.Deploy(directoryName);
                
            string message = """
                ReShade was successfully installed!
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
    }

    private static bool TrySelectExecutable(out string fileName)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Title = "Select the game's runtime executable.";
        openFileDialog.Filter = "Select EXE|*.exe";

        bool result = openFileDialog.ShowDialog() ?? false;

        fileName = openFileDialog.FileName;
        return result;
    }

    public static bool TryGetReShadeVersion(out string? version)
    {
        string path = Path.Combine(Paths.Dlls, "ReShade64.dll");
        if (File.Exists(path))
        {
            string fileVersion = FileVersionInfo.GetVersionInfo(path).FileVersion!;
            version = fileVersion.Substring(0, fileVersion.LastIndexOf('.'));
            return true;
        }

        version = null;
        return false;
    }
}