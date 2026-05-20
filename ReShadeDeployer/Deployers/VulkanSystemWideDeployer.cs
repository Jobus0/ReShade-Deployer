using System;
using System.IO.Abstractions;
using Microsoft.Win32;

namespace ReShadeDeployer;

public class VulkanSystemWideDeployer(IFileSystem fileSystem)
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
            foreach (var layerModuleName in new[] {"ReShade64", "ReShade32"})
            {
                string symlinkDllPath = fileSystem.Path.Combine(Paths.CommonPathLocal, layerModuleName + ".dll");
                string sourceDllPath = fileSystem.Path.Combine(dllPath, layerModuleName + ".dll");

                string symlinkJsonPath = fileSystem.Path.Combine(Paths.CommonPathLocal, layerModuleName + ".json");
                string sourceJsonPath = fileSystem.Path.Combine(dllPath, layerModuleName + ".json");

                fileSystem.Directory.CreateDirectory(Paths.CommonPathLocal);
                fileSystem.File.CreateSymbolicLink(symlinkDllPath, sourceDllPath);
                fileSystem.File.CreateSymbolicLink(symlinkJsonPath, sourceJsonPath);

                string subKey = Environment.Is64BitOperatingSystem && layerModuleName == "ReShade32"
                    ? @"Software\Wow6432Node\Khronos\Vulkan\ImplicitLayers"
                    : @"Software\Khronos\Vulkan\ImplicitLayers";

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(subKey))
                    key.SetValue(symlinkJsonPath, 0, RegistryValueKind.DWord);
            }
        }
        catch (Exception ex)
        {
            throw new DeploymentException("Vulkan deployment failed.", ex);
        }
    }

    /// <summary>
    /// Remove ReShade DLLs from a common local directory and unregister them from Vulkan system-wide.
    /// </summary>
    public void RemoveVulkanGlobally()
    {
        // Delete old Vulkan layer registries.
        string localApplicationData = fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ReShade");
        using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Khronos\Vulkan\ExplicitLayers", true))
        {
            key?.DeleteValue(fileSystem.Path.Combine(localApplicationData, "ReShade32", "ReShade32.json"), false);
            key?.DeleteValue(fileSystem.Path.Combine(localApplicationData, "ReShade64", "ReShade64.json"), false);
        }

        using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Khronos\Vulkan\ImplicitLayers", true))
        {
            key?.DeleteValue(fileSystem.Path.Combine(localApplicationData, "ReShade.json"), false);
            key?.DeleteValue(fileSystem.Path.Combine(localApplicationData, "VkLayer_override.json"), false);
            key?.DeleteValue(fileSystem.Path.Combine(localApplicationData, "ReShade32_vk_override_layer.json"), false);
            key?.DeleteValue(fileSystem.Path.Combine(localApplicationData, "ReShade64_vk_override_layer.json"), false);
            key?.DeleteValue(fileSystem.Path.Combine(localApplicationData, "ReShade32", "ReShade32.json"), false);
            key?.DeleteValue(fileSystem.Path.Combine(localApplicationData, "ReShade64", "ReShade64.json"), false);
        }

        using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"Software\Khronos\Vulkan\ImplicitLayers", true))
        {
            key?.DeleteValue(fileSystem.Path.Combine(localApplicationData, "ReShade32", "ReShade32.json"), false);
            key?.DeleteValue(fileSystem.Path.Combine(localApplicationData, "ReShade64", "ReShade64.json"), false);
        }

        using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Khronos\Vulkan\ImplicitLayers", true))
        {
            key?.DeleteValue(fileSystem.Path.Combine(localApplicationData, "ReShade32", "ReShade32.json"), false);
        }

        // Delete current Vulkan layer registries and files.
        foreach (var layerModuleName in new[] {"ReShade64", "ReShade32"})
        {
            string symlinkDllPath = fileSystem.Path.Combine(Paths.CommonPathLocal, layerModuleName + ".dll");
            string symlinkJsonPath = fileSystem.Path.Combine(Paths.CommonPathLocal, layerModuleName + ".json");

            if (fileSystem.File.Exists(symlinkDllPath))
                fileSystem.File.Delete(symlinkDllPath);

            if (fileSystem.File.Exists(symlinkJsonPath))
                fileSystem.File.Delete(symlinkJsonPath);

            using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(Environment.Is64BitOperatingSystem && layerModuleName == "ReShade32" ? @"Software\Wow6432Node\Khronos\Vulkan\ImplicitLayers" : @"Software\Khronos\Vulkan\ImplicitLayers", true))
            {
                key?.DeleteValue(symlinkJsonPath, false);
            }
        }
    }

    /// <summary>
    /// Get the currently registered Vulkan layer JSON path for 32-bit version.
    /// </summary>
    /// <returns>The path to the currently registered JSON file, or null if none is registered.</returns>
    private string? GetCurrentVulkan32BitLayerRegistryPath()
    {
        try
        {
            string subKey = Environment.Is64BitOperatingSystem
                ? @"Software\Wow6432Node\Khronos\Vulkan\ImplicitLayers"
                : @"Software\Khronos\Vulkan\ImplicitLayers";

            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(subKey);

            if (key == null)
                return null;

            string expectedJsonPath = fileSystem.Path.Combine(Paths.CommonPathLocal, "ReShade32.json");

            // Check if the expected JSON path is registered
            if (key.GetValue(expectedJsonPath) != null)
                return expectedJsonPath;
        }
        catch (Exception)
        {
            // Return null if registry access fails
        }

        return null;
    }

    public bool IsVulkanDeployedWithAddonSupport()
    {
        string? registryPath = GetCurrentVulkan32BitLayerRegistryPath();
        
        if (registryPath != null)
            return fileSystem.File.ResolveLinkTarget(registryPath, returnFinalTarget: true)?.FullName == fileSystem.Path.Combine(Paths.AddonDlls, "ReShade32.json");

        return false;
    }
}