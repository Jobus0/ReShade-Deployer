using ReShadeDeployer.Registry;
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
    private readonly RegistryHelper _registryHelper;
    
    private readonly ReShadeUndeployer _undeployer;
    private readonly ExecutableContextFactory _factory;
    
    private readonly UpdaterViewModel _updaterVm;
    private readonly SettingsViewModel _settingsVm;
    private readonly AddonsViewModel _addonsVm;

    public MainViewModelTests()
    {
        _fileSystem = new MockFileSystem();
        _registry = new MockRegistry();
        _registryHelper = new RegistryHelper(_registry);
        _messageBox = new MockMessageBox();
        _snackbarService = new MockSnackbarService();
        _iconExtractor = new MockIconExtractor();
        _config = new MockConfig();
        
        var fileHelper = new FileHelper(_fileSystem);
        var downloadService = new MockDownloadService(_fileSystem);

        _appUpdater = new AppUpdater(_fileSystem, downloadService, _messageBox, _registryHelper);
        _reShadeUpdater = new ReShadeUpdater(_fileSystem, fileHelper, downloadService, _messageBox);
        _vulkanDeployer = new VulkanSystemWideDeployer(_fileSystem, _registry);
        _dllDeployer = new DllDeployer(_fileSystem, _vulkanDeployer);
        _iniDeployer = new IniDeployer(_fileSystem);
        _presetDeployer = new PresetDeployer(_fileSystem);
        _addonsDeployer = new AddonsDeployer(_fileSystem, fileHelper);
        
        _orchestrator = new DeploymentOrchestrator(
            _fileSystem,
            fileHelper,
            _dllDeployer,
            _iniDeployer,
            _presetDeployer,
            _addonsDeployer,
            _messageBox
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
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService, _config);
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
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService, _config);
        string gamePath = @"C:\Games\TargetGame\game.exe";
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(gamePath)!);
        _fileSystem.File.WriteAllText(gamePath, "game binary");

        // Act
        mainVm.TargetExecutable(gamePath);

        // Assert
        Assert.NotNull(mainVm.SelectedExecutableContext);
        Assert.Equal("game.exe", mainVm.SelectedExecutableContext.FileName);
        Assert.True(mainVm.IsDeployEnabled);
        Assert.Equal(_fileSystem.Path.GetFileName(gamePath), mainVm.SelectedGameText);
    }

    [Fact]
    public void Initialize_WithStartupArgs_TargetsExecutable()
    {
        // Arrange
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService, _config);
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
    public void CentralizedPresetName_WhenSet_RemovesInvalidPathCharacters()
    {
        // Arrange
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService, _config);

        // Act
        mainVm.CentralizedPresetName = "Bad<Folder>:\"/\\|?*Name";

        // Assert
        Assert.Equal("BadFolderName", mainVm.CentralizedPresetName);
    }

    [Fact]
    public void TargetExecutable_WhenExistingPresetPathContainsInvalidPathCharacters_SanitizesCentralizedPresetName()
    {
        // Arrange
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService, _config);
        string gamePath = @"C:\Games\TargetGame\game.exe";
        string gameDirectory = _fileSystem.Path.GetDirectoryName(gamePath)!;
        _fileSystem.Directory.CreateDirectory(gameDirectory);
        _fileSystem.File.WriteAllText(gamePath, "game binary");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(gameDirectory, "ReShade.ini"), $"PresetPath={Paths.Presets}\\Bad<Folder>:\"|?*Name\\ReShadePreset.ini");

        // Act
        mainVm.TargetExecutable(gamePath);

        // Assert
        Assert.Equal("BadFolderName", mainVm.CentralizedPresetName);
    }

    [Fact]
    public void DeployCommand_CallsOrchestrator()
    {
        // Arrange
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService, _config);
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
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService, _config);
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
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService, _config);
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
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService, _config);
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

    [Fact]
    public void UpdateIsPresetNameShared_WhenReShadeIniDoesNotExist_SetsIsPresetNameSharedToTrue()
    {
        // Arrange
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService, _config);
        string gamePath = @"C:\Games\TargetGame\game.exe";
        string gameDirectory = _fileSystem.Path.GetDirectoryName(gamePath)!;
        _fileSystem.Directory.CreateDirectory(gameDirectory);
        _fileSystem.File.WriteAllText(gamePath, "game binary");

        // Create preset folder
        string presetFolder = _fileSystem.Path.Combine(Paths.Presets, "MyPreset");
        _fileSystem.Directory.CreateDirectory(presetFolder);
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(presetFolder, "ReShadePreset.ini"), "preset content");

        mainVm.TargetExecutable(gamePath);
        mainVm.CentralizedPresetName = "MyPreset";

        // Act
        // UpdateIsPresetNameShared is called automatically when CentralizedPresetName is set

        // Assert
        Assert.True(mainVm.IsPresetNameShared);
    }

    [Fact]
    public void UpdateIsPresetNameShared_WhenReShadeIniPresetPathMatchesExpected_SetsIsPresetNameSharedToFalse()
    {
        // Arrange
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService, _config);
        string gamePath = @"C:\Games\TargetGame\game.exe";
        string gameDirectory = _fileSystem.Path.GetDirectoryName(gamePath)!;
        _fileSystem.Directory.CreateDirectory(gameDirectory);
        _fileSystem.File.WriteAllText(gamePath, "game binary");

        // Create preset folder
        string presetFolder = _fileSystem.Path.Combine(Paths.Presets, "MyPreset");
        _fileSystem.Directory.CreateDirectory(presetFolder);
        string expectedPresetPath = _fileSystem.Path.Combine(Paths.Presets, "MyPreset", "ReShadePreset.ini");
        _fileSystem.File.WriteAllText(expectedPresetPath, "preset content");

        // Create ReShade.ini with matching PresetPath
        string reshadeIniPath = _fileSystem.Path.Combine(gameDirectory, "ReShade.ini");
        _fileSystem.File.WriteAllText(reshadeIniPath, $"PresetPath={expectedPresetPath}");

        mainVm.TargetExecutable(gamePath);
        mainVm.CentralizedPresetName = "MyPreset";

        // Act
        // UpdateIsPresetNameShared is called automatically when CentralizedPresetName is set

        // Assert
        Assert.False(mainVm.IsPresetNameShared);
    }

    [Fact]
    public void UpdateIsPresetNameShared_WhenReShadeIniPresetPathDoesNotMatchExpected_SetsIsPresetNameSharedToTrue()
    {
        // Arrange
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService, _config);
        string gamePath = @"C:\Games\TargetGame\game.exe";
        string gameDirectory = _fileSystem.Path.GetDirectoryName(gamePath)!;
        _fileSystem.Directory.CreateDirectory(gameDirectory);
        _fileSystem.File.WriteAllText(gamePath, "game binary");

        // Create preset folder
        string presetFolder = _fileSystem.Path.Combine(Paths.Presets, "MyPreset");
        _fileSystem.Directory.CreateDirectory(presetFolder);
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(presetFolder, "ReShadePreset.ini"), "preset content");

        // Create ReShade.ini with different PresetPath
        string reshadeIniPath = _fileSystem.Path.Combine(gameDirectory, "ReShade.ini");
        string differentPresetPath = _fileSystem.Path.Combine(Paths.Presets, "DifferentPreset", "ReShadePreset.ini");
        _fileSystem.File.WriteAllText(reshadeIniPath, $"PresetPath={differentPresetPath}");

        mainVm.TargetExecutable(gamePath);
        mainVm.CentralizedPresetName = "MyPreset";

        // Act
        // UpdateIsPresetNameShared is called automatically when CentralizedPresetName is set

        // Assert
        Assert.True(mainVm.IsPresetNameShared);
    }

    [Fact]
    public void UpdateVulkanWithAddonSupportWarning_WhenVulkanSelectedAndAddonSupportChecked_AddsWarning()
    {
        // Arrange
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService, _config);

        // Act
        _addonsVm.IsAddonSupportChecked = true;
        var vulkanApi = mainVm.AvailableApis.FirstOrDefault(a => a.Api == GraphicsApi.Vulkan);
        vulkanApi!.IsSelected = true;

        // Assert
        Assert.Contains(UIStrings.Vulkan_Addon_Warning, mainVm.WarningTexts);
    }

    [Fact]
    public void UpdateVulkanWithAddonSupportWarning_WhenVulkanSelectedButAddonSupportNotChecked_RemovesWarning()
    {
        // Arrange
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService, _config);
        _addonsVm.IsAddonSupportChecked = true;
        var vulkanApi = mainVm.AvailableApis.FirstOrDefault(a => a.Api == GraphicsApi.Vulkan);
        vulkanApi!.IsSelected = true;

        // Act
        _addonsVm.IsAddonSupportChecked = false;

        // Assert
        Assert.DoesNotContain(UIStrings.Vulkan_Addon_Warning, mainVm.WarningTexts);
    }

    [Fact]
    public void UpdateVulkanWithAddonSupportWarning_WhenNonVulkanSelected_RemovesWarning()
    {
        // Arrange
        var mainVm = new MainViewModel(_fileSystem, _orchestrator, _undeployer, _factory, _updaterVm, _settingsVm, _addonsVm, _messageBox, _iconExtractor, _snackbarService, _config);
        _addonsVm.IsAddonSupportChecked = true;
        var vulkanApi = mainVm.AvailableApis.FirstOrDefault(a => a.Api == GraphicsApi.Vulkan);
        vulkanApi!.IsSelected = true;

        // Act
        var dxgiApi = mainVm.AvailableApis.FirstOrDefault(a => a.Api == GraphicsApi.DXGI);
        vulkanApi!.IsSelected = false;
        dxgiApi!.IsSelected = true;

        // Assert
        Assert.DoesNotContain(UIStrings.Vulkan_Addon_Warning, mainVm.WarningTexts);
    }
}
