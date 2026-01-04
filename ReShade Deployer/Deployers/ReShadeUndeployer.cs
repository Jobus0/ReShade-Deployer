using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ReShadeDeployer;

public class ReShadeUndeployer
{
    private static readonly string[] DllNames = { "dxgi.dll", "d3d9.dll", "opengl32.dll" };
    
    /// <summary>
    /// Finds all ReShade-related files in the specified game directory.
    /// </summary>
    /// <returns>List of full paths to files that can be removed.</returns>
    public List<string> FindReShadeFiles(string gameDirectory)
    {
        var files = new List<string>();
        
        // 1. Check for ReShade.ini
        string iniPath = Path.Combine(gameDirectory, "ReShade.ini");
        if (File.Exists(iniPath))
            files.Add(iniPath);
        
        // 2. Check DLLs - only if FileDescription contains "ReShade"
        foreach (var dllName in DllNames)
        {
            string dllPath = Path.Combine(gameDirectory, dllName);
            if (File.Exists(dllPath))
            {
                try
                {
                    var fileInfo = FileVersionInfo.GetVersionInfo(dllPath);
                    if (fileInfo.FileDescription?.Contains("ReShade") == true)
                        files.Add(dllPath);
                }
                catch
                {
                    // Ignore errors accessing file info (e.g. permission issues or not a valid PE file)
                }
            }
        }
        
        // 3. Find all .addon32 and .addon64 files
        try
        {
            files.AddRange(Directory.GetFiles(gameDirectory, "*.addon32"));
            files.AddRange(Directory.GetFiles(gameDirectory, "*.addon64"));
        }
        catch (DirectoryNotFoundException)
        {
            // Directory doesn't exist, so no files to find
        }
        
        return files;
    }
    
    /// <summary>
    /// Removes all ReShade-related files from the specified game directory.
    /// </summary>
    public void Undeploy(string gameDirectory)
    {
        foreach (var file in FindReShadeFiles(gameDirectory))
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }
}
