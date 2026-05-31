using System.IO.Abstractions;

namespace ReShadeDeployer.Tests;

public class MockDownloadService(IFileSystem fileSystem) : IDownloadService
{
    public Dictionary<string, string> WebsiteContentResponses { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, byte[]> DownloadResponses { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<(string Url, string SavePath)> DownloadCalls { get; } = new();
    public Dictionary<string, Exception> Failures { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Func<string, Task<string>>? GetWebsiteContentFunc { get; set; }
    public Func<string, string, Task>? DownloadFunc { get; set; }

    public async Task<string> GetWebsiteContent(string url)
    {
        if (Failures.TryGetValue(url, out var ex))
            throw ex;

        if (GetWebsiteContentFunc != null)
            return await GetWebsiteContentFunc(url);

        if (WebsiteContentResponses.TryGetValue(url, out var content))
            return content;

        return string.Empty;
    }

    public async Task Download(string url, string downloadSavePath)
    {
        DownloadCalls.Add((url, downloadSavePath));

        if (Failures.TryGetValue(url, out var ex))
            throw ex;

        if (DownloadFunc != null)
        {
            await DownloadFunc(url, downloadSavePath);
            return;
        }

        byte[] bytes = DownloadResponses.TryGetValue(url, out var data) ? data : Array.Empty<byte>();
        
        string? directory = fileSystem.Path.GetDirectoryName(downloadSavePath);
        if (directory != null && !fileSystem.Directory.Exists(directory))
        {
            fileSystem.Directory.CreateDirectory(directory);
        }
        
        fileSystem.File.WriteAllBytes(downloadSavePath, bytes);
    }
}
