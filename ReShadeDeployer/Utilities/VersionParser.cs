using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReShadeDeployer;

/// <summary>
/// Provides utility methods for parsing version numbers from strings, such as download URLs.
/// This class utilizes a generated regular expression pattern to match version numbers 
/// formatted as sequences of digits separated by periods (e.g., "1.0.0").
/// </summary>
public static partial class VersionParser
{
    [GeneratedRegex("\\d+(\\.\\d+)*")]
    private static partial Regex VersionNumberPattern();
    
    /// <summary>
    /// Gets the version number from a string like a download URL.
    /// </summary>
    /// <param name="text">The string to extract the version number from.</param>
    /// <returns>The version number extracted from the string.</returns>
    public static string Parse(string text)
    {
        return VersionNumberPattern().Match(text).ToString();
    }
    
    /// <summary>
    /// Checks if one version number is higher/newer than another.
    /// </summary>
    /// <param name="version1">The first version to compare.</param>
    /// <param name="version2">The second version to compare.</param>
    /// <returns>True if version1 is newer than version2, false otherwise.</returns>
    public static bool IsNewerThan(string version1, string version2)
    {
        if (string.IsNullOrEmpty(version1))
            return false;
        
        if (string.IsNullOrEmpty(version2))
            return true;
        
        var v1Parts = version1.Split('.').Select(int.Parse).ToArray();
        var v2Parts = version2.Split('.').Select(int.Parse).ToArray();
        
        int maxLength = Math.Max(v1Parts.Length, v2Parts.Length);
        
        for (int i = 0; i < maxLength; i++)
        {
            int v1Part = i < v1Parts.Length ? v1Parts[i] : 0;
            int v2Part = i < v2Parts.Length ? v2Parts[i] : 0;
            
            if (v1Part > v2Part)
                return true;
            if (v1Part < v2Part)
                return false;
        }
        
        return false;
    }
}