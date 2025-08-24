using System.IO;
using Wpf.Ui.Common;

namespace ReShadeDeployer;

public class PresetDeployer(IMessageBox messageBox)
{
    /// <summary>
    /// Deploy ReShadePreset.ini to a directory.
    /// </summary>
    /// <param name="executableContext">Context for the executable to deploy.</param>
    public void Deploy(ExecutableContext executableContext)
    {
        string path = Path.Combine(executableContext.DirectoryPath, "ReShadePreset.ini");

        if (File.Exists(path))
        {
            var result = messageBox.Show(
                UIStrings.PresetOverwrite,
                UIStrings.PresetOverwrite_Title,
                UIStrings.PresetOverwrite_Yes,
                UIStrings.PresetOverwrite_No,
                ControlAppearance.Danger);
            
            if (result != IMessageBox.Result.Primary)
                return;
        }
                
        File.Copy(Paths.ReShadePresetIni, path, true);
    }
}