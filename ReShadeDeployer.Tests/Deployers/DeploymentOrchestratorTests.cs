using Testably.Abstractions.Testing;
using Wpf.Ui.Controls;
using static ReShadeDeployer.Tests.TestHelpers;

namespace ReShadeDeployer.Tests.Deployers;

public class DeploymentOrchestratorTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly MockRegistry _registry;
    private readonly MockMessageBox _messageBox;
    private readonly MockSnackbarService _snackbarService;
    private readonly MockConfig _config;
    
    private readonly VulkanSystemWideDeployer _vulkanDeployer;
    private readonly DllDeployer _dllDeployer;
    private readonly IniDeployer _iniDeployer;
    private readonly PresetDeployer _presetDeployer;
    private readonly AddonsDeployer _addonsDeployer;
    private readonly DeploymentOrchestrator _orchestrator;
    
    private readonly ExecutableContext _executableContext;

    public DeploymentOrchestratorTests()
    {
        _executableContext = CreateExecutableContext(isX64: true);
        _fileSystem = CreateMockFileSystem(_executableContext.DirectoryPath);
        _registry = new MockRegistry();
        _messageBox = new MockMessageBox();
        _snackbarService = new MockSnackbarService();
        _config = new MockConfig();
        
        var fileHelper = new FileHelper(_fileSystem);
        _vulkanDeployer = new VulkanSystemWideDeployer(_fileSystem, _registry);
        _dllDeployer = new DllDeployer(_fileSystem, _vulkanDeployer);
        _iniDeployer = new IniDeployer(_fileSystem);
        _presetDeployer = new PresetDeployer(_fileSystem);
        _addonsDeployer = new AddonsDeployer(_fileSystem, fileHelper);
        
        _orchestrator = new DeploymentOrchestrator(
            _fileSystem,
            _dllDeployer,
            _iniDeployer,
            _presetDeployer,
            _addonsDeployer,
            _messageBox
        );

        // Set up Paths directories and dummy files
        _fileSystem.Directory.CreateDirectory(Paths.Root);
        _fileSystem.Directory.CreateDirectory(Paths.Dlls);
        _fileSystem.Directory.CreateDirectory(Paths.AddonDlls);
        _fileSystem.Directory.CreateDirectory(Paths.CommonPathLocal);
        _fileSystem.Directory.CreateDirectory(Paths.Addons);

        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(Paths.Dlls, "ReShade64.dll"), "dll64");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(Paths.Dlls, "ReShade32.dll"), "dll32");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(Paths.AddonDlls, "ReShade64.dll"), "addondll64");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(Paths.AddonDlls, "ReShade32.dll"), "addondll32");
    }

    [Fact]
    public async Task DeployReShadeForExecutable_D3D9WithD3D8Exe_ShowsNoticeAndDeploys()
    {
        // Arrange
        var context = CreateExecutableContext(isD3D8: true);
        _fileSystem.Directory.CreateDirectory(context.DirectoryPath);
        
        // Act
        // This will attempt to deploy DXGI/D3D9 but we wrap it in try-catch to avoid Application.Current.Shutdown() crash
        try
        {
            await _orchestrator.DeployReShadeForExecutable(context, GraphicsApi.D3D9, addonSupport: false, new List<AddonItem>());
        }
        catch (NullReferenceException)
        {
            // Expected NRE on Application.Current.Shutdown() since Application.Current is null
        }

        // Assert
        // MessageBox shows D3D8 informational message
        Assert.Contains(_messageBox.ShowCalls, call => call.ContentText == UIStrings.D3D8_Info);
        
        // Assert dll was still deployed
        string expectedDllPath = _fileSystem.Path.Combine(context.DirectoryPath, "d3d9.dll");
        Assert.True(_fileSystem.File.Exists(expectedDllPath));
    }

    [Fact]
    public async Task DeployReShadeForExecutable_SuccessfulDeployment_DeploysDllAndIni()
    {
        // Arrange
        var addons = new List<AddonItem>();

        // Act
        try
        {
            await _orchestrator.DeployReShadeForExecutable(_executableContext, GraphicsApi.DXGI, addonSupport: false, addons);
        }
        catch (NullReferenceException)
        {
            // Expected because of Application.Current.Shutdown()
        }

        // Assert
        // Dll is deployed
        Assert.True(_fileSystem.File.Exists(_fileSystem.Path.Combine(_executableContext.DirectoryPath, "dxgi.dll")));
        // Ini is deployed
        Assert.True(_fileSystem.File.Exists(_fileSystem.Path.Combine(_executableContext.DirectoryPath, "ReShade.ini")));
    }

    [Fact]
    public async Task DeployReShadeForExecutable_PresetDeployment_UserDeclinesOverwrite_DoesNotCopyPreset()
    {
        // Arrange
        // Create templates and target files
        _fileSystem.File.WriteAllText(Paths.ReShadePresetIni, "template preset content");
        
        string targetPresetPath = _fileSystem.Path.Combine(_executableContext.DirectoryPath, "ReShadePreset.ini");
        _fileSystem.File.WriteAllText(targetPresetPath, "original preset content");

        // User chooses NOT to overwrite
        _messageBox.NextResults.Enqueue(MessageBoxResult.Secondary);

        // Act
        try
        {
            await _orchestrator.DeployReShadeForExecutable(_executableContext, GraphicsApi.DXGI, addonSupport: false, new List<AddonItem>());
        }
        catch (NullReferenceException)
        {
            // Expected NRE
        }

        // Assert
        Assert.Single(_messageBox.ShowCalls);
        Assert.Contains(UIStrings.PresetOverwrite, _messageBox.ShowCalls[0].ContentText);
        // Original content is preserved
        Assert.Equal("original preset content", _fileSystem.File.ReadAllText(targetPresetPath));
    }

    [Fact]
    public async Task DeployReShadeForExecutable_PresetDeployment_UserAcceptsOverwrite_CopiesPreset()
    {
        // Arrange
        _fileSystem.File.WriteAllText(Paths.ReShadePresetIni, "template preset content");
        
        string targetPresetPath = _fileSystem.Path.Combine(_executableContext.DirectoryPath, "ReShadePreset.ini");
        _fileSystem.File.WriteAllText(targetPresetPath, "original preset content");

        // User chooses TO overwrite (Primary button)
        _messageBox.NextResults.Enqueue(MessageBoxResult.Primary);

        // Act
        try
        {
            await _orchestrator.DeployReShadeForExecutable(_executableContext, GraphicsApi.DXGI, addonSupport: false, new List<AddonItem>());
        }
        catch (NullReferenceException)
        {
            // Expected NRE
        }

        // Assert
        Assert.Equal("template preset content", _fileSystem.File.ReadAllText(targetPresetPath));
    }

    [Fact]
    public async Task DeployReShadeForExecutable_ThrowsDeploymentException_WhenUnderlyingDeployerFails()
    {
        // Arrange
        // Use an invalid directory path that will cause file operations to throw
        var invalidContext = CreateExecutableContext(directoryPath: "C:\\Invalid<|>?*Path");

        // Act & Assert
        await Assert.ThrowsAsync<DeploymentException>(() =>
            _orchestrator.DeployReShadeForExecutable(invalidContext, GraphicsApi.DXGI, addonSupport: false, new List<AddonItem>()));
    }

    [Fact]
    public async Task DeployReShadeForExecutable_CentralizedPresetsEnabled_DeploysToCentralizedPresetsFolder()
    {
        // Arrange
        _config.CentralizedPresets = true;
        _fileSystem.File.WriteAllText(Paths.ReShadePresetIni, "template preset content");
        
        // Act
        try
        {
            await _orchestrator.DeployReShadeForExecutable(_executableContext, GraphicsApi.DXGI, addonSupport: false, new List<AddonItem>(), "TestPresetFolder");
        }
        catch (NullReferenceException) { }

        // Assert
        string expectedCentralizedPresetPath = _fileSystem.Path.Combine(Paths.Presets, "TestPresetFolder", "ReShadePreset.ini");
        Assert.True(_fileSystem.File.Exists(expectedCentralizedPresetPath));
        Assert.Equal("template preset content", _fileSystem.File.ReadAllText(expectedCentralizedPresetPath));

        string reshadeIniPath = _fileSystem.Path.Combine(_executableContext.DirectoryPath, "ReShade.ini");
        Assert.True(_fileSystem.File.Exists(reshadeIniPath));
        string reshadeIniContent = _fileSystem.File.ReadAllText(reshadeIniPath);
        Assert.Contains($"PresetPath={expectedCentralizedPresetPath}", reshadeIniContent);
    }

    [Fact]
    public async Task DeployReShadeForExecutable_CentralizedPresetsEnabled_OverwritePromptDeclined_DoesNotCopyPresetButDeploysOthers()
    {
        // Arrange
        _config.CentralizedPresets = true;
        _fileSystem.File.WriteAllText(Paths.ReShadePresetIni, "template preset content");
        
        string centralizedPresetDir = _fileSystem.Path.Combine(Paths.Presets, "TestPresetFolder");
        _fileSystem.Directory.CreateDirectory(centralizedPresetDir);
        string centralizedPresetPath = _fileSystem.Path.Combine(centralizedPresetDir, "ReShadePreset.ini");
        _fileSystem.File.WriteAllText(centralizedPresetPath, "original preset content");

        // Dialog (overwrite prompt) returns Secondary/Cancel
        _messageBox.NextResults.Enqueue(MessageBoxResult.Secondary);

        // Act
        try
        {
            await _orchestrator.DeployReShadeForExecutable(_executableContext, GraphicsApi.DXGI, addonSupport: false, new List<AddonItem>(), "TestPresetFolder");
        }
        catch (NullReferenceException) { }

        // Assert
        Assert.Single(_messageBox.ShowCalls);
        Assert.Contains(UIStrings.PresetOverwrite, _messageBox.ShowCalls[0].ContentText);

        // Original content is preserved
        Assert.Equal("original preset content", _fileSystem.File.ReadAllText(centralizedPresetPath));

        // ReShade.ini still points to it
        string reshadeIniPath = _fileSystem.Path.Combine(_executableContext.DirectoryPath, "ReShade.ini");
        Assert.True(_fileSystem.File.Exists(reshadeIniPath));
        string reshadeIniContent = _fileSystem.File.ReadAllText(reshadeIniPath);
        Assert.Contains($"PresetPath={centralizedPresetPath}", reshadeIniContent);
    }
}
