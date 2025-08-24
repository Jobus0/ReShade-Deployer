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
}