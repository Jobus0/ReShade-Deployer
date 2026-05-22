using System;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Common;

namespace ReShadeDeployer.ViewModels;

public partial class UpdaterViewModel : ObservableObject
{
    private readonly AppUpdater _appUpdater;
    private readonly ReShadeUpdater _reShadeUpdater;
    private readonly IConfig _config;
    private readonly IMessageBox _messageBox;
    private readonly RegistryHelper _registryHelper;

    public string? AssemblyVersion { get; }

    [ObservableProperty]
    private bool _isUpdateEnabled = true;

    [ObservableProperty]
    private bool _isPanelEnabled = true;

    [ObservableProperty]
    private string _versionLabelContent = string.Empty;

    [ObservableProperty]
    private string? _updateButtonContent;

    /// <summary>Raised when first-time setup is needed (no local ReShade found).</summary>
    public event Action? RequestFirstTimeSetup;

    /// <summary>Raised when a deployer update should be prompted.</summary>
    public event Action? RequestPromptForUpdate;

    public UpdaterViewModel(
        AppUpdater appUpdater,
        ReShadeUpdater reShadeUpdater,
        IConfig config,
        IMessageBox messageBox,
        RegistryHelper registryHelper)
    {
        _appUpdater = appUpdater;
        _reShadeUpdater = reShadeUpdater;
        _config = config;
        _messageBox = messageBox;
        _registryHelper = registryHelper;

        AssemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3);
    }

    public void Initialize()
    {
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
    }

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
            if (_registryHelper.IsContextMenuActionRegistered("Deploy ReShade"))
                _registryHelper.UnregisterContextMenuAction("Deploy ReShade");

            // VulkanSystemWideDeployer is handled by MainViewModel/DI; accessed via service
            _messageBox.Show(UIStrings.UninstallDeployer_Success, UIStrings.Success);
        }
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

    /// <summary>
    /// Allows MainViewModel to re-enable deploy after a ReShade download
    /// if an executable is already targeted. Subscribe to this event.
    /// </summary>
    public event Action? ReShadeDownloadCompleted;

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

    internal async void DownloadReShade()
    {
        IsUpdateEnabled = false;

        await _reShadeUpdater.Update();

        if (_reShadeUpdater.TryGetLocalReShadeVersionNumber(out string version))
        {
            VersionLabelContent = version;

            _config.LatestReShadeVersionNumber = version;
            _config.LatestReShadeVersionNumberCheckDate = DateTime.Now;

            // Remove version label from the update button, keeping only the icon
            UpdateButtonContent = string.Empty;

            ReShadeDownloadCompleted?.Invoke();
        }

        IsUpdateEnabled = true;
    }
}
