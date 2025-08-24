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
}