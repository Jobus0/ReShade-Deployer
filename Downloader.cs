using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using SevenZipExtractor;

namespace ReShadeInstaller;

/// <summary>
/// Provides methods for downloading and extracting official ReShade files.
/// </summary>
public static class Downloader
{
    public static async Task DownloadReShade()
    {
        string websiteUrl = "https://reshade.me";
        string input;
        try
        {
            using HttpClient httpClient = new HttpClient();
            input = await httpClient.GetStringAsync(websiteUrl);
        }
        catch
        {
            MessageBox.Show("ReShade website (https://reshade.me) is down or the connection was blocked.");
            throw;
        }
        
        CreateDirectories();

        await DownloadAndExtract(websiteUrl + Regex.Match(input, "/downloads/\\S*.exe"), Paths.Dlls);
        await DownloadAndExtract(websiteUrl + Regex.Match(input, "/downloads/\\S*_Addon.exe"), Paths.AddonDlls);
    }

    private static async Task DownloadAndExtract(string url, string directoryPath)
    {
        string zipPath = Path.Combine(directoryPath, "ReShade.exe");
        try
        {
            using HttpClient httpClient = new HttpClient();
            await using var httpStream = await httpClient.GetStreamAsync(url);
            await using var fileStream = new FileStream(zipPath, FileMode.CreateNew);
            await httpStream.CopyToAsync(fileStream);
        }
        catch
        {
            MessageBox.Show($"Failed to access '{url}'. Please report to the developer.");
            throw;
        }

        const string innerArchiveName = "[0]";
        string innerArchivePath = Path.Combine(directoryPath, innerArchiveName);
        try
        {
            using ArchiveFile archiveFile = new ArchiveFile(zipPath);
            archiveFile.Entries.First(e => e.FileName == innerArchiveName).Extract(innerArchivePath);
            archiveFile.Extract(e => e.FileName == innerArchiveName ? e.FileName : null);

            using ArchiveFile innerArchiveFile = new ArchiveFile(innerArchivePath);
            innerArchiveFile.Extract(innerEntry => innerEntry.FileName is "ReShade32.dll" or "ReShade64.dll"
                ? Path.Combine(directoryPath, innerEntry.FileName)
                : null);
        }
        finally
        {
            File.Delete(zipPath);
            File.Delete(innerArchivePath);
        }
    }
    
    /// <summary>
    /// Create all the directories used by ReShade Installer.
    /// </summary>
    public static void CreateDirectories()
    {
        Directory.CreateDirectory(Paths.Shaders);
        Directory.CreateDirectory(Paths.Textures);
        Directory.CreateDirectory(Paths.Dlls);
        Directory.CreateDirectory(Paths.AddonDlls);
        Directory.CreateDirectory(Paths.Cache);
    }
    
    /// <summary>
    /// Check the file attribute of the downloaded ReShade DLL and get its version.
    /// </summary>
    /// <param name="version">The version string of the ReShade DLL.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    public static bool TryGetInstalledReShadeVersion(out string? version)
    {
        string path = Path.Combine(Paths.Dlls, "ReShade64.dll");
        if (File.Exists(path))
        {
            string fileVersion = FileVersionInfo.GetVersionInfo(path).FileVersion!;
            version = fileVersion.Substring(0, fileVersion.LastIndexOf('.'));
            return true;
        }

        version = null;
        return false;
    }
}