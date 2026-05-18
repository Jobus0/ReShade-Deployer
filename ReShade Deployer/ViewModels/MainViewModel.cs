using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Common;

namespace ReShadeDeployer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DeploymentOrchestrator _deploymentOrchestrator;
    private readonly ReShadeUndeployer _reShadeUndeployer;
    private readonly IMessageBox _messageBox;

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
    private string _selectGameButtonText = UIStrings.SelectGameButton;

    [ObservableProperty]
    private ControlAppearance _selectGameButtonAppearance = ControlAppearance.Primary;

    [ObservableProperty]
    private string _deployButtonContent = UIStrings.DeployButton;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeployCommand))]
    private bool _isDeployEnabled;

    [ObservableProperty]
    private bool _isExecutableInfoEnabled;

    // Selected and highlighted graphics API
    [ObservableProperty]
    private GraphicsApi _selectedApi = GraphicsApi.DXGI;

    [ObservableProperty]
    private GraphicsApi? _highlightedApi;

    // Undeploy button visibility
    [ObservableProperty]
    private bool _isUndeployVisible;

    public MainViewModel(
        DeploymentOrchestrator deploymentOrchestrator,
        ReShadeUndeployer reShadeUndeployer,
        VulkanSystemWideDeployer vulkanSystemWideDeployer,
        UpdaterViewModel updater,
        SettingsViewModel settings,
        AddonsViewModel addons,
        IMessageBox messageBox)
    {
        _deploymentOrchestrator = deploymentOrchestrator;
        _reShadeUndeployer = reShadeUndeployer;
        _messageBox = messageBox;

        Updater = updater;
        Settings = settings;
        Addons = addons;

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
    }

    /// <summary>Raised when the window should hide itself (e.g. during deploy).</summary>
    public event System.Action? RequestHide;

    /// <summary>Raised when the window should show itself again.</summary>
    public event System.Action? RequestShow;

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
    private void Deploy()
    {
        if (SelectedExecutableContext == null)
            return;

        RequestHide?.Invoke();

        var api = SelectedApi;
        var addonSupport = Addons.IsAddonSupportChecked;

        try
        {
            _deploymentOrchestrator.DeployReShadeForExecutable(
                SelectedExecutableContext, api, addonSupport, Addons.GetSelectedAddons());
        }
        catch (DeploymentException ex)
        {
            _messageBox.Show(ex, UIStrings.DeployError_Title, UIStrings.OK);
            RequestShow?.Invoke();
            return;
        }

        UpdateUndeployVisibility();
        RequestShow?.Invoke();
    }

    private bool CanDeploy() => SelectedExecutableContext != null && IsDeployEnabled;

    [RelayCommand(CanExecute = nameof(CanUndeploy))]
    private void Undeploy()
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
        string fileList = string.Join("\n", files.Select(Path.GetFileName));
        string message = UIStrings.Undeploy_Confirm + "\n\n" + fileList;

        var result = _messageBox.Show(
            message,
            UIStrings.Undeploy_Confirm_Title,
            UIStrings.Undeploy,
            UIStrings.Cancel,
            ControlAppearance.Caution);

        if (result == IMessageBox.Result.Primary)
        {
            _reShadeUndeployer.Undeploy(SelectedExecutableContext.DirectoryPath);
            _messageBox.Show(UIStrings.Undeploy_Success, UIStrings.Success);

            // Re-scan to update button visibility
            UpdateUndeployVisibility();
        }
    }

    private bool CanUndeploy() => SelectedExecutableContext != null;

    [RelayCommand(CanExecute = nameof(CanShowExecutableInfo))]
    private void ShowExecutableInfo()
    {
        if (SelectedExecutableContext != null)
            _messageBox.Show(SelectedExecutableContext.ToString(), "Executable Info");
    }

    private bool CanShowExecutableInfo() => SelectedExecutableContext != null;

    public void TargetExecutable(string executablePath)
    {
        if (!File.Exists(executablePath))
            return;

        SelectedExecutableContext = new ExecutableContext(executablePath);

        SelectGameButtonText = executablePath;
        SelectGameButtonAppearance = ControlAppearance.Secondary;

        DeployButtonContent = string.Format(UIStrings.DeployButton_Targeted, SelectedExecutableContext.FileName);
        IsDeployEnabled = true;
        IsExecutableInfoEnabled = true;

        // A game might include multiple APIs, so prioritize: Vulkan > DXGI > D3D9 > OpenGL
        if (SelectedExecutableContext.IsVulkan)
        {
            SelectedApi = GraphicsApi.Vulkan;
            HighlightedApi = GraphicsApi.Vulkan;
        }
        else if (SelectedExecutableContext.IsDXGI)
        {
            SelectedApi = GraphicsApi.DXGI;
            HighlightedApi = GraphicsApi.DXGI;
        }
        else if (SelectedExecutableContext.IsD3D9)
        {
            SelectedApi = GraphicsApi.D3D9;
            HighlightedApi = GraphicsApi.D3D9;
        }
        else if (SelectedExecutableContext.IsOpenGL)
        {
            SelectedApi = GraphicsApi.OpenGL;
            HighlightedApi = GraphicsApi.OpenGL;
        }
        else
        {
            HighlightedApi = null;
        }

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
}
