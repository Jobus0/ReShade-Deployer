﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using SevenZipExtractor;

namespace ReShadeDeployer;

/// <summary>
/// Provides methods for downloading and extracting official ReShade files.
/// </summary>
public static class Downloader
{
    private const string WebsiteUrl = "https://reshade.me";

    /// <summary>
    /// Retrieves the content of a website using an HTTP GET request.
    /// </summary>
    /// <returns>The website content as a string.</returns>
    private static async Task<string> GetWebsiteContent()
    {
        using var httpClient = new HttpClient();
        return await httpClient.GetStringAsync(WebsiteUrl);
    }
    
    /// <summary>
    /// Extracts the version number from a download URL.
    /// </summary>
    /// <param name="downloadUrl">The download URL string.</param>
    /// <returns>The version number extracted from the download URL.</returns>
    private static string UrlToVersion(string downloadUrl)
    {
        return Regex.Match(downloadUrl, @"\d+(\.\d+)*").ToString();
    }
    
    /// <summary>
    /// Download the latest version of ReShade (including add-on support version) from the official website and extract it into the lib folder.
    /// </summary>
    public static async Task DownloadReShade()
    {
        string websiteContent;
        try
        {
            websiteContent = await GetWebsiteContent();
        }
        catch
        {
            MessageBox.Show(UIStrings.ConnectionError, UIStrings.ConnectionError_Title, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        CreateDirectories();

        string downloadUrl = Regex.Match(websiteContent, "/downloads/\\S*.exe").ToString();
        
        if (TryGetLocalReShadeVersion(out string localVersion) && localVersion == UrlToVersion(downloadUrl))
        {
            MessageBox.Show(UIStrings.UpdateError, UIStrings.Update, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        await DownloadAndExtract(WebsiteUrl + downloadUrl, Paths.Dlls);
        await DownloadAndExtract(WebsiteUrl + Regex.Match(websiteContent, "/downloads/\\S*_Addon.exe"), Paths.AddonDlls);
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
            MessageBox.Show(string.Format(UIStrings.DownloadError, url), UIStrings.DownloadError_Title, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        const string innerArchiveName = "[0]";
        string innerArchivePath = Path.Combine(directoryPath, innerArchiveName);
        try
        {
            using ArchiveFile archiveFile = new ArchiveFile(zipPath);
            archiveFile.Extract(e => e.FileName == innerArchiveName
                ? innerArchivePath
                : null);

            using ArchiveFile innerArchiveFile = new ArchiveFile(innerArchivePath);
            innerArchiveFile.Extract(innerEntry => innerEntry.FileName is "ReShade32.dll" or "ReShade64.dll"
                ? Path.Combine(directoryPath, innerEntry.FileName)
                : null);
        }
        catch (IOException)
        {
            MessageBox.Show(string.Format(UIStrings.AccessError, url), UIStrings.AccessError_Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.GetType() + ": " + e.Message, UIStrings.AccessError_Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            File.Delete(zipPath);
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
    public static bool TryGetLocalReShadeVersion(out string version)
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
    public static async Task<string> GetLatestOnlineReShadeVersion()
    {
        var websiteContent = await GetWebsiteContent();
        return UrlToVersion(Regex.Match(websiteContent, "/downloads/\\S*.exe").ToString());
    }
}