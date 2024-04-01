﻿using System;
using System.Diagnostics;
using System.IO;

namespace ReShadeDeployer;

public static class DllDeployer
{
    /// <summary>
    /// Deploy ReShade DLL to a game directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory to put the DLL symlink in.</param>
    /// <param name="executablePath">Path of the game executable.</param>
    /// <param name="dllPath">Path of the local downloaded ReShade DLL.</param>
    /// <param name="api">Target API name (dxgi, d3d9, opengl32, vulkan).</param>
    public static void Deploy(string directoryPath, string executablePath, string dllPath, string api)
    {
        if (api == "vulkan")
        {
            WpfMessageBox.Show(UIStrings.Vulkan_Info, UIStrings.Notice);
            return;
        }

        string symlinkPath = Path.Combine(directoryPath, api + ".dll");
            
        if (File.Exists(symlinkPath))
        {
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(symlinkPath);
            bool existingFileIsReShade = fileVersionInfo.FileDescription?.Contains("ReShade") ?? false;
            
            if (existingFileIsReShade)
            {
                File.Delete(symlinkPath);
            }
            else
            {
                // Find a unique filename for the existing non-ReShade .dll
                string oldPath = symlinkPath + ".old";
                int oldCount = 0;
                while (File.Exists(oldPath + (oldCount > 0 ? oldCount : null)))
                    oldCount++;

                if (oldCount > 0)
                    oldPath += oldCount;
                
                // Rename existing .dll to append .old (dxgi.dll becomes dxgi.dll.old, old dxgi.dll.old1 if that exists too)
                File.Move(symlinkPath, oldPath);
            }
        }
        
        SymbolicLink.CreateSymbolicLink(symlinkPath, Path.Combine(dllPath, IsX64(executablePath) ? "ReShade64.dll" : "ReShade32.dll"), 0);
    }

    /// <summary>
    /// Determines whether the specified executable file was built for x64 architecture.
    /// </summary>
    /// <param name="fileName">The path of the executable file to check.</param>
    /// <returns>
    ///   <c>true</c> if the specified file was built for x64 architecture; otherwise, <c>false</c>.
    /// </returns>
    private static bool IsX64(string fileName)
    {
        const ushort machineTypeX64 = 34404;
        
        byte[] buffer = new byte[4096];
        using (Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            _ = stream.Read(buffer, 0, buffer.Length);
        int peHeaderLocation = BitConverter.ToInt32(buffer, 60);
        return BitConverter.ToUInt16(buffer, peHeaderLocation + 4) == machineTypeX64;
    }
}