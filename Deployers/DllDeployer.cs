using System;
using System.Diagnostics;
using System.IO;

namespace ReShadeDeployer;

public enum GraphicsApi { D3D9, DXGI, OpenGL, Vulkan }

public static class DllDeployer
{
    /// <summary>
    /// Deploy ReShade DLL to a game directory.
    /// </summary>
    /// <param name="executableContext">Context for the executable to deploy.</param>
    /// <param name="api">Target API name (dxgi, d3d9, opengl32, vulkan).</param>
    /// <param name="dllPath">Path of the local downloaded ReShade DLL.</param>
    public static void Deploy(ExecutableContext executableContext, GraphicsApi api, string dllPath)
    {
        if (api == GraphicsApi.Vulkan)
        {
            WpfMessageBox.Show(UIStrings.Vulkan_Info, UIStrings.Notice);
            return;
        }
        
        if (api == GraphicsApi.D3D9 && executableContext.IsD3D8)
            WpfMessageBox.Show(UIStrings.D3D8_Info, UIStrings.Notice);

        string symlinkPath = Path.Combine(executableContext.DirectoryPath, DllNameFromApi(api) + ".dll");
            
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
        
        SymbolicLink.CreateSymbolicLink(symlinkPath, Path.Combine(dllPath, executableContext.IsX64 ? "ReShade64.dll" : "ReShade32.dll"), 0);
    }

    private static string DllNameFromApi(GraphicsApi api) => api switch
    {
        GraphicsApi.D3D9 => "d3d9",
        GraphicsApi.DXGI => "dxgi",
        GraphicsApi.OpenGL => "opengl32",
        _ => throw new ArgumentOutOfRangeException(nameof(api), api, null)
    };
}