using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace ReShadeDeployer;

public class FileHelper(IFileSystem fileSystem)
{
    /// <summary>
    /// Check if a file is locked due to being in use.
    /// </summary>
    /// <param name="path">Path to the file to check.</param>
    /// <returns>Whether the file is locked or not.</returns>
    public bool IsFileLocked(string path)
    {
        try
        {
            using (FileSystemStream stream = fileSystem.File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
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
    public IFileSystemInfo GetFileSystemInfo(string path)
    {
        return fileSystem.File.GetAttributes(path).HasFlag(FileAttributes.Directory)
            ? fileSystem.DirectoryInfo.New(path)
            : fileSystem.FileInfo.New(path);
    }
    
    /// <summary>
    /// Creates a file or directory (depending on the target) symbolic link identified by path that points to pathToTarget.
    /// </summary>
    /// <param name="path">The path where the symbolic link should be created.</param>
    /// <param name="pathToTarget">The path of the target to which the symbolic link points.</param>
    public void CreateSymbolicLink(string path, string pathToTarget)
    {
        if (fileSystem.File.GetAttributes(pathToTarget).HasFlag(FileAttributes.Directory))
            fileSystem.Directory.CreateSymbolicLink(path, pathToTarget);
        else
            fileSystem.File.CreateSymbolicLink(path, pathToTarget);
    }

    /// <summary>
    /// Gets the target path of the link located in linkPath, or null if the file or directory at linkPath doesn't represent a link.
    /// </summary>
    public string? ResolveLinkPath(string linkPath)
    {
        return GetFileSystemInfo(linkPath).LinkTarget;
    }

    /// <summary>
    /// Copies a file or directory from sourcePath to destinationPath.
    /// </summary>
    /// <param name="sourcePath">Path to the source file or directory.</param>
    /// <param name="destinationPath">Path to the destination file or directory.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    public void Copy(string sourcePath, string destinationPath, bool overwrite)
    {
        if (fileSystem.File.GetAttributes(sourcePath).HasFlag(FileAttributes.Directory))
        {
            fileSystem.Directory.CreateDirectory(destinationPath);
            foreach (string dirPath in fileSystem.Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                fileSystem.Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));
            foreach (string filePath in fileSystem.Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
                fileSystem.File.Copy(filePath, filePath.Replace(sourcePath, destinationPath), overwrite);
        }
        else
        {
            fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(destinationPath) ?? destinationPath);
            fileSystem.File.Copy(sourcePath, destinationPath, overwrite);
        }
    }

    /// <summary>
    /// Checks whether two files exist and have identical contents.
    /// </summary>
    /// <param name="path1">Path to the first file.</param>
    /// <param name="path2">Path to the second file.</param>
    /// <returns>True if both files exist and their contents are identical; otherwise, false.</returns>
    public bool AreFilesEqual(string path1, string path2)
    {
        if (string.IsNullOrEmpty(path1) || string.IsNullOrEmpty(path2))
            return false;

        if (!fileSystem.File.Exists(path1) || !fileSystem.File.Exists(path2))
            return false;

        if (string.Equals(path1, path2, StringComparison.OrdinalIgnoreCase))
            return true;

        var fileInfo1 = fileSystem.FileInfo.New(path1);
        var fileInfo2 = fileSystem.FileInfo.New(path2);

        if (fileInfo1.Length != fileInfo2.Length)
            return false;

        byte[] file1Bytes = fileSystem.File.ReadAllBytes(path1);
        byte[] file2Bytes = fileSystem.File.ReadAllBytes(path2);

        return file1Bytes.AsSpan().SequenceEqual(file2Bytes);
    }
}