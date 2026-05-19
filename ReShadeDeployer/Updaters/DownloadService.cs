using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReShadeDeployer;

/// <summary>
/// Provides methods for downloading and extracting files.
/// </summary>
public class DownloadService
{
    /// <summary>
    /// Retrieves the content of a given URL using an HTTP GET request.
    /// </summary>
    /// <returns>The URL content as a string.</returns>
    public async Task<string> GetWebsiteContent(string url)
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
    public async Task Download(string url, string downloadSavePath)
    {
        using HttpClient httpClient = new HttpClient();
        await using var httpStream = await httpClient.GetStreamAsync(url);
        await using var fileStream = new FileStream(downloadSavePath, FileMode.Create);
        await httpStream.CopyToAsync(fileStream);
    }
}