using System;
using System.Diagnostics;
using System.IO;

namespace ReShadeDeployer;

public enum GraphicsApi { D3D9, DXGI, OpenGL, Vulkan }

public class DllDeployer(VulkanSystemWideDeployer vulkanSystemWideDeployer)
{
    /// <summary>
    /// Deploy ReShade DLL to a game directory.
    /// </summary>
    /// <param name="executableContext">Context for the executable to deploy.</param>
    /// <param name="api">Target API name (dxgi, d3d9, opengl32, vulkan).</param>
    /// <param name="addonSupport">Whether to deploy the addon supported DLL instead of the normal one.</param>
    public void Deploy(ExecutableContext executableContext, GraphicsApi api, bool addonSupport)
    {
        string dllPath = addonSupport ? Paths.AddonDlls : Paths.Dlls;
        
        if (api == GraphicsApi.Vulkan)
        {
            vulkanSystemWideDeployer.DeployVulkanGlobally(dllPath);
            return;
        }

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