using System.IO;

namespace ReShadeDeployer;

public static class FileHelper
{
    /// <summary>
    /// Check if a file is locked due to being in use.
    /// </summary>
    /// <param name="path">Path to the file to check.</param>
    /// <returns>Whether the file is locked or not.</returns>
    public static bool IsFileLocked(string path)
    {
        try
        {
            using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                stream.Close();
        }
        catch (IOException)
        {
            //the file is unavailable because it is:
            //still being written to
            //or being processed by another thread
            //or does not exist (has already been processed)
            return true;
        }

        //file is not locked
        return false;
    }

    /// <summary>
    /// Get FileSystemInfo for a path, handling both files and directories.
    /// </summary>
    /// <param name="path">Path to the file or directory.</param>
    /// <returns>FileInfo or DirectoryInfo depending on the path type.</returns>
    public static FileSystemInfo GetFileSystemInfo(string path)
    {
        return File.GetAttributes(path).HasFlag(FileAttributes.Directory)
            ? new DirectoryInfo(path)
            : new FileInfo(path);
    }
    
    /// <summary>
    /// Creates a file or directory (depending on the target) symbolic link identified by path that points to pathToTarget.
    /// </summary>
    /// <param name="path">The path where the symbolic link should be created.</param>
    /// <param name="pathToTarget">The path of the target to which the symbolic link points.</param>
    public static void CreateSymbolicLink(string path, string pathToTarget)
    {
        if (File.GetAttributes(pathToTarget).HasFlag(FileAttributes.Directory))
            Directory.CreateSymbolicLink(path, pathToTarget);
        else
            File.CreateSymbolicLink(path, pathToTarget);
    }

    /// <summary>
    /// Gets the target path of the link located in linkPath, or null if the file or directory at linkPath doesn't represent a link.
    /// </summary>
    public static string? ResolveLinkPath(string linkPath)
    {
        return GetFileSystemInfo(linkPath).LinkTarget;
    }
}