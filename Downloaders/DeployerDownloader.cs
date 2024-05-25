using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using SevenZipExtractor;

namespace ReShadeDeployer;

public static class DeployerDownloader
{
    private const string GithubUrl = "https://api.github.com/repos/Jobus0/ReShade-Deployer/releases/latest";
    
    /// <summary>
    /// Download the latest version of ReShade Deployer from GitHub.
    /// </summary>
    public static async void UpdateDeployer()
    {
        // Fetch latest version of ReShade Deployer from GitHub
        string content = await DownloadHelper.GetWebsiteContent(GithubUrl);
        string downloadUrl = Regex.Match(content, @"""browser_download_url"":\s?""(\S*?)""").Groups[1].ToString();
        string downloadPath = Path.Combine(Paths.Lib, "ReShade-Deployer.zip");

        try
        {
            await DownloadHelper.Download(downloadUrl, downloadPath);
        }
        catch
        {
            WpfMessageBox.Show(string.Format(UIStrings.DownloadError, downloadUrl), UIStrings.DownloadError_Title);
            return;
        }
        
        // Rename the current exe because we cannot overwrite it while it is running
        string currentExe = Process.GetCurrentProcess().MainModule!.FileName;
        string oldExe = currentExe + ".oldver";
        File.Move(currentExe, oldExe, overwrite: true);
        
        // Extract the new version of ReShade Deployer
        using (ArchiveFile archiveFile = new ArchiveFile(downloadPath))
            archiveFile.Extract(e => Path.Combine(Paths.Root, e.FileName));
        
        // Delete the downloaded archive
        File.Delete(downloadPath);
        
        // Run the new version of ReShade Deployer
        var startupArgs = ((App)Application.Current).StartupArgs;
        var startInfo = new ProcessStartInfo
        {
            FileName = currentExe,
            Arguments = startupArgs.Length > 0 ? $"\"{startupArgs[0]}\"" : string.Empty,
            UseShellExecute = true,
            Verb = "runas",
        };
        Process.Start(startInfo);
        
        // Close the current (old) version of ReShade Deployer
        Application.Current.Shutdown();
    }
    
    /// <summary>
    /// Get the latest version number of ReShade Deployer from GitHub. Throws an exception if the website is not reachable.
    /// </summary>
    /// <returns>Latest version number, formatted like "1.0.0".</returns>
    public static async Task<string> GetLatestOnlineVersionNumber()
    {
        string content = await DownloadHelper.GetWebsiteContent(GithubUrl);
        string tag = Regex.Match(content, @"""tag_name"":\S*").ToString();
        string version = DownloadHelper.ExtractVersionFromText(tag);
        return version;
    }

    /// <summary>
    /// Delete the old version of ReShade Deployer if it exists.
    /// </summary>
    public static async void CleanUpOldVersion()
    {
        string oldExe = Process.GetCurrentProcess().MainModule!.FileName + ".oldver";
        if (File.Exists(oldExe))
        {
            try
            {
                File.Delete(oldExe);
            }
            catch
            {
                // If the old version could not be deleted, we need to wait a bit and try again
                try
                {
                    await Task.Delay(200);
                    File.Delete(oldExe);
                }
                catch { /* Ignored */ }
            }
        }
    }
}