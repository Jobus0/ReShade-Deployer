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
    public enum VersionComparisonResult
    {
        Newer,
        Older,
        Equal
    }

    [GeneratedRegex("\\d+(\\.\\d+)*")]
    private static partial Regex VersionNumberPattern();

    [GeneratedRegex("^(0|[1-9]\\d*)\\.(0|[1-9]\\d*)\\.(0|[1-9]\\d*)(?:-((?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\\.(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\\+([0-9a-zA-Z-]+(?:\\.[0-9a-zA-Z-]+)*))?$")]
    private static partial Regex SemanticVersionPattern();
    
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
    /// Compares two version numbers and returns their relationship.
    /// </summary>
    /// <param name="version1">The first version to compare.</param>
    /// <param name="version2">The second version to compare.</param>
    /// <returns>Newer if version1 is newer than version2, Older if version1 is older, Equal if they are the same.</returns>
    public static VersionComparisonResult Compare(string version1, string version2)
    {
        if (string.IsNullOrEmpty(version1))
            return VersionComparisonResult.Older;
        
        if (string.IsNullOrEmpty(version2))
            return VersionComparisonResult.Newer;
        
        var v1Parts = version1.Split('.').Select(int.Parse).ToArray();
        var v2Parts = version2.Split('.').Select(int.Parse).ToArray();
        
        int maxLength = Math.Max(v1Parts.Length, v2Parts.Length);
        
        for (int i = 0; i < maxLength; i++)
        {
            int v1Part = i < v1Parts.Length ? v1Parts[i] : 0;
            int v2Part = i < v2Parts.Length ? v2Parts[i] : 0;
            
            if (v1Part > v2Part)
                return VersionComparisonResult.Newer;
            if (v1Part < v2Part)
                return VersionComparisonResult.Older;
        }
        
        return VersionComparisonResult.Equal;
    }

    /// <summary>
    /// Checks if one version number is higher/newer than another.
    /// </summary>
    /// <param name="version1">The first version to compare.</param>
    /// <param name="version2">The second version to compare.</param>
    /// <returns>True if version1 is newer than version2, false otherwise.</returns>
    public static bool IsNewerThan(string version1, string version2)
    {
        return Compare(version1, version2) == VersionComparisonResult.Newer;
    }

    /// <summary>
    /// Validates if a string is a valid semantic version.
    /// </summary>
    /// <param name="version">The version string to validate.</param>
    /// <returns>True if the string is a valid semantic version, false otherwise.</returns>
    public static bool IsValidSemanticVersion(string version)
    {
        if (string.IsNullOrEmpty(version))
            return false;

        return SemanticVersionPattern().IsMatch(version);
    }
}