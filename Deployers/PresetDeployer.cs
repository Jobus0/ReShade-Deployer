using System.IO;
using System.Windows;

namespace ReShadeInstaller;

public static class PresetDeployer
{
    public static void Deploy(string installPath)
    {
        string path = installPath + "\\ReShadePreset.ini";
            
        if (File.Exists(path) && MessageBox.Show("ReShadePreset.ini already exists. Would you like to overwrite it?", "Overwrite Preset", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            return;
                
        File.Copy(".\\ReShadePreset.ini", path, true);
    }
}