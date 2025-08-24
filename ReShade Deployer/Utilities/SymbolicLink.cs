using System.Runtime.InteropServices;

namespace ReShadeDeployer;

public static class SymbolicLink
{
    /// <summary>
    /// Create a symbolic link to a file or directory.
    /// </summary>
    /// <param name="lpSymlinkFileName">The name of the symbolic link file.</param>
    /// <param name="lpTargetFileName">The name of the target file.</param>
    /// <param name="dwFlags">0 = File. 1 = Directory.</param>
    /// <returns>True if the symbolic link was successfully created, otherwise false.</returns>
    [DllImport("kernel32.dll")]
    public static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);
}