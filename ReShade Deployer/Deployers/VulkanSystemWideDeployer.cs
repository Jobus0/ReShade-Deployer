using System;
using System.IO;
using Microsoft.Win32;

namespace ReShadeDeployer;

public class VulkanSystemWideDeployer
{
    /// <summary>
    /// Deploy ReShade DLLs to a common local directory, and then register them for Vulkan system-wide.
    /// </summary>
    /// <param name="dllPath">Path to the ReShade DLLs.</param>
    public void DeployVulkanGlobally(string dllPath)
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
                
                string subKey = Environment.Is64BitOperatingSystem && layerModuleName == "ReShade32"
                    ? @"Software\Wow6432Node\Khronos\Vulkan\ImplicitLayers"
                    : @"Software\Khronos\Vulkan\ImplicitLayers";
                
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(subKey))
                    key.SetValue(symlinkJsonPath, 0, RegistryValueKind.DWord);
            }
        }
		catch (Exception e)
        {
            throw new DeploymentException("Vulkan deployment failed.", e);
		}
    }
    
    /// <summary>
    /// Remove ReShade DLLs from a common local directory and unregister them from Vulkan system-wide.
    /// </summary>
    public void RemoveVulkanGlobally()
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
                
            using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(Environment.Is64BitOperatingSystem && layerModuleName == "ReShade32" ? @"Software\Wow6432Node\Khronos\Vulkan\ImplicitLayers" : @"Software\Khronos\Vulkan\ImplicitLayers", true))
            {
                key?.DeleteValue(symlinkJsonPath, false);
            }
        }
    }
}