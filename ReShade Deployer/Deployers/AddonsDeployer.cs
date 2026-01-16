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
    /// <summary>
    /// Gets a list of available addon names from the configured Add-ons directory.
    /// .addon32 and .addon64 files with the same name are grouped together.
    /// </summary>
    /// <returns>A sorted list of AddonItem.</returns>
    public IList<AddonItem> GetAvailableAddons()
    {
        if (!Directory.Exists(Paths.Addons))
            return Array.Empty<AddonItem>();

        var files = Directory.GetFiles(Paths.Addons, "*.addon*", SearchOption.AllDirectories);
        
        var addons = new List<AddonItem>();

        foreach (var file in files)
        {
            bool isAddon32 = file.EndsWith(".addon32", StringComparison.OrdinalIgnoreCase);
            bool isAddon64 = file.EndsWith(".addon64", StringComparison.OrdinalIgnoreCase);
            if (isAddon32 || isAddon64)
            {
                string name = Path.GetFileNameWithoutExtension(file);

                var addonItem = addons.FirstOrDefault(x => x.Name == name);
                if (addonItem != null)
                {
                    if (isAddon32)
                        addonItem.X32Path = file;
                    else if (isAddon64)
                        addonItem.X64Path = file;
                }
                else
                {
                    addonItem = new AddonItem() {Name = name};
                    
                    if (isAddon32)
                        addonItem.X32Path = file;
                    else if (isAddon64)
                        addonItem.X64Path = file;
                    
                    addons.Add(addonItem);
                }
            }
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
            string filePath = context.IsX64 ? addon.X64Path : addon.X32Path;
            
            if (File.Exists(filePath))
            {
                string destinationPath = Path.Combine(context.DirectoryPath, Path.GetFileName(filePath));
                SymbolicLink.CreateSymbolicLink(destinationPath, filePath, 0);
            }
            else
            {
                throw new FileNotFoundException($"'{Path.GetFileName(filePath)}' not found in Add-ons directory. The target game is a {(context.IsX64 ? "64-bit" : "32-bit")} executable, but the {Path.GetExtension(filePath)} file is missing.");
            }
        }
    }
}
