using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Testably.Abstractions;

namespace ReShadeDeployer;

public class ReShadeUpdater(IFileSystem fileSystem, FileHelper fileHelper, IDownloadService downloadService, IMessageBox messageBox)
{
    private const string WebsiteUrl = "https://reshade.me";
    
    /// <summary>
    /// Download the latest version of ReShade (including add-on support version) from the official website and extract it into the lib folder.
    /// </summary>
    public async Task Update(string? specificVersion = null)
    {
        // Make sure the required folders exist
        CreateDirectories();

        if (specificVersion != null)
        {
            string standardUrl = $"https://reshade.me/downloads/ReShade_Setup_{specificVersion}.exe";
            string addonUrl = $"https://reshade.me/downloads/ReShade_Setup_{specificVersion}_Addon.exe";

            if (await DownloadAndExtractReShade(standardUrl, Paths.Dlls))
            {
                await DownloadAndExtractReShade(addonUrl, Paths.AddonDlls);
            }
        }
        else
        {
            string websiteContent;
            try
            {
                websiteContent = await downloadService.GetWebsiteContent(WebsiteUrl);
            }
            catch
            {
                messageBox.Show(UIStrings.ConnectionError, UIStrings.ConnectionError_Title);
                return;
            }

            string downloadUrl = Regex.Match(websiteContent, "/downloads/\\S*.exe").ToString();
            
            if (TryGetLocalReShadeVersionNumber(out string localVersion) && localVersion == VersionParser.Parse(downloadUrl))
            {
                messageBox.Show(UIStrings.UpdateError, UIStrings.UpdateUnavailable_Title);
                return;
            }
            
            // Download the standard and the add-on supported ReShade files.
            if (await DownloadAndExtractReShade(WebsiteUrl + downloadUrl, Paths.Dlls))
            {
                await DownloadAndExtractReShade(WebsiteUrl + Regex.Match(websiteContent, "/downloads/\\S*_Addon.exe"), Paths.AddonDlls);
            }
        }
        
        // Download Compatibility.ini
        try
        {
            await downloadService.Download("https://raw.githubusercontent.com/crosire/reshade-shaders/list/Compatibility.ini", Paths.CompatibilityIni);
        }
        catch { /* Ignore. File is nice, but not necessary. */ }
    }

    /// <summary>
    /// Takes an URL to an official ReShade installer, downloads it, and extracts the inner .dll file into the specified folder.
    /// </summary>
    /// <param name="url">URL to an official ReShade installer.</param>
    /// <param name="directoryPath">Directory to extract .dll into.</param>
    private async Task<bool> DownloadAndExtractReShade(string url, string directoryPath)
    {
        string downloadPath = fileSystem.Path.Combine(directoryPath, "ReShade.exe");

        try
        {
            await downloadService.Download(url, downloadPath);
        }
        catch
        {
            messageBox.Show(string.Format(UIStrings.DownloadError, url), UIStrings.DownloadError_Title);
            return false;
        }

        try
        {
            MemoryStream output;
            await using (FileSystemStream input = fileSystem.File.OpenRead(downloadPath))
            {
                output = new MemoryStream((int)input.Length);

                byte[] block = new byte[512];
                byte[] signature = { 0x50, 0x4B, 0x03, 0x04 }; // PK..

                // Look for archive at the end of this executable and copy it to a memory stream
                while (input.Read(block, 0, block.Length) >= signature.Length)
                {
                    if (block.Take(signature.Length).SequenceEqual(signature) && block.Skip(signature.Length).Take(26).Max() != 0)
                    {
                        output.Write(block, 0, block.Length);
                        input.CopyTo(output);
                        break;
                    }
                }
            }
            
            IZipArchive zip = fileSystem.ZipArchive().New(output, ZipArchiveMode.Read, false);
            
            // Validate archive contains the ReShade DLLs
            if (zip.GetEntry("ReShade32.dll") == null || zip.GetEntry("ReShade64.dll") == null)
            {
                throw new InvalidDataException();
            }

            foreach (var entry in zip.Entries)
            {
                string path = fileSystem.Path.Combine(directoryPath, entry.FullName);
                
                if (fileSystem.File.Exists(path) && fileHelper.IsFileLocked(path))
                    fileSystem.File.Move(path, path + ".oldver", overwrite: true);
                
                fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(path)!);
                entry.ExtractToFile(path, true);
            }
            zip.ExtractToDirectory(directoryPath, true);
            return true;
        }
        catch (Exception ex)
        {
            messageBox.Show(ex.GetType() + ": " + ex.Message, UIStrings.ExtractionError_Title);
            return false;
        }
        finally
        {
            fileSystem.File.Delete(downloadPath);
        }
    }
    
    /// <summary>
    /// Create all the directories used by ReShade Deployer.
    /// </summary>
    public void CreateDirectories()
    {
        fileSystem.Directory.CreateDirectory(Paths.Shaders);
        fileSystem.Directory.CreateDirectory(Paths.Textures);
        fileSystem.Directory.CreateDirectory(Paths.Addons);
        fileSystem.Directory.CreateDirectory(Paths.Dlls);
        fileSystem.Directory.CreateDirectory(Paths.AddonDlls);
        fileSystem.Directory.CreateDirectory(Paths.Cache);
    }
    
    /// <summary>
    /// Check the file attribute of the downloaded ReShade DLL and get its version.
    /// </summary>
    /// <param name="version">The version string of the ReShade DLL.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    public bool TryGetLocalReShadeVersionNumber(out string version)
    {
        string path = fileSystem.Path.Combine(Paths.Dlls, "ReShade64.dll");
        if (fileSystem.File.Exists(path))
        {
            string fileVersion = fileSystem.FileVersionInfo.GetVersionInfo(path).FileVersion!;
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
    public async Task<string> GetLatestOnlineReShadeVersionNumber()
    {
        var websiteContent = await downloadService.GetWebsiteContent(WebsiteUrl);
        return VersionParser.Parse(Regex.Match(websiteContent, "/downloads/\\S*.exe").ToString());
    }
}