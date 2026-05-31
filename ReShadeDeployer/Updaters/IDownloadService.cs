using System.Threading.Tasks;

namespace ReShadeDeployer;

/// <summary>
/// Provides methods for downloading and extracting files.
/// </summary>
public interface IDownloadService
{
    /// <summary>
    /// Retrieves the content of a given URL using an HTTP GET request.
    /// </summary>
    /// <returns>The URL content as a string.</returns>
    Task<string> GetWebsiteContent(string url);

    /// <summary>
    /// Downloads a file from a given URL and saves it to a given path.
    /// </summary>
    /// <param name="url">URL of the file to download.</param>
    /// <param name="downloadSavePath">Path to save the downloaded file to.</param>
    Task Download(string url, string downloadSavePath);
}
