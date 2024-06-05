using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReShadeDeployer;

/// <summary>
/// Provides methods for downloading and extracting official ReShade files.
/// </summary>
public static partial class DownloadHelper
{
    /// <summary>
    /// Retrieves the content of a given URL using an HTTP GET request.
    /// </summary>
    /// <returns>The URL content as a string.</returns>
    public static async Task<string> GetWebsiteContent(string url)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "ReShade Deployer");
        return await httpClient.GetStringAsync(url);
    }
    
    /// <summary>
    /// Downloads a file from a given URL and saves it to a given path.
    /// </summary>
    /// <param name="url">URL of the file to download.</param>
    /// <param name="downloadSavePath">Path to save the downloaded file to.</param>
    public static async Task Download(string url, string downloadSavePath)
    {
        using HttpClient httpClient = new HttpClient();
        await using var httpStream = await httpClient.GetStreamAsync(url);
        await using var fileStream = new FileStream(downloadSavePath, FileMode.CreateNew);
        await httpStream.CopyToAsync(fileStream);
    }
    
    /// <summary>
    /// Extracts the version number from a download URL.
    /// </summary>
    /// <param name="text">The text to extract the version number from.</param>
    /// <returns>The version number extracted from the download URL.</returns>
    public static string ExtractVersionFromText(string text)
    {
        return ExtractVersionFromTextRegex().Match(text).ToString();
    }
    
    [GeneratedRegex("\\d+(\\.\\d+)*")]
    private static partial Regex ExtractVersionFromTextRegex();
}