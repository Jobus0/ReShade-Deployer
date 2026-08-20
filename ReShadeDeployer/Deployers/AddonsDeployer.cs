using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using IniParser.Model.Configuration;
using IniParser.Parser;

namespace ReShadeDeployer;

/// <summary>
/// Handles discovery and deployment of ReShade addons.
/// </summary>
public class AddonsDeployer(IFileSystem fileSystem, FileHelper fileHelper)
{
    private readonly string[] configExtensions = [".cfg", ".ini", ".xml", ".json", ".yaml", ".yml"];

    // The reserved config file name that carries per-addon deployment options.
    private const string AddonConfigFileName = "Add-on.ini";

    /// <summary>
    /// Gets a list of available addon names from the configured Add-ons directory.
    /// .addon32 and .addon64 files with the same name are grouped together.
    /// </summary>
    /// <returns>A sorted list of AddonItem.</returns>
    public IList<AddonItem> GetAvailableAddons()
    {
        if (!fileSystem.Directory.Exists(Paths.Addons))
            return Array.Empty<AddonItem>();

        var addons = new List<AddonItem>();

        
        var addonsDirInfo = fileSystem.DirectoryInfo.New(Paths.Addons);
        var topFiles = addonsDirInfo.GetFileSystemInfos("*.addon*", SearchOption.TopDirectoryOnly);
        foreach (var file in topFiles)
        {
            string name = fileSystem.Path.GetFileNameWithoutExtension(file.Name);

            bool isAddon32 = file.Extension.Equals(".addon32", StringComparison.OrdinalIgnoreCase);
            bool isAddon64 = file.Extension.Equals(".addon64", StringComparison.OrdinalIgnoreCase);
            if (isAddon32 || isAddon64)
            {
                var addonItem = addons.FirstOrDefault(x => x.Name == name);
                if (addonItem == null)
                {
                    addonItem = new AddonItem() { Name = name };
                    addons.Add(addonItem);
                }

                if (isAddon32)
                    addonItem.X32Paths.Add(file.FullName);
                else if (isAddon64)
                    addonItem.X64Paths.Add(file.FullName);
            }
        }

        var directories = addonsDirInfo.GetFileSystemInfos("*", SearchOption.TopDirectoryOnly).Where(fsi => fsi is IDirectoryInfo);
        foreach (var directory in directories)
        {
            var dirInfo = (IDirectoryInfo)directory;
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
                    addonItem.X32Paths.Add(file.FullName);
                else if (file.Extension.Equals(".addon64", StringComparison.OrdinalIgnoreCase))
                    addonItem.X64Paths.Add(file.FullName);
                else if (file.Name.Equals("Shaders", StringComparison.OrdinalIgnoreCase))
                    addonItem.ShadersPath = file.FullName;
                else if (file.Name.Equals("Textures", StringComparison.OrdinalIgnoreCase))
                    addonItem.TexturesPath = file.FullName;
                else if (file.Name.Equals(AddonConfigFileName, StringComparison.OrdinalIgnoreCase))
                    ReadAddonConfig(addonItem, file.FullName);  // parse but do NOT add to config/additional lists
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

    private static readonly IniDataParser AddonIniParser = new(new IniParserConfiguration
    {
        AllowKeysWithoutSection = true,
        SkipInvalidLines = true,
    });

    /// <summary>
    /// Parses an Add-on.ini file and stores its recognised fields on <paramref name="addonItem"/>.
    /// The file has no section headers; all keys live at the top level.
    /// All fields are optional; unrecognised keys are silently ignored.
    /// </summary>
    private void ReadAddonConfig(AddonItem addonItem, string iniPath)
    {
        var data = AddonIniParser.Parse(fileSystem.File.ReadAllText(iniPath));

        // Keys without a section are collected under the "Global" section by IniParser
        var section = data.Global;

        if (section.ContainsKey("SetupFile"))
            addonItem.SetupFile = section["SetupFile"];

        if (section.ContainsKey("ReShadeDllNameOverride"))
            addonItem.ReShadeDllNameOverride = section["ReShadeDllNameOverride"];

        if (section.ContainsKey("OverrideCopy"))
        {
            addonItem.OverrideCopy = section["OverrideCopy"] switch
            {
                var s when s.Equals("AlwaysCopy", StringComparison.OrdinalIgnoreCase) => OverrideCopyMode.AlwaysCopy,
                var s when s.Equals("AlwaysSymlinks", StringComparison.OrdinalIgnoreCase) => OverrideCopyMode.AlwaysSymlinks,
                _ => OverrideCopyMode.Default,
            };
        }
    }

    /// <summary>
    /// Deploys the specified addons to the target executable's directory.
    /// Selects the appropriate .addon32 or .addon64 file based on the executable's architecture.
    /// </summary>
    /// <param name="context">The target executable context.</param>
    /// <param name="addons">list of addons to deploy.</param>
    public void Deploy(ExecutableContext context, IList<AddonItem> addons)
    {
        foreach (var addon in addons)
        {
            bool alwaysCopy     = addon.OverrideCopy == OverrideCopyMode.AlwaysCopy;
            bool alwaysSymlinks = addon.OverrideCopy == OverrideCopyMode.AlwaysSymlinks;

            if (addon.HasAnyAddon)
            {
                var filePaths = context.IsX64 ? addon.X64Paths : addon.X32Paths;
                foreach (var filePath in filePaths)
                {
                    var fileName = fileSystem.Path.GetFileName(filePath);
                    string destinationPath = fileSystem.Path.Combine(context.DirectoryPath, fileName);

                    if (alwaysCopy)
                        fileSystem.File.Copy(filePath, destinationPath, false);
                    else
                        fileSystem.File.CreateSymbolicLink(destinationPath, filePath);
                }
            }

            if (addon.AdditionalFiles != null)
            {
                foreach (var filePath in addon.AdditionalFiles)
                {
                    string fileName = fileSystem.Path.GetFileName(filePath);

                    if (fileName is "Shaders" or "Textures")
                        continue;

                    string destinationPath = fileSystem.Path.Combine(context.DirectoryPath, fileName);

                    // Check if a symlink already exists at the destination to prevent conflicts
                    // If it points to the same file, skip; otherwise throw an error to prevent overwriting
                    if (fileSystem.File.Exists(destinationPath))
                    {
                        // If the destination is a symlink pointing to the same file, skip
                        if (fileHelper.ResolveLinkPath(destinationPath) == filePath)
                            continue;

                        throw new IOException($"Addon '{addon.Name}' installation failed. '{fileName}' already exists in the target game's directory.");
                    }

                    if (alwaysCopy)
                        fileHelper.Copy(filePath, destinationPath, false);
                    else
                        fileHelper.CreateSymbolicLink(destinationPath, filePath);
                }
            }

            if (addon.AdditionalConfigFiles != null)
            {
                foreach (var filePath in addon.AdditionalConfigFiles)
                {
                    string destinationPath = fileSystem.Path.Combine(context.DirectoryPath, fileSystem.Path.GetFileName(filePath));

                    if (!fileSystem.File.Exists(destinationPath))
                    {
                        if (alwaysSymlinks)
                            fileHelper.CreateSymbolicLink(destinationPath, filePath);
                        else
                            fileSystem.File.Copy(filePath, destinationPath, false);
                    }
                }
            }

            // Run the optional setup script after all files for this addon are deployed
            if (!string.IsNullOrEmpty(addon.SetupFile))
            {
                string setupFilePath = fileSystem.Path.Combine(context.DirectoryPath, addon.SetupFile);
                RunSetupFile(addon.Name, setupFilePath);
            }
        }
    }

    /// <summary>
    /// Launches a setup script (.bat, .ps1, .exe) and waits for it to finish.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private void RunSetupFile(string addonName, string setupFilePath)
    {
        if (!fileSystem.File.Exists(setupFilePath))
            throw new FileNotFoundException($"Addon '{addonName}' setup file not found: {setupFilePath}");

        string workingDirectory = fileSystem.Path.GetDirectoryName(setupFilePath)!;
        string extension = fileSystem.Path.GetExtension(setupFilePath).ToLowerInvariant();

        System.Diagnostics.ProcessStartInfo psi = extension switch
        {
            ".ps1" => new System.Diagnostics.ProcessStartInfo
            {
                FileName        = "powershell.exe",
                Arguments       = $"-ExecutionPolicy Bypass -File \"{setupFilePath}\"",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
            },
            ".bat" or ".cmd" => new System.Diagnostics.ProcessStartInfo
            {
                FileName        = "cmd.exe",
                Arguments       = $"/c \"{setupFilePath}\"",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
            },
            _ => new System.Diagnostics.ProcessStartInfo
            {
                FileName        = setupFilePath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = true,
            },
        };

        using var process = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException($"Addon '{addonName}': failed to start setup file '{setupFilePath}'.");

        process.WaitForExit();
    }
}