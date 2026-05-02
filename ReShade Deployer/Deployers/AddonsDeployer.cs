using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReShadeDeployer;

/// <summary>
/// Handles discovery and deployment of ReShade addons.
/// </summary>
public class AddonsDeployer
{
    private readonly string[] configExtensions = [".cfg", ".ini", ".xml", ".json", ".yaml", ".yml"];

    /// <summary>
    /// Gets a list of available addon names from the configured Add-ons directory.
    /// .addon32 and .addon64 files with the same name are grouped together.
    /// </summary>
    /// <returns>A sorted list of AddonItem.</returns>
    public IList<AddonItem> GetAvailableAddons()
    {
        if (!Directory.Exists(Paths.Addons))
            return Array.Empty<AddonItem>();

        var addons = new List<AddonItem>();

        var addonsDirInfo = new DirectoryInfo(Paths.Addons);
        var topFiles = addonsDirInfo.GetFileSystemInfos("*.addon*", SearchOption.TopDirectoryOnly);
        foreach (var file in topFiles)
        {
            string name = Path.GetFileNameWithoutExtension(file.Name);

            bool isAddon32 = file.Extension.Equals(".addon32", StringComparison.OrdinalIgnoreCase);
            bool isAddon64 = file.Extension.Equals(".addon64", StringComparison.OrdinalIgnoreCase);
            if (isAddon32 || isAddon64)
            {
                var addonItem = addons.FirstOrDefault(x => x.Name == name);
                if (addonItem != null)
                {
                    if (isAddon32)
                        addonItem.X32Path = file.FullName;
                    else if (isAddon64)
                        addonItem.X64Path = file.FullName;
                }
                else
                {
                    addonItem = new AddonItem() {Name = name};

                    if (isAddon32)
                        addonItem.X32Path = file.FullName;
                    else if (isAddon64)
                        addonItem.X64Path = file.FullName;

                    addons.Add(addonItem);
                }
            }
        }

        var directories = addonsDirInfo.GetFileSystemInfos("*", SearchOption.TopDirectoryOnly).Where(fsi => fsi is DirectoryInfo);
        foreach (var directory in directories)
        {
            var dirInfo = (DirectoryInfo)directory;
            var files = dirInfo.GetFileSystemInfos("*", SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
                continue;

            var addonItem = new AddonItem() {Name = dirInfo.Name};
            var additionalFiles = new List<string>();
            var additionalConfigFiles = new List<string>();

            bool IsConfigType(string extension) => configExtensions.Any(ext => extension.Equals(ext, StringComparison.OrdinalIgnoreCase));

            foreach (var file in files)
            {
                if (file.Extension.Equals(".addon32", StringComparison.OrdinalIgnoreCase))
                    addonItem.X32Path = file.FullName;
                else if (file.Extension.Equals(".addon64", StringComparison.OrdinalIgnoreCase))
                    addonItem.X64Path = file.FullName;
                else if (file.Name == "Shaders")
                    addonItem.ShadersPath = file.FullName;
                else if (file.Name == "Textures")
                    addonItem.TexturesPath = file.FullName;
                else if (IsConfigType(file.Extension))
                    additionalConfigFiles.Add(file.FullName);
                else
                    additionalFiles.Add(file.FullName);
            }
            
            addonItem.AdditionalFiles = additionalFiles;
            addonItem.AdditionalConfigFiles = additionalConfigFiles;
            addons.Add(addonItem);
        }

        return addons.OrderBy(x => x.Name).ToArray();
    }

    /// <summary>
    /// Deploys the specified addons to the target executable's directory.
    /// Selects the appropriate .addon32 or .addon64 file based on the executable's architecture.
    /// </summary>
    /// <param name="context">The target executable context.</param>
    /// <param name="addons">list of addons to deploy.</param>
    public void Deploy(ExecutableContext context, IList<AddonItem> addons)
    {
        if (!Directory.Exists(Paths.Addons))
            return;

        foreach (var addon in addons)
        {
            if (addon.HasAnyAddon)
            {
                string filePath = context.IsX64 ? addon.X64Path : addon.X32Path;

                if (File.Exists(filePath))
                {
                    string destinationPath = Path.Combine(context.DirectoryPath, Path.GetFileName(filePath));
                    File.CreateSymbolicLink(destinationPath, filePath);
                }
                else
                {
                    throw new FileNotFoundException($"'{Path.GetFileName(filePath)}' not found in Add-ons directory. The target game is a {(context.IsX64 ? "64-bit" : "32-bit")} executable, but the {Path.GetExtension(filePath)} file is missing.");
                }
            }

            if (addon.AdditionalFiles != null)
            {
                foreach (var filePath in addon.AdditionalFiles)
                {
                    if (Path.GetFileName(filePath) == "Shaders" || Path.GetFileName(filePath) == "Textures")
                        continue;

                    string destinationPath = Path.Combine(context.DirectoryPath, Path.GetFileName(filePath));

                    // Check if a symlink already exists at the destination to prevent conflicts
                    // If it points to the same file, skip; otherwise throw an error to prevent overwriting
                    if (File.Exists(destinationPath) && File.GetAttributes(destinationPath).HasFlag(FileAttributes.ReparsePoint))
                    {
                        string target = new string(File.ReadAllText(destinationPath).SkipWhile(c => c != '\"').Skip(1).TakeWhile(c => c != '\"').ToArray());
                        if (target == filePath)
                            continue;

                        throw new IOException($"Addon '{addon.Name}' installation failed. '{Path.GetFileName(filePath)}' already exists in the target game's directory.");
                    }

                    if (File.GetAttributes(filePath).HasFlag(FileAttributes.Directory))
                        Directory.CreateSymbolicLink(destinationPath, filePath);
                    else
                        File.CreateSymbolicLink(destinationPath, filePath);
                }
            }

            if (addon.AdditionalConfigFiles != null)
            {
                foreach (var filePath in addon.AdditionalConfigFiles)
                {
                    string destinationPath = Path.Combine(context.DirectoryPath, Path.GetFileName(filePath));
                    File.Copy(filePath, destinationPath, false);
                }
            }
        }
    }
}