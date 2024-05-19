using System.IO;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Common;

namespace ReShadeDeployer;

public static class PresetDeployer
{
    /// <summary>
    /// Deploy ReShadePreset.ini to a directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory to write ReShadePreset.ini to.</param>
    public static void Deploy(string directoryPath)
    {
        string path = Path.Combine(directoryPath, "ReShadePreset.ini");

        if (File.Exists(path))
        {
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = UIStrings.PresetOverwrite_Title,
                Content = new TextBlock {Text = UIStrings.PresetOverwrite, TextWrapping = TextWrapping.Wrap},
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.Height,
                ButtonLeftName = UIStrings.PresetOverwrite_Yes,
                ButtonLeftAppearance = ControlAppearance.Danger,
                ButtonRightName = UIStrings.PresetOverwrite_No,
                Width = 300
            };
            messageBox.ButtonLeftClick += (_, _) => {messageBox.DialogResult = true; messageBox.Close();};
            messageBox.ButtonRightClick += (_, _) => {messageBox.DialogResult = false; messageBox.Close();};

            if (messageBox.ShowDialog() != true)
                return;
        }
                
        File.Copy(Paths.ReShadePresetIni, path, true);
    }
}