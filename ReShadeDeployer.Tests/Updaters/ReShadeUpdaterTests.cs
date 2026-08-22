using System.IO.Compression;
using Testably.Abstractions.Testing;

namespace ReShadeDeployer.Tests.Updaters;

public class ReShadeUpdaterTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly FileHelper _fileHelper;
    private readonly MockDownloadService _downloadService;
    private readonly MockMessageBox _messageBox;
    private readonly ReShadeUpdater _updater;

    public ReShadeUpdaterTests()
    {
        _fileSystem = new MockFileSystem();
        _fileHelper = new FileHelper(_fileSystem);
        _downloadService = new MockDownloadService(_fileSystem);
        _messageBox = new MockMessageBox();
        _updater = new ReShadeUpdater(_fileSystem, _fileHelper, _downloadService, _messageBox);

        // Ensure directories used in Paths are set up
        _fileSystem.Directory.CreateDirectory(Paths.Root);
        _fileSystem.Directory.CreateDirectory(Paths.Dlls);
        _fileSystem.Directory.CreateDirectory(Paths.AddonDlls);
    }

    [Fact]
    public async Task GetLatestOnlineReShadeVersionNumber_ShouldReturnParsedVersion_WhenContentIsValid()
    {
        // Arrange
        const string websiteUrl = "https://reshade.me";
        _downloadService.WebsiteContentResponses[websiteUrl] = 
            "<html><body><a href=\"/downloads/ReShade_Setup_6.0.1.exe\">Download ReShade 6.0.1</a></body></html>";

        // Act
        string version = await _updater.GetLatestOnlineReShadeVersionNumber();

        // Assert
        Assert.Equal("6.0.1", version);
    }

    [Fact]
    public void TryGetLocalReShadeVersionNumber_ShouldReturnTrueAndParsedVersion_WhenFileExists()
    {
        // Arrange
        string dllPath = _fileSystem.Path.Combine(Paths.Dlls, "ReShade64.dll");
        _fileSystem.File.WriteAllText(dllPath, "mock dll content");
        
        // Mock the FileVersionInfo structure in Testably
        _fileSystem.WithFileVersionInfo(dllPath, v => v.SetFileVersion("5.9.2.0"));

        // Act
        bool result = _updater.TryGetLocalReShadeVersionNumber(out string version);

        // Assert
        Assert.True(result);
        Assert.Equal("5.9.2", version);
    }

    [Fact]
    public void TryGetLocalReShadeVersionNumber_ShouldReturnFalse_WhenFileDoesNotExist()
    {
        // Act
        bool result = _updater.TryGetLocalReShadeVersionNumber(out string version);

        // Assert
        Assert.False(result);
        Assert.Equal(string.Empty, version);
    }

    [Fact]
    public async Task Update_ShouldShowError_WhenConnectionFails()
    {
        // Arrange
        const string websiteUrl = "https://reshade.me";
        _downloadService.Failures[websiteUrl] = new HttpRequestException("Host unresolved");

        // Act
        bool result = await _updater.Update();

        // Assert
        Assert.False(result);
        Assert.Single(_messageBox.ShowCalls);
        Assert.Equal(UIStrings.ConnectionError, _messageBox.ShowCalls[0].ContentText);
    }

    [Fact]
    public async Task Update_ShouldShowError_WhenLocalVersionIsAlreadyLatest()
    {
        // Arrange
        const string websiteUrl = "https://reshade.me";
        _downloadService.WebsiteContentResponses[websiteUrl] = 
            "<html><body><a href=\"/downloads/ReShade_Setup_5.9.2.exe\">Download</a></body></html>";

        string dllPath = _fileSystem.Path.Combine(Paths.Dlls, "ReShade64.dll");
        _fileSystem.File.WriteAllText(dllPath, "mock dll content");
        _fileSystem.WithFileVersionInfo(dllPath, v => v.SetFileVersion("5.9.2.0"));

        // Act
        bool result = await _updater.Update();

        // Assert
        Assert.False(result);
        Assert.Single(_messageBox.ShowCalls);
        Assert.Equal(UIStrings.UpdateError, _messageBox.ShowCalls[0].ContentText);
        // Verify no downloads were initiated
        Assert.Empty(_downloadService.DownloadCalls);
    }

    [Fact]
    public async Task Update_ShouldDownloadAndExtract_WhenNewVersionIsAvailable()
    {
        // Arrange
        const string websiteUrl = "https://reshade.me";
        _downloadService.WebsiteContentResponses[websiteUrl] = 
            "<html><body>" +
            "<a href=\"/downloads/ReShade_Setup_6.0.0.exe\">Download Standard</a>" +
            "<a href=\"/downloads/ReShade_Setup_6.0.0_Addon.exe\">Download Addon</a>" +
            "</body></html>";

        // Setup local dll with an older version
        string dllPath = _fileSystem.Path.Combine(Paths.Dlls, "ReShade64.dll");
        _fileSystem.File.WriteAllText(dllPath, "old dll");
        _fileSystem.WithFileVersionInfo(dllPath, v => v.SetFileVersion("5.9.2.0"));

        // Generate installers containing embedded zip archives
        byte[] standardInstaller = CreateMockReShadeInstallerBytes("standard_64", "standard_32");
        byte[] addonInstaller = CreateMockReShadeInstallerBytes("addon_64", "addon_32");

        _downloadService.DownloadResponses[websiteUrl + "/downloads/ReShade_Setup_6.0.0.exe"] = standardInstaller;
        _downloadService.DownloadResponses[websiteUrl + "/downloads/ReShade_Setup_6.0.0_Addon.exe"] = addonInstaller;
        
        // Mock compatibility.ini content
        _downloadService.DownloadResponses["https://raw.githubusercontent.com/crosire/reshade-shaders/list/Compatibility.ini"] = 
            System.Text.Encoding.UTF8.GetBytes("[Compatibility]");

        // Act
        bool result = await _updater.Update();

        // Assert
        Assert.True(result);
        // 1. Verify files are downloaded
        Assert.Contains(_downloadService.DownloadCalls, call => call.Url.Contains("ReShade_Setup_6.0.0.exe"));
        Assert.Contains(_downloadService.DownloadCalls, call => call.Url.Contains("ReShade_Setup_6.0.0_Addon.exe"));
        Assert.Contains(_downloadService.DownloadCalls, call => call.Url.Contains("Compatibility.ini"));

        // 2. Verify extracted DLL files exist in standard path
        Assert.True(_fileSystem.File.Exists(_fileSystem.Path.Combine(Paths.Dlls, "ReShade32.dll")));
        Assert.True(_fileSystem.File.Exists(_fileSystem.Path.Combine(Paths.Dlls, "ReShade64.dll")));
        Assert.Equal("standard_32", _fileSystem.File.ReadAllText(_fileSystem.Path.Combine(Paths.Dlls, "ReShade32.dll")));
        Assert.Equal("standard_64", _fileSystem.File.ReadAllText(_fileSystem.Path.Combine(Paths.Dlls, "ReShade64.dll")));

        // 3. Verify extracted DLL files exist in addon path
        Assert.True(_fileSystem.File.Exists(_fileSystem.Path.Combine(Paths.AddonDlls, "ReShade32.dll")));
        Assert.True(_fileSystem.File.Exists(_fileSystem.Path.Combine(Paths.AddonDlls, "ReShade64.dll")));
        Assert.Equal("addon_32", _fileSystem.File.ReadAllText(_fileSystem.Path.Combine(Paths.AddonDlls, "ReShade32.dll")));
        Assert.Equal("addon_64", _fileSystem.File.ReadAllText(_fileSystem.Path.Combine(Paths.AddonDlls, "ReShade64.dll")));

        // 4. Verify compatibility ini is written
        Assert.True(_fileSystem.File.Exists(Paths.CompatibilityIni));
        Assert.Equal("[Compatibility]", _fileSystem.File.ReadAllText(Paths.CompatibilityIni));
    }

    private byte[] CreateMockReShadeInstallerBytes(string content64, string content32)
    {
        // 1. Create a valid ZIP archive containing ReShade32.dll and ReShade64.dll
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var entry32 = archive.CreateEntry("ReShade32.dll");
            using (var s = entry32.Open())
            using (var w = new StreamWriter(s))
                w.Write(content32);

            var entry64 = archive.CreateEntry("ReShade64.dll");
            using (var s = entry64.Open())
            using (var w = new StreamWriter(s))
                w.Write(content64);
        }
        byte[] zipBytes = ms.ToArray();

        // 2. Align to 512-byte block boundary. We place 512 bytes of padding first.
        byte[] installerBytes = new byte[512 + zipBytes.Length];
        Array.Fill<byte>(installerBytes, 0, 0, 512); // Padding
        Array.Copy(zipBytes, 0, installerBytes, 512, zipBytes.Length); // ZIP bytes starting at offset 512

        return installerBytes;
    }
}
