using System.IO.Abstractions;

namespace ReShadeDeployer;

public class PresetDeployer(IFileSystem fileSystem)
{
    /// <summary>
    /// Deploy ReShadePreset.ini to a directory.
    /// </summary>
    /// <param name="executableContext">Context for the executable to deploy.</param>
    /// <param name="presetName">Optional name of the preset folder for centralized storage.</param>
    public void Deploy(ExecutableContext executableContext, string? presetName = null)
    {
        string destPath = GetPath(executableContext, presetName);
        string? destDir = fileSystem.Path.GetDirectoryName(destPath);
        if (destDir != null && !fileSystem.Directory.Exists(destDir))
            fileSystem.Directory.CreateDirectory(destDir);
        
        if (fileSystem.File.Exists(Paths.ReShadePresetIni))
            fileSystem.File.Copy(Paths.ReShadePresetIni, destPath, true);
        else if (!string.IsNullOrEmpty(presetName))
            fileSystem.File.WriteAllText(GetPath(executableContext, presetName), GetDefaultContent());
    }

    /// <summary>
    /// Gets the path to the ReShadePreset.ini file based on the executable context.
    /// </summary>
    /// <param name="executableContext">The context of the executable for which to get the path.</param>
    /// <param name="presetName">Optional name of the preset folder for centralized storage.</param>
    /// <returns>The full path to the ReShadePreset.ini file.</returns>
    public string GetPath(ExecutableContext executableContext, string? presetName = null)
    {
        if (!string.IsNullOrEmpty(presetName))
            return fileSystem.Path.Combine(Paths.Presets, presetName, "ReShadePreset.ini");
        
        return fileSystem.Path.Combine(executableContext.DirectoryPath, "ReShadePreset.ini");
    }

    /// <summary>
    /// Gets the default content for a ReShade preset file.
    /// </summary>
    /// <returns>The default preset content string.</returns>
    public static string GetDefaultContent()
    {
        return "Techniques=\nTechniqueSorting=";
    }
}