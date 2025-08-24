using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace ReShadeDeployer;

public class DeployerDownloader(DownloadService downloadService, IMessageBox messageBox)
{
    private const string GithubUrl = "https://api.github.com/repos/Jobus0/ReShade-Deployer/releases/latest";
    
    /// <summary>
    /// Download the latest version of ReShade Deployer from GitHub.
    /// </summary>
    public async void UpdateDeployer()
    {
        // Fetch latest version of ReShade Deployer from GitHub
        string content = await downloadService.GetWebsiteContent(GithubUrl);
        string downloadUrl = Regex.Match(content, @"""browser_download_url"":\s?""(\S*?)""").Groups[1].ToString();
        string downloadPath = Path.Combine(Paths.Lib, "ReShade-Deployer.zip");

        try
        {
            await downloadService.Download(downloadUrl, downloadPath);
        }
        catch
        {
            messageBox.Show(string.Format(UIStrings.DownloadError, downloadUrl), UIStrings.DownloadError_Title);
            return;
        }
        
        // Rename the current exe because we cannot overwrite it while it is running
        string currentExe = Process.GetCurrentProcess().MainModule!.FileName;
        string oldExe = currentExe + ".oldver";
        File.Move(currentExe, oldExe, overwrite: true);
        
        // Extract the new version of ReShade Deployer
        using (ZipArchive zip = new ZipArchive(File.OpenRead(downloadPath), ZipArchiveMode.Read, false))
            zip.ExtractToDirectory(Paths.Root);
        
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
    public async Task<string> GetLatestOnlineVersionNumber()
    {
        string content = await downloadService.GetWebsiteContent(GithubUrl);
        string tag = Regex.Match(content, @"""tag_name"":\S*").ToString();
        string version = VersionParser.Parse(tag);
        return version;
    }

    /// <summary>
    /// Delete the old version of ReShade Deployer if it exists.
    /// </summary>
    public async void CleanUpOldVersion()
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

        foreach (var path in new[] {Paths.Dlls, Paths.AddonDlls})
        {
            if (!Directory.Exists(path))
                continue;

            // Delete all old dll versions
            foreach (var file in Directory.EnumerateFiles(path, "*.dll.oldver", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    File.Delete(file);
                }
                catch { /* Ignored */ }
            }
        }
    }
}