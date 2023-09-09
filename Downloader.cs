using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using SevenZipExtractor;

namespace ReShadeInstaller;

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

        try
        {
            using ArchiveFile archiveFile = new ArchiveFile(zipPath);
            foreach (Entry entry in archiveFile.Entries)
            {
                if (entry.FileName == "[0]")
                {
                    entry.Extract(entry.FileName);

                    using ArchiveFile innerArchiveFile = new ArchiveFile("[0]");
                    foreach (Entry innerEntry in innerArchiveFile.Entries)
                    {
                        if (innerEntry.FileName is "ReShade32.dll" or "ReShade64.dll")
                        {
                            innerEntry.Extract(innerEntry.FileName);
                            string finalPath = Path.Combine(directoryPath, innerEntry.FileName);
                            if (File.Exists(finalPath))
                                File.Delete(finalPath);
                            File.Move(innerEntry.FileName, finalPath);
                        }
                    }
                }
            }
        }
        finally
        {
            File.Delete(zipPath);
            File.Delete("[0]");
        }
    }
    
    public static void CreateDirectories()
    {
        Directory.CreateDirectory(Paths.Shaders);
        Directory.CreateDirectory(Paths.Textures);
        Directory.CreateDirectory(Paths.Dlls);
        Directory.CreateDirectory(Paths.AddonDlls);
        Directory.CreateDirectory(Paths.Cache);
    }
}