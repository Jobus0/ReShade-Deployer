using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Common;

namespace ReShadeDeployer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DeploymentOrchestrator _deploymentOrchestrator;
    private readonly ReShadeUndeployer _reShadeUndeployer;
    private readonly VulkanSystemWideDeployer _vulkanSystemWideDeployer;
    private readonly AppUpdater _appUpdater;
    private readonly ReShadeUpdater _reShadeUpdater;
    private readonly IConfig _config;
    private readonly IMessageBox _messageBox;

    public string? AssemblyVersion { get; }

    public IList<AddonItem> Addons { get; }

    // ── Observable properties ──────────────────────────────────────────

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
    private bool _isUpdateEnabled = true;

    [ObservableProperty]
    private bool _isPanelEnabled = true;

    [ObservableProperty]
    private string _versionLabelContent = string.Empty;

    [ObservableProperty]
    private string? _updateButtonContent;

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

    // Addon support checkbox
    [ObservableProperty]
    private bool _isAddonSupportChecked;

    // Addons combo visibility
    [ObservableProperty]
    private bool _isAddonsComboVisible;

    // Undeploy button visibility
    [ObservableProperty]
    private bool _isUndeployVisible;

    // Addons summary text
    [ObservableProperty]
    private string _addonsSummaryText = UIStrings.AddonSummaryPlaceholder;

    // Settings menu items
    [ObservableProperty]
    private bool _isContextMenuRegistered;

    [ObservableProperty]
    private bool _isAlwaysExitOnDeploy;

    // ── Constructor ────────────────────────────────────────────────────

    public MainViewModel(
        DeploymentOrchestrator deploymentOrchestrator,
        AddonsDeployer addonsDeployer,
        ReShadeUndeployer reShadeUndeployer,
        VulkanSystemWideDeployer vulkanSystemWideDeployer,
        AppUpdater appUpdater,
        ReShadeUpdater reShadeUpdater,
        IConfig config,
        IMessageBox messageBox)
    {
        _deploymentOrchestrator = deploymentOrchestrator;
        _reShadeUndeployer = reShadeUndeployer;
        _vulkanSystemWideDeployer = vulkanSystemWideDeployer;
        _appUpdater = appUpdater;
        _reShadeUpdater = reShadeUpdater;
        _config = config;
        _messageBox = messageBox;

        AssemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3);

        Addons = addonsDeployer.GetAvailableAddons();

        var lastSelectedAddons = _config.LastSelectedAddons;
        foreach (var addonItem in Addons)
            if (lastSelectedAddons.Contains(addonItem.Name))
                addonItem.IsSelected = true;

        IsAlwaysExitOnDeploy = _config.AlwaysExitOnDeploy;
        IsContextMenuRegistered = RegistryHelper.IsContextMenuActionRegistered("Deploy ReShade");

        UpdateAddonsSummaryText();
    }

    // ── Initialization (called from code-behind OnLoaded) ──────────────

    public void Initialize(string[] startupArgs)
    {
        if (startupArgs.Length > 0 && !string.IsNullOrEmpty(startupArgs[0]))
            TargetExecutable(startupArgs[0]);

        IsAddonSupportChecked = startupArgs.Contains("-addon-support");

        if (_reShadeUpdater.TryGetLocalReShadeVersionNumber(out string version))
        {
            VersionLabelContent = version;
            CheckForNewReShadeVersion(version);
        }
        else
        {
            RequestFirstTimeSetup?.Invoke();
        }

        _appUpdater.CleanUpOldVersion();

        if (AssemblyVersion != null)
            CheckForNewDeployerVersion();

        UpdateAddonsComboVisibility();
        UpdateAddonsSummaryText();
    }

    // ── Events to the View ─────────────────────────────────────────────

    /// <summary>Raised when the window should hide itself (e.g. during deploy).</summary>
    public event Action? RequestHide;
    
    /// <summary>Raised when the window should show itself again.</summary>
    public event Action? RequestShow;

    /// <summary>Raised when first-time setup is needed (no local ReShade found).</summary>
    public event Action? RequestFirstTimeSetup;

    /// <summary>Raised when a deployer update should be prompted.</summary>
    public event Action? RequestPromptForUpdate;

    // ── Commands ───────────────────────────────────────────────────────

    [RelayCommand]
    private void SelectGame(string? executablePath)
    {
        if (!string.IsNullOrEmpty(executablePath))
            TargetExecutable(executablePath);
    }

    [RelayCommand(CanExecute = nameof(CanDeploy))]
    private void Deploy()
    {
        if (SelectedExecutableContext == null)
            return;

        RequestHide?.Invoke();

        var api = GetCheckedApi();
        var addonSupport = IsAddonSupportChecked;

        try
        {
            _deploymentOrchestrator.DeployReShadeForExecutable(
                SelectedExecutableContext, api, addonSupport, GetSelectedAddons());
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

    [RelayCommand]
    private void UpdateReShade()
    {
        DownloadReShade();
    }

    [RelayCommand]
    private void CheckForDeployerUpdate()
    {
        CheckForNewDeployerVersion(true);
    }

    [RelayCommand(CanExecute = nameof(CanShowExecutableInfo))]
    private void ShowExecutableInfo()
    {
        if (SelectedExecutableContext != null)
            _messageBox.Show(SelectedExecutableContext.ToString(), "Executable Info");
    }

    private bool CanShowExecutableInfo() => SelectedExecutableContext != null;

    [RelayCommand]
    private void ShowAbout()
    {
        _messageBox.Show(string.Format(UIStrings.About_Info, AssemblyVersion), UIStrings.About);
    }

    [RelayCommand]
    private void Uninstall()
    {
        var result = _messageBox.Show(
            UIStrings.UninstallDeployer_Content,
            UIStrings.UninstallDeployer,
            UIStrings.UninstallDeployer,
            UIStrings.Cancel,
            ControlAppearance.Caution);

        if (result == IMessageBox.Result.Primary)
        {
            if (RegistryHelper.IsContextMenuActionRegistered("Deploy ReShade"))
                RegistryHelper.UnregisterContextMenuAction("Deploy ReShade");

            _vulkanSystemWideDeployer.RemoveVulkanGlobally();

            _messageBox.Show(UIStrings.UninstallDeployer_Success, UIStrings.Success);
        }
    }

    [RelayCommand]
    private void ToggleAddonSupport()
    {
        UpdateAddonsComboVisibility();
        UpdateAddonsSummaryText();
    }

    // ── Public methods (called from code-behind) ───────────────────────

    /// <summary>
    /// Called by the code-behind when a user clicks an addon item in the ComboBox.
    /// The code-behind handles the raw mouse event and hit-testing; this method
    /// handles state updates.
    /// </summary>
    public void OnAddonItemClicked(AddonItem item)
    {
        item.IsSelected = !item.IsSelected;
        UpdateAddonsSummaryText();
        _config.LastSelectedAddons = Addons.Where(a => a.IsSelected).Select(a => a.Name).ToArray();
    }

    public void PromptForUpdate()
    {
        var result = _messageBox.Show(
            string.Format(UIStrings.UpdateAvailable, _config.LatestDeployerVersionNumber, AssemblyVersion!),
            UIStrings.UpdateAvailable_Title,
            UIStrings.Update,
            UIStrings.Skip);

        if (result == IMessageBox.Result.Primary)
        {
            IsPanelEnabled = false;
            IsUpdateEnabled = false;
            _appUpdater.Update();
        }
    }

    /// <summary>
    /// Called from the code-behind when the first-time setup dialog is accepted.
    /// </summary>
    public void AcceptFirstTimeSetup()
    {
        DownloadReShade();
    }

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

        UpdateAddonsComboVisibility();
        UpdateAddonsSummaryText();
        UpdateUndeployVisibility();
    }

    // ── Property change callbacks ──────────────────────────────────────

    partial void OnIsContextMenuRegisteredChanged(bool value)
    {
        if (value)
            RegistryHelper.RegisterContextMenuAction("Deploy ReShade",
                Environment.ProcessPath! + " \"%1\"",
                Environment.ProcessPath! + ",0");
        else
            RegistryHelper.UnregisterContextMenuAction("Deploy ReShade");
    }

    partial void OnIsAlwaysExitOnDeployChanged(bool value)
    {
        _config.AlwaysExitOnDeploy = value;
    }

    // ── Private helpers ────────────────────────────────────────────────

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

        throw new Exception("No API selected!");
    }

    private IList<AddonItem> GetSelectedAddons()
    {
        return Addons.Where(addon => addon.IsSelected && addon.IsSupported).ToArray();
    }

    private void UpdateAddonsComboVisibility()
    {
        foreach (AddonItem addonItem in Addons)
        {
            bool hasMatchingX64 = IsAddonSupportChecked && SelectedExecutableContext is { IsX64: true } && addonItem.HasX64Addon;
            bool hasMatchingX32 = IsAddonSupportChecked && SelectedExecutableContext is { IsX64: false } && addonItem.HasX32Addon;
            addonItem.IsSupported = hasMatchingX64 || hasMatchingX32 || !addonItem.HasAnyAddon;
        }

        var hasSupportedAddons = Addons.Any(a => a.IsSupported);
        var hasNonAddonAddons = Addons.Any(a => !a.HasAnyAddon);
        IsAddonsComboVisible = (IsAddonSupportChecked || hasNonAddonAddons) && hasSupportedAddons;
    }

    private void UpdateAddonsSummaryText()
    {
        var selected = GetSelectedAddons();
        AddonsSummaryText = selected.Count == 0
            ? UIStrings.AddonSummaryPlaceholder
            : string.Join(", ", selected.Select(addon => addon.Name));
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

    private async void CheckForNewDeployerVersion(bool force = false)
    {
        if (AssemblyVersion == null)
            return;

        string latestVersion = _config.LatestDeployerVersionNumber;
        bool newerVersionFound = false;
        if (force || DateTime.Now - _config.LatestDeployerVersionNumberCheckDate > TimeSpan.FromDays(7))
        {
            _config.LatestDeployerVersionNumberCheckDate = DateTime.Now;

            try
            {
                latestVersion = await _appUpdater.GetLatestOnlineVersionNumber();

                if (VersionParser.IsNewerThan(latestVersion, _config.LatestDeployerVersionNumber))
                    _config.LatestDeployerVersionNumber = latestVersion;

                if (VersionParser.IsNewerThan(latestVersion, AssemblyVersion))
                    newerVersionFound = true;
            }
            catch (Exception)
            {
                return;
            }
        }

        if (newerVersionFound)
        {
            RequestPromptForUpdate?.Invoke();
        }
        else if (force)
        {
            _messageBox.Show(
                string.Format(UIStrings.UpdateUnavailable, latestVersion, AssemblyVersion!),
                UIStrings.UpdateUnavailable_Title);
        }
    }

    private async void CheckForNewReShadeVersion(string localVersion)
    {
        string latestVersion = _config.LatestReShadeVersionNumber;
        if (DateTime.Now - _config.LatestReShadeVersionNumberCheckDate > TimeSpan.FromDays(7))
        {
            try
            {
                latestVersion = await _reShadeUpdater.GetLatestOnlineReShadeVersionNumber();

                _config.LatestReShadeVersionNumber = latestVersion;
                _config.LatestReShadeVersionNumberCheckDate = DateTime.Now;
            }
            catch (Exception)
            {
                return;
            }
        }

        if (localVersion != latestVersion)
        {
            UpdateButtonContent = latestVersion;
        }
    }

    private async void DownloadReShade()
    {
        // Disable buttons until ReShade is downloaded
        IsDeployEnabled = false;
        IsUpdateEnabled = false;

        await _reShadeUpdater.Update();

        if (_reShadeUpdater.TryGetLocalReShadeVersionNumber(out string version))
        {
            VersionLabelContent = version;

            _config.LatestReShadeVersionNumber = version;
            _config.LatestReShadeVersionNumberCheckDate = DateTime.Now;

            // Remove version label from the update button, keeping only the icon
            UpdateButtonContent = string.Empty;

            if (SelectedExecutableContext != null)
                IsDeployEnabled = true;
        }

        IsUpdateEnabled = true;
    }
}
