using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SevenZipExtractor;

namespace ReShadeDeployer;

public static class ReShadeDownloader
{
    private const string WebsiteUrl = "https://reshade.me";
    
    /// <summary>
    /// Download the latest version of ReShade (including add-on support version) from the official website and extract it into the lib folder.
    /// </summary>
    public static async Task DownloadReShade()
    {
        string websiteContent;
        try
        {
            websiteContent = await DownloadHelper.GetWebsiteContent(WebsiteUrl);
        }
        catch
        {
            WpfMessageBox.Show(UIStrings.ConnectionError, UIStrings.ConnectionError_Title);
            return;
        }
        
        // Make sure the required folders exist
        CreateDirectories();

        string downloadUrl = Regex.Match(websiteContent, "/downloads/\\S*.exe").ToString();
        
        if (TryGetLocalReShadeVersionNumber(out string localVersion) && localVersion == DownloadHelper.ExtractVersionFromText(downloadUrl))
        {
            WpfMessageBox.Show(UIStrings.UpdateError, UIStrings.Update);
            return;
        }
        
        // Download the standard and the add-on supported ReShade files.
        await DownloadAndExtractReShade(WebsiteUrl + downloadUrl, Paths.Dlls);
        await DownloadAndExtractReShade(WebsiteUrl + Regex.Match(websiteContent, "/downloads/\\S*_Addon.exe"), Paths.AddonDlls);
        
        // Download Compatibility.ini
        try
        {
            await DownloadHelper.Download("https://raw.githubusercontent.com/crosire/reshade-shaders/list/Compatibility.ini", Paths.CompatibilityIni);
        }
        catch { /* Ignore. File is nice, but not necessary. */ }
    }

    /// <summary>
    /// Takes an URL to an official ReShade installer, downloads it, and extracts the inner .dll file into the specified folder.
    /// </summary>
    /// <param name="url">URL to an official ReShade installer.</param>
    /// <param name="directoryPath">Directory to extract .dll into.</param>
    private static async Task DownloadAndExtractReShade(string url, string directoryPath)
    {
        string downloadPath = Path.Combine(directoryPath, "ReShade.exe");

        try
        {
            await DownloadHelper.Download(url, downloadPath);
        }
        catch
        {
            WpfMessageBox.Show(string.Format(UIStrings.DownloadError, url), UIStrings.DownloadError_Title);
            return;
        }
        
        // Getting the .dll from the installer executable requires two steps of extraction.
        // First, extract the outer '[0]' archive, which contains the .dll files (x86 and x64).
        // Second, extract the files from said archive into the specified directory.

        const string innerArchiveName = "[0]";
        string innerArchivePath = Path.Combine(directoryPath, innerArchiveName);
        try
        {
            using ArchiveFile archiveFile = new ArchiveFile(downloadPath);
            archiveFile.Extract(e => e.FileName == innerArchiveName
                ? innerArchivePath
                : null);

            using ArchiveFile innerArchiveFile = new ArchiveFile(innerArchivePath);
            innerArchiveFile.Extract(innerEntry => Path.Combine(directoryPath, innerEntry.FileName));
        }
        catch (IOException)
        {
            WpfMessageBox.Show(string.Format(UIStrings.AccessError, url), UIStrings.AccessError_Title);
        }
        catch (Exception e)
        {
            WpfMessageBox.Show(e.GetType() + ": " + e.Message, UIStrings.AccessError_Title);
        }
        finally
        {
            File.Delete(downloadPath);
            File.Delete(innerArchivePath);
        }
    }
    
    /// <summary>
    /// Create all the directories used by ReShade Deployer.
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
    public static bool TryGetLocalReShadeVersionNumber(out string version)
    {
        string path = Path.Combine(Paths.Dlls, "ReShade64.dll");
        if (File.Exists(path))
        {
            string fileVersion = FileVersionInfo.GetVersionInfo(path).FileVersion!;
            version = fileVersion.Substring(0, fileVersion.LastIndexOf('.'));
            return true;
        }

        version = string.Empty;
        return false;
    }
    
    /// <summary>
    /// Get the latest version number of ReShade from the official website. Throws an exception if the website is not reachable.
    /// </summary>
    /// <returns>Latest version number, formatted like "1.0.0".</returns>
    public static async Task<string> GetLatestOnlineReShadeVersionNumber()
    {
        var websiteContent = await DownloadHelper.GetWebsiteContent(WebsiteUrl);
        return DownloadHelper.ExtractVersionFromText(Regex.Match(websiteContent, "/downloads/\\S*.exe").ToString());
    }
}