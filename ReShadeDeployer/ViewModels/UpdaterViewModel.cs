using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui;
using Wpf.Ui.Controls;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace ReShadeDeployer.ViewModels;

public partial class UpdaterViewModel : ObservableObject
{
    private readonly AppUpdater _appUpdater;
    private readonly ReShadeUpdater _reShadeUpdater;
    private readonly IConfig _config;
    private readonly IMessageBox _messageBox;
    private readonly ISnackbarService _snackbarService;

    public string? AssemblyVersion { get; }

    [ObservableProperty]
    private bool _isUpdateEnabled = true;

    [ObservableProperty]
    private bool _isPanelEnabled = true;

    [ObservableProperty]
    private string _versionLabelContent = string.Empty;

    [ObservableProperty]
    private Visibility _updateButtonVisibility = Visibility.Collapsed;
    
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
        ISnackbarService snackbarService)
    {
        _appUpdater = appUpdater;
        _reShadeUpdater = reShadeUpdater;
        _config = config;
        _messageBox = messageBox;
        _snackbarService = snackbarService;

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

        _ = _appUpdater.CleanUpOldVersion();

        if (AssemblyVersion != null)
            CheckForNewDeployerVersion();
    }

    [RelayCommand]
    private void UpdateReShade()
    {
        _ = DownloadReShade();
    }

    [RelayCommand]
    private void CheckForDeployerUpdate()
    {
        CheckForNewDeployerVersion(true);
    }

    [RelayCommand]
    private async Task DownloadSpecificVersion()
    {
        var (result, version) = await _messageBox.ShowWithTextBox(UIStrings.DownloadSpecificVersion_Title,
            UIStrings.DownloadSpecificVersion_Content,
            UIStrings.Download, UIStrings.Cancel);

        version = VersionParser.Parse(version);

        if (result == MessageBoxResult.Primary && !string.IsNullOrEmpty(version))
            await DownloadReShade(version);
    }

    public async Task PromptForUpdate()
    {
        var result = await _messageBox.Show(UIStrings.UpdateAvailable_Title,
            string.Format(UIStrings.UpdateAvailable, _config.LatestDeployerVersionNumber, AssemblyVersion!),
            primaryButtonText: UIStrings.Update,
            closeButtonText: UIStrings.Skip);

        if (result == MessageBoxResult.Primary)
        {
            IsPanelEnabled = false;
            IsUpdateEnabled = false;
            _ = _appUpdater.Update();
        }
    }

    /// <summary>
    /// Called from the code-behind when the first-time setup dialog is accepted.
    /// </summary>
    public void AcceptFirstTimeSetup()
    {
        _ = DownloadReShade();
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
            await _messageBox.Show(UIStrings.UpdateUnavailable_Title, string.Format(UIStrings.UpdateUnavailable, latestVersion, AssemblyVersion!));
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
            UpdateButtonVisibility = Visibility.Visible;
        }
        else
        {
            UpdateButtonVisibility = Visibility.Collapsed;
        }
    }

    internal async Task DownloadReShade(string? specificVersion = null)
    {
        IsUpdateEnabled = false;

        await _reShadeUpdater.Update(specificVersion);

        if (_reShadeUpdater.TryGetLocalReShadeVersionNumber(out string version))
        {
            VersionLabelContent = version;

            var versionComparison = VersionParser.Compare(version, _config.LatestReShadeVersionNumber);
            if (versionComparison is VersionParser.VersionComparisonResult.Newer or VersionParser.VersionComparisonResult.Equal)
            {
                _config.LatestReShadeVersionNumber = version;
                _config.LatestReShadeVersionNumberCheckDate = DateTime.Now;
                
                // Remove version label from the update button, keeping only the icon
                UpdateButtonContent = string.Empty;
                UpdateButtonVisibility = Visibility.Collapsed;
            }
            else
            {
                UpdateButtonContent = _config.LatestReShadeVersionNumber;
                UpdateButtonVisibility = Visibility.Visible;
            }

            ReShadeDownloadCompleted?.Invoke();
            _snackbarService.Show(UIStrings.ReShadeUpdate_Success, ControlAppearance.Info, SymbolRegular.ArrowDownload20);
        }

        IsUpdateEnabled = true;
    }
}