using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ReShadeDeployer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFileSystem _fileSystem;
    private readonly DeploymentOrchestrator _deploymentOrchestrator;
    private readonly ReShadeUndeployer _reShadeUndeployer;
    private readonly ExecutableContextFactory _executableContextFactory;
    private readonly IMessageBox _messageBox;
    private readonly IIconExtractor _iconExtractor;
    private readonly ISnackbarService _snackbarService;

    // Child ViewModels
    public UpdaterViewModel Updater { get; }
    public SettingsViewModel Settings { get; }
    public AddonsViewModel Addons { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeployCommand))]
    [NotifyCanExecuteChangedFor(nameof(UndeployCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowExecutableInfoCommand))]
    [NotifyPropertyChangedFor(nameof(IsPresetNameShared))]
    private ExecutableContext? _selectedExecutableContext;

    [ObservableProperty]
    private ImageSource? _selectedGameIcon;

    [ObservableProperty]
    private string _selectedGameText = UIStrings.SelectedGame;

    [ObservableProperty]
    private string _selectedGameTooltip = string.Empty;

    [ObservableProperty]
    private ControlAppearance _selectGameButtonAppearance = ControlAppearance.Primary;

    [ObservableProperty]
    private ControlAppearance _deployButtonAppearance = ControlAppearance.Primary;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeployCommand))]
    private bool _isDeployEnabled;

    [ObservableProperty]
    private bool _isExecutableInfoEnabled;

    // Available graphics APIs for the UI
    public ObservableCollection<GraphicsApiOption> AvailableApis { get; } = new();

    // Undeploy button visibility
    [ObservableProperty]
    private bool _isUndeployVisible;

    public ObservableCollection<string> WarningTexts { get; } = new();

    [ObservableProperty]
    private bool _isPresetNameShared;

    public string LocalPresetName
    {
        get;
        set
        {
            if (SetProperty(ref field, RemoveInvalidPathCharacters(value)))
            {
                DeployCommand.NotifyCanExecuteChanged();
                UpdateIsPresetNameShared();
            }
        }
    } = string.Empty;

    public MainViewModel(
        IFileSystem fileSystem,
        DeploymentOrchestrator deploymentOrchestrator,
        ReShadeUndeployer reShadeUndeployer,
        ExecutableContextFactory executableContextFactory,
        UpdaterViewModel updater,
        SettingsViewModel settings,
        AddonsViewModel addons,
        IMessageBox messageBox,
        IIconExtractor iconExtractor,
        ISnackbarService snackbarService)
    {
        _fileSystem = fileSystem;
        _deploymentOrchestrator = deploymentOrchestrator;
        _reShadeUndeployer = reShadeUndeployer;
        _executableContextFactory = executableContextFactory;
        _messageBox = messageBox;
        _iconExtractor = iconExtractor;
        _snackbarService = snackbarService;

        Updater = updater;
        Settings = settings;
        Addons = addons;

        InitializeAvailableApis();

        // When a ReShade download finishes, re-enable deploy if a game is already targeted
        Updater.ReShadeDownloadCompleted += () =>
        {
            if (SelectedExecutableContext != null)
                IsDeployEnabled = true;
        };
        
        WarningTexts.CollectionChanged += (sender, args) => OnWarningTextsChanged();
        Addons.AddonSupportCheckedChanged += (isChecked) => UpdateVulkanWithAddonSupportWarning();
        Settings.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(Settings.UseLocalPresets))
            {
                DeployCommand.NotifyCanExecuteChanged();
            }
        };
    }

    public void Initialize(string[] startupArgs)
    {
        if (startupArgs.Length > 0 && !string.IsNullOrEmpty(startupArgs[0]))
            TargetExecutable(startupArgs[0]);

        Addons.IsAddonSupportChecked = startupArgs.Contains("-addon-support");

        Updater.Initialize();

        Addons.Refresh(Addons.IsAddonSupportChecked, SelectedExecutableContext);

#if DEBUG
        IsExecutableInfoEnabled = true;
#endif
    }

    private void InitializeAvailableApis()
    {
        AvailableApis.Add(new GraphicsApiOption(UIStrings.DirectX10, UIStrings.DirectX10_Tooltip, GraphicsApi.DXGI, isSelected: true));
        AvailableApis.Add(new GraphicsApiOption(UIStrings.DirectX9, UIStrings.DirectX9_Tooltip, GraphicsApi.D3D9));
        AvailableApis.Add(new GraphicsApiOption(UIStrings.OpenGL, UIStrings.OpenGL_Tooltip, GraphicsApi.OpenGL));
        AvailableApis.Add(new GraphicsApiOption(UIStrings.Vulkan, UIStrings.Vulkan_Tooltip, GraphicsApi.Vulkan));

        foreach (var availableApi in AvailableApis)
            availableApi.SelectionChanged += OnApiSelectionChanged;
    }

    [RelayCommand]
    private void SelectGame(string? executablePath)
    {
        if (!string.IsNullOrEmpty(executablePath))
            TargetExecutable(executablePath);
    }

    [RelayCommand]
    private void ToggleAddonSupport()
    {
        Addons.Refresh(Addons.IsAddonSupportChecked, SelectedExecutableContext);
    }

    [RelayCommand(CanExecute = nameof(CanDeploy))]
    private async Task Deploy()
    {
        if (SelectedExecutableContext == null)
            return;

        var api = AvailableApis.FirstOrDefault(a => a.IsSelected)?.Api ?? GraphicsApi.DXGI;
        var addonSupport = Addons.IsAddonSupportChecked;
        var presetName = string.IsNullOrEmpty(LocalPresetName) ? null : LocalPresetName;

        try
        {
            await _deploymentOrchestrator.DeployReShadeForExecutable(SelectedExecutableContext, api, addonSupport, Addons.GetSelectedAddons(), presetName);
        }
        catch (DeploymentException ex)
        {
            await _messageBox.ShowException(UIStrings.DeployError_Title, ex, UIStrings.OK);
            return;
        }

        UpdateUndeployVisibility();
    }

    private bool CanDeploy()
    {
        if (SelectedExecutableContext == null || !IsDeployEnabled)
            return false;

        if (Settings.UseLocalPresets && string.IsNullOrWhiteSpace(LocalPresetName))
            return false;

        return true;
    }

    [RelayCommand(CanExecute = nameof(CanUndeploy))]
    private async Task Undeploy()
    {
        if (SelectedExecutableContext == null)
            return;

        var files = _reShadeUndeployer.FindReShadeFiles(SelectedExecutableContext.DirectoryPath);

        if (files.Count == 0)
        {
            UpdateUndeployVisibility();
            return;
        }

        // Build file list for dialog (show only file names)
        string fileList = string.Join("\n", files.Select(_fileSystem.Path.GetFileName));
        string message = UIStrings.Undeploy_Confirm + "\n\n" + fileList;

        var result = await _messageBox.Show(UIStrings.Undeploy_Confirm_Title,
            message,
            primaryButtonText: UIStrings.Undeploy, ControlAppearance.Caution,
            closeButtonText: UIStrings.Cancel);

        if (result == MessageBoxResult.Primary)
        {
            _reShadeUndeployer.Undeploy(SelectedExecutableContext.DirectoryPath);
            _snackbarService.Show(UIStrings.Undeploy_Success, ControlAppearance.Success, SymbolRegular.Delete20);

            // Re-scan to update button visibility
            UpdateUndeployVisibility();
        }
    }

    private bool CanUndeploy() => SelectedExecutableContext != null;

    [RelayCommand(CanExecute = nameof(CanShowExecutableInfo))]
    private void ShowExecutableInfo()
    {
        if (SelectedExecutableContext != null)
            _ = _messageBox.Show("Executable Info", SelectedExecutableContext.ToString());
    }

    private bool CanShowExecutableInfo() => SelectedExecutableContext != null;

    public void TargetExecutable(string executablePath)
    {
        if (!_fileSystem.File.Exists(executablePath))
            return;

        SelectedExecutableContext = _executableContextFactory.Create(executablePath);

        SelectedGameIcon = _iconExtractor.Extract(executablePath);
        SelectedGameText = Path.GetFileName(executablePath);
        SelectedGameTooltip = executablePath;
        SelectGameButtonAppearance = ControlAppearance.Secondary;

        IsDeployEnabled = true;

        LocalPresetName = GetDefaultPresetFolderName(executablePath);

        WarningTexts.Clear();

        if (SelectedExecutableContext.Banned)
            WarningTexts.Add(UIStrings.BannedGameWarning);
        
        // A game might include multiple APIs, so prioritize: Vulkan > DXGI > D3D9 > OpenGL
        // or leave unchanged if none are found
        GraphicsApi? recommendedApi = SelectedExecutableContext switch
        {
            {IsVulkan: true} => GraphicsApi.Vulkan,
            {IsDXGI: true} => GraphicsApi.DXGI,
            {IsD3D9: true} => GraphicsApi.D3D9,
            {IsOpenGL: true} => GraphicsApi.OpenGL,
            _ => null
        };

        if (recommendedApi != null)
            SetApiSelection(recommendedApi.Value);

        foreach (var availableApi in AvailableApis)
            availableApi.IsDetected = availableApi.Api == recommendedApi;

        Addons.Refresh(Addons.IsAddonSupportChecked, SelectedExecutableContext);
        UpdateUndeployVisibility();
        UpdateIsPresetNameShared();
    }

    private void UpdateUndeployVisibility()
    {
        if (SelectedExecutableContext == null)
        {
            IsUndeployVisible = false;
            return;
        }

        var files = _reShadeUndeployer.FindReShadeFiles(SelectedExecutableContext.DirectoryPath);
        IsUndeployVisible = files.Count > 0;
    }

    private void SetApiSelection(GraphicsApi api)
    {
        foreach (var availableApi in AvailableApis)
            availableApi.IsSelected = (availableApi.Api == api);

        UpdateVulkanWithAddonSupportWarning();
    }

    private void UpdateVulkanWithAddonSupportWarning()
    {
        GraphicsApi api = AvailableApis.FirstOrDefault(a => a.IsSelected)?.Api ?? GraphicsApi.DXGI;
        
        if (api == GraphicsApi.Vulkan && Addons.IsAddonSupportChecked)
        {
            if (!WarningTexts.Contains(UIStrings.Vulkan_Addon_Warning))
                WarningTexts.Add(UIStrings.Vulkan_Addon_Warning);
        }
        else
            WarningTexts.Remove(UIStrings.Vulkan_Addon_Warning);
    }

    private void OnApiSelectionChanged(GraphicsApiOption selectedOption)
    {
        SetApiSelection(selectedOption.Api);
    }
    
    private void OnWarningTextsChanged()
    {
        DeployButtonAppearance = WarningTexts.Count > 0 ? ControlAppearance.Caution : ControlAppearance.Primary;
    }

    private void UpdateIsPresetNameShared()
    {
        if (SelectedExecutableContext == null || string.IsNullOrWhiteSpace(LocalPresetName))
        {
            IsPresetNameShared = false;
            return;
        }

        // Check if the named preset folder exists in the local Presets directory
        string presetFolder = _fileSystem.Path.Combine(Paths.Presets, LocalPresetName);
        if (!_fileSystem.Directory.Exists(presetFolder))
        {
            IsPresetNameShared = false;
            return;
        }

        // The expected PresetPath value that a correctly configured game would have
        string expectedPresetPath = _fileSystem.Path.Combine(Paths.Presets, LocalPresetName, "ReShadePreset.ini");

        // Check whether the game's ReShade.ini already points to this preset
        string reshadeIniPath = _fileSystem.Path.Combine(SelectedExecutableContext.DirectoryPath, "ReShade.ini");
        try
        {
            if (_fileSystem.File.Exists(reshadeIniPath))
            {
                string content = _fileSystem.File.ReadAllText(reshadeIniPath);
                var match = GetPresetPathValue().Match(content);
                if (match.Success)
                {
                    string currentPresetPath = match.Groups[1].Value.Trim();
                    if (string.Equals(currentPresetPath, expectedPresetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        IsPresetNameShared = false;
                        return;
                    }
                }
            }
        }
        catch (Exception) { /* ignore */ }

        // Preset folder exists but this game doesn't point to it yet — it will be shared
        IsPresetNameShared = true;
    }

    private string RemoveInvalidPathCharacters(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return string.Concat(value.Where(c => !_fileSystem.Path.GetInvalidFileNameChars().Contains(c)));
    }

    private string GetDefaultPresetFolderName(string executablePath)
    {
        string fileName = _fileSystem.Path.GetFileName(executablePath);
        string defaultTextBoxText = StripBuildSuffixes().Replace(fileName, "").TrimEnd('-', '_');

        try
        {
            // Check if ReShade.ini already exists and contains PresetPath with Presets folder
            string directoryPath = _fileSystem.Path.GetDirectoryName(executablePath) ?? "";
            string currentReShadeIni = _fileSystem.Path.Combine(directoryPath, "ReShade.ini");
            if (_fileSystem.File.Exists(currentReShadeIni))
            {
                var reshadeIniContent = _fileSystem.File.ReadAllTextAsync(currentReShadeIni).Result;
                var presetPathMatch = GetPresetPathValue().Match(reshadeIniContent);
                    
                if (presetPathMatch.Success)
                {
                    string currentPresetPath = presetPathMatch.Groups[1].Value.Trim();
                    if (currentPresetPath.StartsWith(Paths.Presets, StringComparison.OrdinalIgnoreCase))
                    {
                        // Extract the inner folder name from the path
                        // Expected format: Presets\SomeFolder\ReShadePreset.ini or Presets/SomeFolder/ReShadePreset.ini
                        var relativePath = currentPresetPath.Substring(Paths.Presets.Length).TrimStart('\\', '/');
                        var pathParts = relativePath.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
                            
                        if (pathParts.Length > 0)
                        {
                            // Use the first folder name as the default
                            defaultTextBoxText = pathParts[0];
                        }
                    }
                }
            }
        }
        catch (Exception) { /* ignore */ }

        return defaultTextBoxText;
    }

    [GeneratedRegex(@"(?i)(?:[-_]?(?:Win64|Win32|x64|x86))?(?:[-_]?(?:Shipping|Release|Retail|DebugGame|Test))?(?:[-_]?(?:dx11|dx12|vulkan|vk))?(?:[-_]?(?:Steam|Epic|GOG|EGS))?(?:\.exe)?$")]
    private static partial Regex StripBuildSuffixes();
    
    [GeneratedRegex(@"^PresetPath\s?=\s?(.*)$", RegexOptions.Multiline)]
    private static partial Regex GetPresetPathValue();
}