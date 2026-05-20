using System.IO.Abstractions;

namespace ReShadeDeployer;

public class PresetDeployer(IFileSystem fileSystem)
{
    /// <summary>
    /// Deploy ReShadePreset.ini to a directory.
    /// </summary>
    /// <param name="executableContext">Context for the executable to deploy.</param>
    public void Deploy(ExecutableContext executableContext)
    {
        if (fileSystem.File.Exists(Paths.ReShadePresetIni))
            fileSystem.File.Copy(Paths.ReShadePresetIni, GetPath(executableContext), true);
    }

    /// <summary>
    /// Gets the path to the ReShadePreset.ini file based on the executable context.
    /// </summary>
    /// <param name="executableContext">The context of the executable for which to get the path.</param>
    /// <returns>The full path to the ReShadePreset.ini file.</returns>
    public string GetPath(ExecutableContext executableContext)
    {
        return fileSystem.Path.Combine(executableContext.DirectoryPath, "ReShadePreset.ini");
    }
}