using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReShadeDeployer;

public class ReShadeDownloader(DownloadService downloadService, IMessageBox messageBox)
{
    private const string WebsiteUrl = "https://reshade.me";
    
    /// <summary>
    /// Download the latest version of ReShade (including add-on support version) from the official website and extract it into the lib folder.
    /// </summary>
    public async Task DownloadReShade()
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
        
        // Make sure the required folders exist
        CreateDirectories();

        string downloadUrl = Regex.Match(websiteContent, "/downloads/\\S*.exe").ToString();
        
        if (TryGetLocalReShadeVersionNumber(out string localVersion) && localVersion == VersionParser.Parse(downloadUrl))
        {
            messageBox.Show(UIStrings.UpdateError, UIStrings.Update);
            return;
        }
        
        // Download the standard and the add-on supported ReShade files.
        await DownloadAndExtractReShade(WebsiteUrl + downloadUrl, Paths.Dlls);
        await DownloadAndExtractReShade(WebsiteUrl + Regex.Match(websiteContent, "/downloads/\\S*_Addon.exe"), Paths.AddonDlls);
        
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
    private async Task DownloadAndExtractReShade(string url, string directoryPath)
    {
        string downloadPath = Path.Combine(directoryPath, "ReShade.exe");

        try
        {
            await downloadService.Download(url, downloadPath);
        }
        catch
        {
            messageBox.Show(string.Format(UIStrings.DownloadError, url), UIStrings.DownloadError_Title);
            return;
        }

        try
        {
            MemoryStream output;
            await using (FileStream input = File.OpenRead(downloadPath))
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
            
            ZipArchive zip = new ZipArchive(output, ZipArchiveMode.Read, false);
            
            // Validate archive contains the ReShade DLLs
            if (zip.GetEntry("ReShade32.dll") == null || zip.GetEntry("ReShade64.dll") == null)
            {
                throw new InvalidDataException();
            }

            foreach (var entry in zip.Entries)
            {
                string path = Path.Combine(directoryPath, entry.FullName);
                
                if (File.Exists(path) && FileHelper.IsFileLocked(path))
                    File.Move(path, path + ".oldver", overwrite: true);
                
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                entry.ExtractToFile(path, true);
            }
            zip.ExtractToDirectory(directoryPath, true);
        }
        catch (Exception e)
        {
            messageBox.Show(e.GetType() + ": " + e.Message, UIStrings.ExtractionError_Title);
        }
        finally
        {
            File.Delete(downloadPath);
        }
    }
    
    /// <summary>
    /// Create all the directories used by ReShade Deployer.
    /// </summary>
    public void CreateDirectories()
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
    public bool TryGetLocalReShadeVersionNumber(out string version)
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
    public async Task<string> GetLatestOnlineReShadeVersionNumber()
    {
        var websiteContent = await downloadService.GetWebsiteContent(WebsiteUrl);
        return VersionParser.Parse(Regex.Match(websiteContent, "/downloads/\\S*.exe").ToString());
    }
}