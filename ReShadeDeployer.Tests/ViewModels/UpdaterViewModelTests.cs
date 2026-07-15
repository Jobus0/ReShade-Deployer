using Testably.Abstractions.Testing;
using ReShadeDeployer.ViewModels;
using Wpf.Ui.Controls;

namespace ReShadeDeployer.Tests.ViewModels;

public class UpdaterViewModelTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly MockDownloadService _downloadService;
    private readonly MockMessageBox _messageBox;
    private readonly MockSnackbarService _snackbarService;
    private readonly MockConfig _config;

    private readonly AppUpdater _appUpdater;
    private readonly ReShadeUpdater _reShadeUpdater;

    public UpdaterViewModelTests()
    {
        _fileSystem = new MockFileSystem();
        _downloadService = new MockDownloadService(_fileSystem);
        _messageBox = new MockMessageBox();
        _snackbarService = new MockSnackbarService();
        _config = new MockConfig();

        var fileHelper = new FileHelper(_fileSystem);
        _appUpdater = new AppUpdater(_fileSystem, _downloadService, _messageBox);
        _reShadeUpdater = new ReShadeUpdater(_fileSystem, fileHelper, _downloadService, _messageBox);

        // Pre-create necessary directories
        _fileSystem.Directory.CreateDirectory(Paths.Root);
        _fileSystem.Directory.CreateDirectory(Paths.Dlls);
        _fileSystem.Directory.CreateDirectory(Paths.AddonDlls);
        _fileSystem.Directory.CreateDirectory(Paths.CommonPathLocal);
    }

    [Fact]
    public void Initialize_WhenNoLocalReShade_FiresRequestFirstTimeSetup()
    {
        // Arrange
        var viewModel = new UpdaterViewModel(_appUpdater, _reShadeUpdater, _config, _messageBox, _snackbarService);
        bool firstTimeSetupFired = false;
        viewModel.RequestFirstTimeSetup += () => firstTimeSetupFired = true;

        // Act
        viewModel.Initialize();

        // Assert
        Assert.True(firstTimeSetupFired);
        Assert.Equal(string.Empty, viewModel.VersionLabelContent);
    }

    [Fact]
    public void Initialize_WhenLocalReShadeExists_SetsVersionLabel()
    {
        // Arrange
        // Write a dummy ReShade64.dll and set its version info
        string dllPath = _fileSystem.Path.Combine(Paths.Dlls, "ReShade64.dll");
        _fileSystem.File.WriteAllText(dllPath, "dummy ReShade DLL");
        _fileSystem.WithFileVersionInfo(dllPath, v => v.SetFileVersion("5.8.0.0"));

        _config.LatestReShadeVersionNumber = "5.8.0";
        _config.LatestReShadeVersionNumberCheckDate = DateTime.Now; // skip online check

        var viewModel = new UpdaterViewModel(_appUpdater, _reShadeUpdater, _config, _messageBox, _snackbarService);

        // Act
        viewModel.Initialize();

        // Assert
        Assert.Equal("5.8.0", viewModel.VersionLabelContent);
    }

    [Fact]
    public void Initialize_WhenLocalReShadeExistsAndNewerVersionExists_SetsUpdateButtonContent()
    {
        // Arrange
        string dllPath = _fileSystem.Path.Combine(Paths.Dlls, "ReShade64.dll");
        _fileSystem.File.WriteAllText(dllPath, "dummy ReShade DLL");
        _fileSystem.WithFileVersionInfo(dllPath, v => v.SetFileVersion("5.8.0.0"));

        // Setup latest version in config as a newer version, and check date as recently checked
        _config.LatestReShadeVersionNumber = "5.9.2";
        _config.LatestReShadeVersionNumberCheckDate = DateTime.Now;

        var viewModel = new UpdaterViewModel(_appUpdater, _reShadeUpdater, _config, _messageBox, _snackbarService);

        // Act
        viewModel.Initialize();

        // Assert
        Assert.Equal("5.8.0", viewModel.VersionLabelContent);
        Assert.Equal("5.9.2", viewModel.UpdateButtonContent);
    }

    [Fact]
    public void PromptForUpdate_UserCancels_DoesNotTriggerUpdate()
    {
        // Arrange
        _messageBox.NextResults.Enqueue(MessageBoxResult.Secondary); // Cancel/Skip
        _config.LatestDeployerVersionNumber = "2.0.0";
        
        var viewModel = new UpdaterViewModel(_appUpdater, _reShadeUpdater, _config, _messageBox, _snackbarService);

        // Act
        viewModel.PromptForUpdate();

        // Assert
        Assert.True(viewModel.IsPanelEnabled);
        Assert.True(viewModel.IsUpdateEnabled);
    }
}
