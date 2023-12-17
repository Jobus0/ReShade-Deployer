﻿using System.IO;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Common;

namespace ReShadeInstaller;

public static class PresetDeployer
{
    /// <summary>
    /// Deploy ReShadePreset.ini to a directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory to write ReShadePreset.ini to.</param>
    public static void Deploy(string directoryPath)
    {
        string path = directoryPath + "\\ReShadePreset.ini";

        if (File.Exists(path))
        {
            string message = """
                ReShadePreset.ini already exists.

                Would you like to overwrite it?"
                """;
            
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Overwrite Preset",
                Content = new TextBlock {Text = message, TextWrapping = TextWrapping.Wrap},
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.Height,
                ButtonLeftName = "Yes, overwrite file",
                ButtonLeftAppearance = ControlAppearance.Danger,
                ButtonRightName = "No, keep old file",
                Width = 300
            };
            messageBox.ButtonLeftClick += (_, _) => {messageBox.DialogResult = true; messageBox.Close();};
            messageBox.ButtonRightClick += (_, _) => {messageBox.DialogResult = false; messageBox.Close();};

            if (messageBox.ShowDialog() != true)
                return;
        }
                
        File.Copy(".\\ReShadePreset.ini", path, true);
    }
}