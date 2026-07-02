using System.Diagnostics;
using System.IO.Compression;
using Testably.Abstractions.Testing;

namespace ReShadeDeployer.Tests.Updaters;

public class AppUpdaterTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly MockDownloadService _downloadService;
    private readonly MockMessageBox _messageBox;
    private readonly AppUpdater _updater;
    private readonly string _currentExePath;

    public AppUpdaterTests()
    {
        _fileSystem = new MockFileSystem();
        _downloadService = new MockDownloadService(_fileSystem);
        _messageBox = new MockMessageBox();
        _updater = new AppUpdater(_fileSystem, _downloadService, _messageBox);

        // Resolve current process main module filename and pre-create it in mock filesystem
        _currentExePath = Process.GetCurrentProcess().MainModule!.FileName;
        
        // Ensure directories for paths are created
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(_currentExePath)!);
        _fileSystem.Directory.CreateDirectory(Paths.Root);
        _fileSystem.Directory.CreateDirectory(Paths.Lib);
        _fileSystem.Directory.CreateDirectory(Paths.Dlls);
        _fileSystem.Directory.CreateDirectory(Paths.AddonDlls);
    }

    [Fact]
    public async Task GetLatestOnlineVersionNumber_ShouldReturnParsedVersion_WhenContentIsValid()
    {
        // Arrange
        const string githubUrl = "https://api.github.com/repos/Jobus0/ReShade-Deployer/releases/latest";
        _downloadService.WebsiteContentResponses[githubUrl] = "{\"tag_name\":\"v1.4.2\"}";

        // Act
        string version = await _updater.GetLatestOnlineVersionNumber();

        // Assert
        Assert.Equal("1.4.2", version);
    }

    [Fact]
    public async Task GetLatestOnlineVersionNumber_ShouldThrow_WhenDownloadServiceThrows()
    {
        // Arrange
        const string githubUrl = "https://api.github.com/repos/Jobus0/ReShade-Deployer/releases/latest";
        _downloadService.Failures[githubUrl] = new HttpRequestException("Network failure");

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _updater.GetLatestOnlineVersionNumber());
    }

    [Fact]
    public async Task CleanUpOldVersion_ShouldDeleteOldExeAndDllOldverFiles_WhenTheyExist()
    {
        // Arrange
        string oldExePath = _currentExePath + ".oldver";
        _fileSystem.File.WriteAllText(oldExePath, "old exe content");

        string dll1 = _fileSystem.Path.Combine(Paths.Dlls, "ReShade32.dll.oldver");
        string dll2 = _fileSystem.Path.Combine(Paths.AddonDlls, "ReShade64.dll.oldver");
        
        _fileSystem.File.WriteAllText(dll1, "old dll");
        _fileSystem.File.WriteAllText(dll2, "old dll 2");

        // Act
        await _updater.CleanUpOldVersion();

        // Assert
        Assert.False(_fileSystem.File.Exists(oldExePath));
        Assert.False(_fileSystem.File.Exists(dll1));
        Assert.False(_fileSystem.File.Exists(dll2));
    }

    [Fact]
    public async Task Update_ShouldDownloadExtractAndCleanUp_WhenDownloadSucceeds()
    {
        // Arrange
        const string githubUrl = "https://api.github.com/repos/Jobus0/ReShade-Deployer/releases/latest";
        const string downloadUrl = "https://github.com/Jobus0/ReShade-Deployer/releases/download/v1.4.2/ReShade-Deployer.zip";
        
        // Mock GitHub response containing the browser download URL
        _downloadService.WebsiteContentResponses[githubUrl] = $"{{\"tag_name\":\"v1.4.2\",\"browser_download_url\":\"{downloadUrl}\"}}";

        // Generate a valid mock ZIP archive and supply it as download response
        byte[] mockZip = CreateMockZipArchiveBytes("extracted-file.txt", "extracted content");
        _downloadService.DownloadResponses[downloadUrl] = mockZip;

        // The current executable must exist in the mock filesystem to be renamed/moved
        _fileSystem.File.WriteAllText(_currentExePath, "current executable binary");

        // Act
        await _updater.Update();

        // Assert
        // 1. Verify download was called
        Assert.Contains(_downloadService.DownloadCalls, call => call.Url == downloadUrl);

        // 2. Verify current exe was backed up to .oldver
        string oldExePath = _currentExePath + ".oldver";
        Assert.True(_fileSystem.File.Exists(oldExePath));
        Assert.Equal("current executable binary", _fileSystem.File.ReadAllText(oldExePath));

        // 3. Verify ZIP archive content was extracted to Paths.Root
        string extractedFilePath = _fileSystem.Path.Combine(Paths.Root, "extracted-file.txt");
        Assert.True(_fileSystem.File.Exists(extractedFilePath));
        Assert.Equal("extracted content", _fileSystem.File.ReadAllText(extractedFilePath));

        // 4. Verify downloaded zip itself was deleted
        string downloadPath = _fileSystem.Path.Combine(Paths.Lib, "ReShade-Deployer.zip");
        Assert.False(_fileSystem.File.Exists(downloadPath));

        // 5. Verify no error message was shown
        Assert.Empty(_messageBox.ShowCalls);
    }

    [Fact]
    public async Task Update_ShouldShowMessageBoxAndAbort_WhenDownloadFails()
    {
        // Arrange
        const string githubUrl = "https://api.github.com/repos/Jobus0/ReShade-Deployer/releases/latest";
        const string downloadUrl = "https://github.com/Jobus0/ReShade-Deployer/releases/download/v1.4.2/ReShade-Deployer.zip";
        
        _downloadService.WebsiteContentResponses[githubUrl] = $"{{\"tag_name\":\"v1.4.2\",\"browser_download_url\":\"{downloadUrl}\"}}";
        _downloadService.Failures[downloadUrl] = new HttpRequestException("Download server unreachable");

        _fileSystem.File.WriteAllText(_currentExePath, "current executable binary");

        // Act
        await _updater.Update();

        // Assert
        // 1. Verify message box was shown with error
        Assert.Single(_messageBox.ShowCalls);
        Assert.Contains(downloadUrl, _messageBox.ShowCalls[0].ContentText);

        // 2. Verify current exe was not moved (still exists, and no .oldver exists)
        Assert.True(_fileSystem.File.Exists(_currentExePath));
        Assert.False(_fileSystem.File.Exists(_currentExePath + ".oldver"));
    }

    private byte[] CreateMockZipArchiveBytes(string filename, string content)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry(filename);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream);
            writer.Write(content);
        }
        return memoryStream.ToArray();
    }
}
