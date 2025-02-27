using System.IO;
using Wpf.Ui.Common;

namespace ReShadeDeployer;

public static class PresetDeployer
{
    /// <summary>
    /// Deploy ReShadePreset.ini to a directory.
    /// </summary>
    /// <param name="executableContext">Context for the executable to deploy.</param>
    public static void Deploy(ExecutableContext executableContext)
    {
        string path = Path.Combine(executableContext.DirectoryPath, "ReShadePreset.ini");

        if (File.Exists(path))
        {
            var result = WpfMessageBox.Show(
                UIStrings.PresetOverwrite,
                UIStrings.PresetOverwrite_Title,
                UIStrings.PresetOverwrite_Yes,
                UIStrings.PresetOverwrite_No,
                ControlAppearance.Danger);
            
            if (result != WpfMessageBox.Result.Primary)
                return;
        }
                
        File.Copy(Paths.ReShadePresetIni, path, true);
    }
}