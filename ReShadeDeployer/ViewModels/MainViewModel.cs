using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
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

    [ObservableProperty]
    private string? _warningText;

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

        try
        {
            await _deploymentOrchestrator.DeployReShadeForExecutable(SelectedExecutableContext, api, addonSupport, Addons.GetSelectedAddons());
        }
        catch (DeploymentException ex)
        {
            await _messageBox.ShowException(UIStrings.DeployError_Title, ex, UIStrings.OK);
            return;
        }

        UpdateUndeployVisibility();
    }

    private bool CanDeploy() => SelectedExecutableContext != null && IsDeployEnabled;

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
        DeployButtonAppearance = SelectedExecutableContext.Banned ? ControlAppearance.Caution : ControlAppearance.Primary;

        WarningText = null;

        if (SelectedExecutableContext.Banned)
            WarningText = UIStrings.BannedGameWarning;

        IsExecutableInfoEnabled = true;

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
    }

    private void OnApiSelectionChanged(GraphicsApiOption selectedOption)
    {
        SetApiSelection(selectedOption.Api);
    }
}