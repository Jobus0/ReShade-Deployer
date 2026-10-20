namespace ReShadeDeployer;

/// <summary>
/// Validation status of a local ReShade DLL file.
/// </summary>
public enum ReShadeDllStatus
{
    /// <summary>The file exists, is a genuine ReShade DLL, and matches the highest local ReShade version.</summary>
    Valid,
    
    /// <summary>The file does not exist.</summary>
    Missing,
    
    /// <summary>The file exists, but is not a ReShade DLL (its file description doesn't contain "ReShade").</summary>
    InvalidFile,
    
    /// <summary>The file is a ReShade DLL, but its version differs from the highest local ReShade version.</summary>
    VersionMismatch
}

/// <summary>
/// Name, version and validation status of one of the local ReShade DLL files.
/// </summary>
/// <param name="Name">Display name of the DLL, e.g. "ReShade64" or "ReShade32 (Addon Support)".</param>
/// <param name="Version">Version of the DLL (e.g. "6.3.0"), or null if the file is missing or not a valid ReShade DLL.</param>
/// <param name="Status">Validation status of the DLL.</param>
public record ReShadeDllInfo(string Name, string? Version, ReShadeDllStatus Status);
