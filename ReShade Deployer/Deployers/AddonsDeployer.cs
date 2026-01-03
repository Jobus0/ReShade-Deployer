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
    /// Gets a list of available addon names from the configured Addons directory.
    /// .addon32 and .addon64 files with the same name are grouped together.
    /// </summary>
    /// <returns>A sorted list of unique addon names.</returns>
    public IEnumerable<string> GetAvailableAddons()
    {
        if (!Directory.Exists(Paths.Addons))
            return Enumerable.Empty<string>();

        var files = Directory.GetFiles(Paths.Addons, "*.addon*", SearchOption.AllDirectories);
        
        var addons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (file.EndsWith(".addon32", StringComparison.OrdinalIgnoreCase) || 
                file.EndsWith(".addon64", StringComparison.OrdinalIgnoreCase))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                addons.Add(name);
            }
        }

        return addons.OrderBy(x => x);
    }

    /// <summary>
    /// Deploys the specified addons to the target executable's directory.
    /// Selects the appropriate .addon32 or .addon64 file based on the executable's architecture.
    /// </summary>
    /// <param name="context">The target executable context.</param>
    /// <param name="addons">list of addon names to deploy.</param>
    public void Deploy(ExecutableContext context, IEnumerable<string> addons)
    {
        if (!Directory.Exists(Paths.Addons))
            return;

        foreach (var addonName in addons)
        {
            string extension = context.IsX64 ? ".addon64" : ".addon32";
            string fileName = addonName + extension;
            
            // Search for the file in the Addons directory (recursive)
            // We need to find the specific file because we don't know its subfolder
            string? sourceFile = Directory.GetFiles(Paths.Addons, fileName, SearchOption.AllDirectories).FirstOrDefault();

            if (sourceFile != null && File.Exists(sourceFile))
            {
                string destinationPath = Path.Combine(context.DirectoryPath, fileName);
                SymbolicLink.CreateSymbolicLink(destinationPath, sourceFile, 0);
            }
            else
            {
                throw new FileNotFoundException($"Add-on '{addonName}' not found in Add-ons directory. The target game is a {(context.IsX64 ? "64-bit" : "32-bit")} executable, but the {(context.IsX64 ? ".addon64" : ".addon32")} file is missing.");
            }
        }
    }
}
