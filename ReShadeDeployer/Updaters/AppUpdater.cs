using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Testably.Abstractions;

namespace ReShadeDeployer;

public class AppUpdater(IFileSystem fileSystem, IDownloadService downloadService, IMessageBox messageBox)
{
    private const string GithubUrl = "https://api.github.com/repos/Jobus0/ReShade-Deployer/releases/latest";
    
    /// <summary>
    /// Download the latest version of ReShade Deployer from GitHub and restart the application with the new version.
    /// </summary>
    public async Task Update()
    {
        // Fetch latest version of ReShade Deployer from GitHub
        string content = await downloadService.GetWebsiteContent(GithubUrl);
        string downloadUrl = Regex.Match(content, @"""browser_download_url"":\s?""(\S*?)""").Groups[1].ToString();
        string downloadPath = fileSystem.Path.Combine(Paths.Lib, "ReShade-Deployer.zip");

        try
        {
            await downloadService.Download(downloadUrl, downloadPath);
        }
        catch
        {
            await messageBox.Show(UIStrings.DownloadError_Title, string.Format(UIStrings.DownloadError, downloadUrl));
            return;
        }
        
        // Rename the current exe because we cannot overwrite it while it is running
        string currentExe = Process.GetCurrentProcess().MainModule!.FileName;
        string oldExe = currentExe + ".oldver";
        fileSystem.File.Move(currentExe, oldExe, overwrite: true);
        
        // Extract the new version of ReShade Deployer
        using (IZipArchive zip = fileSystem.ZipArchive().New(fileSystem.File.OpenRead(downloadPath), ZipArchiveMode.Read, false))
            zip.ExtractToDirectory(Paths.Root);
        
        // Delete the downloaded archive
        fileSystem.File.Delete(downloadPath);
        
        // Run the new version of ReShade Deployer
        if (Application.Current is App app)
        {
            var startupArgs = app.StartupArgs;
            var startInfo = new ProcessStartInfo
            {
                FileName = currentExe,
                Arguments = startupArgs.Length > 0 ? $"\"{startupArgs[0]}\"" : string.Empty,
                UseShellExecute = true,
                Verb = "runas",
            };
            Process.Start(startInfo);
            
            // Close the current (old) version of ReShade Deployer
            app.Shutdown();
        }
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
    public async Task CleanUpOldVersion()
    {
        string oldExe = Process.GetCurrentProcess().MainModule!.FileName + ".oldver";
        if (fileSystem.File.Exists(oldExe))
        {
            try
            {
                fileSystem.File.Delete(oldExe);
            }
            catch
            {
                // If the old version could not be deleted, we need to wait a bit and try again
                try
                {
                    await Task.Delay(200);
                    fileSystem.File.Delete(oldExe);
                }
                catch { /* Ignored */ }
            }
        }

        foreach (var path in new[] {Paths.Dlls, Paths.AddonDlls})
        {
            if (!fileSystem.Directory.Exists(path))
                continue;

            // Delete all old dll versions
            foreach (var file in fileSystem.Directory.EnumerateFiles(path, "*.dll.oldver", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    fileSystem.File.Delete(file);
                }
                catch { /* Ignored */ }
            }
        }
    }
}