using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace ReShadeDeployer;

public enum GraphicsApi { D3D9, DXGI, OpenGL, Vulkan }

public static class DllDeployer
{
    /// <summary>
    /// Deploy ReShade DLL to a game directory.
    /// </summary>
    /// <param name="executableContext">Context for the executable to deploy.</param>
    /// <param name="api">Target API name (dxgi, d3d9, opengl32, vulkan).</param>
    /// <param name="addonSupport">Whether to deploy the addon supported DLL instead of the normal one.</param>
    public static void Deploy(ExecutableContext executableContext, GraphicsApi api, bool addonSupport)
    {
        string dllPath = addonSupport ? Paths.AddonDlls : Paths.Dlls;
        
        if (api == GraphicsApi.Vulkan)
        {
            DeployVulkanGlobally(dllPath);
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

    /// <summary>
    /// Deploy ReShade DLLs to a common local directory, and then register them for Vulkan system-wide.
    /// </summary>
    /// <param name="dllPath">Path to the ReShade DLLs.</param>
    private static void DeployVulkanGlobally(string dllPath)
    {
		try
        {
            // Remove existing Vulkan ReShade registries and files first
            RemoveVulkanGlobally();

            // Add new Vulkan ReShade registries and files using symlinks
            foreach (var layerModuleName in new[] { "ReShade64", "ReShade32" })
            {
                string symlinkDllPath = Path.Combine(Paths.CommonPathLocal, layerModuleName + ".dll");
                string sourceDllPath = Path.Combine(dllPath, layerModuleName + ".dll");
                
                string symlinkJsonPath = Path.Combine(Paths.CommonPathLocal, layerModuleName + ".json");
                string sourceJsonPath = Path.Combine(dllPath, layerModuleName + ".json");
                
                Directory.CreateDirectory(Paths.CommonPathLocal);
                SymbolicLink.CreateSymbolicLink(symlinkDllPath, sourceDllPath, 0);
                SymbolicLink.CreateSymbolicLink(symlinkJsonPath, sourceJsonPath, 0);
                
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(Environment.Is64BitOperatingSystem && layerModuleName == "ReShade32" ? @"Software\Wow6432Node\Khronos\Vulkan\ImplicitLayers" : @"Software\Khronos\Vulkan\ImplicitLayers"))
                    key.SetValue(symlinkJsonPath, 0, RegistryValueKind.DWord);
            }
        }
		catch (Exception e)
		{
			WpfMessageBox.Show("Vulkan Installation Failed", "Failed to install Vulkan layer manifest:\n" + e.Message);
		}
    }
    
    /// <summary>
    /// Remove ReShade DLLs from a common local directory and unregister them from Vulkan system-wide.
    /// </summary>
    public static void RemoveVulkanGlobally()
    {
        // Delete old Vulkan layer registries.
        string localApplicationData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ReShade");
        using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Khronos\Vulkan\ExplicitLayers", true))
        {
            key?.DeleteValue(Path.Combine(localApplicationData, "ReShade32", "ReShade32.json"), false);
            key?.DeleteValue(Path.Combine(localApplicationData, "ReShade64", "ReShade64.json"), false);
        }
        using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Khronos\Vulkan\ImplicitLayers", true))
        {
            key?.DeleteValue(Path.Combine(localApplicationData, "ReShade.json"), false);
            key?.DeleteValue(Path.Combine(localApplicationData, "VkLayer_override.json"), false);
            key?.DeleteValue(Path.Combine(localApplicationData, "ReShade32_vk_override_layer.json"), false);
            key?.DeleteValue(Path.Combine(localApplicationData, "ReShade64_vk_override_layer.json"), false);
            key?.DeleteValue(Path.Combine(localApplicationData, "ReShade32", "ReShade32.json"), false);
            key?.DeleteValue(Path.Combine(localApplicationData, "ReShade64", "ReShade64.json"), false);
        }
        using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"Software\Khronos\Vulkan\ImplicitLayers", true))
        {
            key?.DeleteValue(Path.Combine(localApplicationData, "ReShade32", "ReShade32.json"), false);
            key?.DeleteValue(Path.Combine(localApplicationData, "ReShade64", "ReShade64.json"), false);
        }
        using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Khronos\Vulkan\ImplicitLayers", true))
        {
            key?.DeleteValue(Path.Combine(localApplicationData, "ReShade32", "ReShade32.json"), false);
        }
        
        // Delete current Vulkan layer registries and files.
        foreach (var layerModuleName in new[] { "ReShade64", "ReShade32" })
        {
            string symlinkDllPath = Path.Combine(Paths.CommonPathLocal, layerModuleName + ".dll");
            string symlinkJsonPath = Path.Combine(Paths.CommonPathLocal, layerModuleName + ".json");
                
            if (File.Exists(symlinkDllPath))
                File.Delete(symlinkDllPath);
                
            if (File.Exists(symlinkJsonPath))
                File.Delete(symlinkJsonPath);
                
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(Environment.Is64BitOperatingSystem && layerModuleName == "ReShade32" ? @"Software\Wow6432Node\Khronos\Vulkan\ImplicitLayers" : @"Software\Khronos\Vulkan\ImplicitLayers"))
                key.DeleteValue(symlinkJsonPath);
        }
    }

    private static string DllNameFromApi(GraphicsApi api) => api switch
    {
        GraphicsApi.D3D9 => "d3d9",
        GraphicsApi.DXGI => "dxgi",
        GraphicsApi.OpenGL => "opengl32",
        _ => throw new ArgumentOutOfRangeException(nameof(api), api, null)
    };
}