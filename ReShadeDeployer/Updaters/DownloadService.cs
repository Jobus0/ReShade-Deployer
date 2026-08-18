using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using System.Threading.Tasks;

namespace ReShadeDeployer;

/// <summary>
/// Provides methods for downloading and extracting files.
/// </summary>
public class DownloadService(HttpClient httpClient, IFileSystem fileSystem) : IDownloadService
{
    /// <summary>
    /// Retrieves the content of a given URL using an HTTP GET request.
    /// </summary>
    public async Task<string> GetWebsiteContent(string url)
    {
        return await httpClient.GetStringAsync(url);
    }
    
    /// <summary>
    /// Downloads a file from a given URL and saves it to a given path.
    /// </summary>
    public async Task Download(string url, string downloadSavePath)
    {
        await using var httpStream = await httpClient.GetStreamAsync(url);
        await using var fileStream = fileSystem.FileStream.New(downloadSavePath, FileMode.Create);
        await httpStream.CopyToAsync(fileStream);
    }
}