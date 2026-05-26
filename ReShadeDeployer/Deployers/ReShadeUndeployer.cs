using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;

namespace ReShadeDeployer;

public class ReShadeUndeployer(IFileSystem fileSystem)
{
    /// <summary>
    /// Finds all ReShade-related files in the specified game directory.
    /// </summary>
    /// <returns>List of full paths to files that can be removed.</returns>
    public ICollection<string> FindReShadeFiles(string gameDirectory)
    {
        var files = new HashSet<string>();
        
        if (!fileSystem.Directory.Exists(gameDirectory))
            return files;
        
        var directoryInfo = fileSystem.DirectoryInfo.New(gameDirectory);
        
        // Find ReShade.ini
        string iniPath = fileSystem.Path.Combine(gameDirectory, "ReShade.ini");
        if (fileSystem.File.Exists(iniPath))
            files.Add(iniPath);
        
        // Find DLLs where FileDescription contains "ReShade"
        foreach (var entry in directoryInfo.EnumerateFiles("*.dll", SearchOption.TopDirectoryOnly))
        {
            string dllPath = entry.FullName;
            if (fileSystem.File.Exists(dllPath))
            {
                try
                {
                    var fileInfo = fileSystem.FileVersionInfo.GetVersionInfo(dllPath);
                    if (fileInfo.FileDescription?.Contains("ReShade") == true)
                        files.Add(dllPath);
                }
                catch
                {
                    // Ignore errors accessing file info (e.g. permission issues or not a valid PE file)
                }
            }
        }

        // Find all add-on symlinks
        foreach (var entry in directoryInfo.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly))
            if (entry.LinkTarget != null && entry.LinkTarget.StartsWith(Paths.Addons))
                files.Add(entry.FullName);
        
        // Find all .addon32 and .addon64 files (including manually installed non-symlinks)
        foreach (var addon in directoryInfo.EnumerateFiles("*.addon32 OR *.addon64", SearchOption.TopDirectoryOnly))
            files.Add(addon.FullName);
        
        return files;
    }
    
    /// <summary>
    /// Removes all ReShade-related files from the specified game directory.
    /// </summary>
    public void Undeploy(string gameDirectory)
    {
        foreach (var file in FindReShadeFiles(gameDirectory))
        {
            if (fileSystem.File.Exists(file))
                fileSystem.File.Delete(file);
            else if (fileSystem.Directory.Exists(file))
                fileSystem.Directory.Delete(file, true);
        }
    }
}
