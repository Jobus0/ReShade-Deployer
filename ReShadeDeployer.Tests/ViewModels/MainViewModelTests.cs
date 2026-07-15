using Testably.Abstractions.Testing;
using ReShadeDeployer.ViewModels;
using Wpf.Ui.Controls;

namespace ReShadeDeployer.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly MockRegistry _registry;
    private readonly MockMessageBox _messageBox;
    private readonly MockSnackbarService _snackbarService;
    private readonly MockIconExtractor _iconExtractor;
    private readonly MockConfig _config;

    private readonly AppUpdater _appUpdater;
    private readonly ReShadeUpdater _reShadeUpdater;
    private readonly VulkanSystemWideDeployer _vulkanDeployer;
    private readonly DllDeployer _dllDeployer;
    private readonly IniDeployer _iniDeployer;
    private readonly PresetDeployer _presetDeployer;
    private readonly AddonsDeployer _addonsDeployer;
    private readonly DeploymentOrchestrator _orchestrator;
    
    private readonly ReShadeUndeployer _undeployer;
    private readonly ExecutableContextFactory _factory;
    
    private readonly UpdaterViewModel _updaterVm;
    private readonly SettingsViewModel _settingsVm;
    private readonly AddonsViewModel _addonsVm;

    public MainViewModelTests()
    {
        _fileSystem = new MockFileSystem();
        _registry = new MockRegistry();
        _messageBox = new MockMessageBox();
        _snackbarService = new MockSnackbarService();
        _iconExtractor = new MockIconExtractor();
        _config = new MockConfig();
        
        var fileHelper = new FileHelper(_fileSystem);
        var downloadService = new MockDownloadService(_fileSystem);

        _appUpdater = new AppUpdater(_fileSystem, downloadService, _messageBox);
        _reShadeUpdater = new ReShadeUpdater(_fileSystem, fileHelper, downloadService, _messageBox);
        _vulkanDeployer = new VulkanSystemWideDeployer(_fileSystem, _registry);
        _dllDeployer = new DllDeployer(_fileSystem, _vulkanDeployer);
        _iniDeployer = new IniDeployer(_fileSystem);
        _presetDeployer = new PresetDeployer(_fileSystem);
        _addonsDeployer = new AddonsDeployer(_fileSystem, fileHelper);
        
        _orchestrator = new DeploymentOrchestrator(
            _fileSystem,
            _dllDeployer,
            _vulkanDeployer,
            _iniDeployer,
            _presetDeployer,
            _addonsDeployer,
            _messageBox,
            _config,
            _snackbarService
        );

        _undeployer = new ReShadeUndeployer(_fileSystem);
        _factory = new ExecutableContextFactory(_fileSystem);

        _updaterVm = new UpdaterViewModel(_appUpdater, _reShadeUpdater, _config, _messageBox, _snackbarService);
        var registryHelper = new RegistryHelper(_registry);
        _settingsVm = new SettingsViewModel(_config, registryHelper, _messageBox, _vulkanDeployer);
        _addonsVm = new AddonsViewModel(_addonsDeployer, _config);

        // Pre-create some folders/files
        _fileSystem.Directory.CreateDirectory(Paths.Root);
        _fileSystem.Directory.CreateDirectory(Paths.Dlls);
        _fileSystem.Directory.CreateDirectory(Paths.AddonDlls);
        _fileSystem.Directory.CreateDirectory(Paths.CommonPathLocal);
        _fileSystem.Directory.CreateDirectory(Paths.Addons);

        string dllPath = _fileSystem.Path.Combine(Paths.Dlls, "ReShade64.dll");
        _fileSystem.File.WriteAllText(dllPath, "dll64");
        _fileSystem.WithFileVersionInfo(dllPath, v => v.SetFileVersion("5.8.0.0"));
    }

    [Fact]
    public void TargetExecutable_WhenFileDoesNotExist_ReturnsEarlyWithoutTargeting()
    {
        // Arrange
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService);
        string nonExistentPath = @"C:\Games\DoesNotExist.exe";

        // Act
        mainVm.TargetExecutable(nonExistentPath);

        // Assert
        Assert.Null(mainVm.SelectedExecutableContext);
        Assert.False(mainVm.IsDeployEnabled);
    }

    [Fact]
    public void TargetExecutable_WhenFileExists_SetsContextAndEnablesDeploy()
    {
        // Arrange
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService);
        string gamePath = @"C:\Games\TargetGame\game.exe";
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(gamePath)!);
        _fileSystem.File.WriteAllText(gamePath, "game binary");

        // Act
        mainVm.TargetExecutable(gamePath);

        // Assert
        Assert.NotNull(mainVm.SelectedExecutableContext);
        Assert.Equal("game.exe", mainVm.SelectedExecutableContext.FileName);
        Assert.True(mainVm.IsDeployEnabled);
        Assert.True(mainVm.IsExecutableInfoEnabled);
        Assert.Equal(_fileSystem.Path.GetFileName(gamePath), mainVm.SelectedGameText);
    }

    [Fact]
    public void Initialize_WithStartupArgs_TargetsExecutable()
    {
        // Arrange
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService);
        string gamePath = @"C:\Games\TargetGame\game.exe";
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(gamePath)!);
        _fileSystem.File.WriteAllText(gamePath, "game binary");

        // Act
        mainVm.Initialize(new[] { gamePath });

        // Assert
        Assert.NotNull(mainVm.SelectedExecutableContext);
        Assert.Equal("game.exe", mainVm.SelectedExecutableContext.FileName);
    }

    [Fact]
    public void DeployCommand_CallsOrchestrator()
    {
        // Arrange
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService);
        string gamePath = @"C:\Games\TargetGame\game.exe";
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(gamePath)!);
        _fileSystem.File.WriteAllText(gamePath, "game binary");
        mainVm.TargetExecutable(gamePath);

        // Act
        try
        {
            mainVm.DeployCommand.Execute(null);
        }
        catch (NullReferenceException)
        {
            // Expected NRE on Application.Current.Shutdown()
        }

        // Assert
        // Verify dll was deployed in game folder
        Assert.True(_fileSystem.File.Exists(@"C:\Games\TargetGame\dxgi.dll"));
    }

    [Fact]
    public void UndeployCommand_NoFilesFound_HidesUndeployButton()
    {
        // Arrange
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService);
        string gamePath = @"C:\Games\TargetGame\game.exe";
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(gamePath)!);
        _fileSystem.File.WriteAllText(gamePath, "game binary");
        mainVm.TargetExecutable(gamePath);
        
        mainVm.IsUndeployVisible = true;

        // Act
        mainVm.UndeployCommand.Execute(null);

        // Assert
        Assert.False(mainVm.IsUndeployVisible);
        Assert.Empty(_messageBox.ShowCalls);
    }

    [Fact]
    public void UndeployCommand_FilesFound_UserConfirms_DeletesFiles()
    {
        // Arrange
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService);
        string gamePath = @"C:\Games\TargetGame\game.exe";
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(gamePath)!);
        _fileSystem.File.WriteAllText(gamePath, "game binary");
        
        // Write a ReShade.ini file to target game dir
        string iniPath = @"C:\Games\TargetGame\ReShade.ini";
        _fileSystem.File.WriteAllText(iniPath, "[ReShade]");

        mainVm.TargetExecutable(gamePath);
        Assert.True(mainVm.IsUndeployVisible);

        // Setup user to click Confirm/Uninstall
        _messageBox.NextResults.Enqueue(MessageBoxResult.Primary);

        // Act
        mainVm.UndeployCommand.Execute(null);

        // Assert
        Assert.Single(_messageBox.ShowCalls);
        Assert.Single(_snackbarService.ShowCalls);
        Assert.Contains(UIStrings.Undeploy_Confirm, _messageBox.ShowCalls[0].ContentText);
        Assert.Contains(UIStrings.Undeploy_Success, _snackbarService.ShowCalls[0].Title);
        // File was deleted
        Assert.False(_fileSystem.File.Exists(iniPath));
        // Undeploy visibility updated
        Assert.False(mainVm.IsUndeployVisible);
    }

    [Fact]
    public void UpdaterDownloadCompletedEvent_SetsIsDeployEnabled_IfGameTargeted()
    {
        // Arrange
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService);
        string gamePath = @"C:\Games\TargetGame\game.exe";
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(gamePath)!);
        _fileSystem.File.WriteAllText(gamePath, "game binary");
        mainVm.TargetExecutable(gamePath);
        
        // Manually toggle off deploy
        mainVm.IsDeployEnabled = false;

        // Act
        // Invoke the ReShadeDownloadCompleted event on UpdaterViewModel using reflection
        var fieldInfo = typeof(UpdaterViewModel).GetField("ReShadeDownloadCompleted", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var eventDelegate = (Action?)fieldInfo?.GetValue(_updaterVm);
        eventDelegate?.Invoke();

        // Assert
        Assert.True(mainVm.IsDeployEnabled);
    }
}
