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

    // API radio buttons
    [ObservableProperty]
    private bool _isDxgiChecked = true;

    [ObservableProperty]
    private bool _isD3d9Checked;

    [ObservableProperty]
    private bool _isOpenGlChecked;

    [ObservableProperty]
    private bool _isVulkanChecked;

    // API highlight (bold) state
    [ObservableProperty]
    private bool _isDxgiHighlighted;

    [ObservableProperty]
    private bool _isD3d9Highlighted;

    [ObservableProperty]
    private bool _isOpenGlHighlighted;

    [ObservableProperty]
    private bool _isVulkanHighlighted;

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

        var api = GetCheckedApi();
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
            SetCheckedApi(GraphicsApi.Vulkan);
            HighlightApi(GraphicsApi.Vulkan);
        }
        else if (SelectedExecutableContext.IsDXGI)
        {
            SetCheckedApi(GraphicsApi.DXGI);
            HighlightApi(GraphicsApi.DXGI);
        }
        else if (SelectedExecutableContext.IsD3D9)
        {
            SetCheckedApi(GraphicsApi.D3D9);
            HighlightApi(GraphicsApi.D3D9);
        }
        else if (SelectedExecutableContext.IsOpenGL)
        {
            SetCheckedApi(GraphicsApi.OpenGL);
            HighlightApi(GraphicsApi.OpenGL);
        }
        else
        {
            // Clear highlight
            HighlightApi(null);
        }

        Addons.Refresh(Addons.IsAddonSupportChecked, SelectedExecutableContext);
        UpdateUndeployVisibility();
    }

    private void SetCheckedApi(GraphicsApi api)
    {
        IsVulkanChecked = api == GraphicsApi.Vulkan;
        IsDxgiChecked   = api == GraphicsApi.DXGI;
        IsD3d9Checked   = api == GraphicsApi.D3D9;
        IsOpenGlChecked = api == GraphicsApi.OpenGL;
    }

    private void HighlightApi(GraphicsApi? api)
    {
        IsVulkanHighlighted = api == GraphicsApi.Vulkan;
        IsDxgiHighlighted   = api == GraphicsApi.DXGI;
        IsD3d9Highlighted   = api == GraphicsApi.D3D9;
        IsOpenGlHighlighted = api == GraphicsApi.OpenGL;
    }

    private GraphicsApi GetCheckedApi()
    {
        if (IsDxgiChecked)   return GraphicsApi.DXGI;
        if (IsD3d9Checked)   return GraphicsApi.D3D9;
        if (IsOpenGlChecked) return GraphicsApi.OpenGL;
        if (IsVulkanChecked) return GraphicsApi.Vulkan;

        throw new System.Exception("No API selected!");
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
