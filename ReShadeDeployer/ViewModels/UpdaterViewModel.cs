using System;
using System.Linq;
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
        else if (_reShadeUpdater.GetLocalReShadeDlls().All(dll => dll.Status == ReShadeDllStatus.Missing))
        {
            RequestFirstTimeSetup?.Invoke();
        }
        else
        {
            // ReShade has been downloaded before, but one or more DLLs have been deleted, overwritten or mismatched.
            _ = ValidateReShadeInstallation();
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
            UIStrings.Download, UIStrings.Cancel,
            inputValidationConditional: VersionParser.IsValidSemanticVersion);

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

    /// <summary>
    /// Validate that all four local ReShade DLLs exist, are genuine ReShade files and share the same version.
    /// If not, show a message box listing the correct and flawed DLLs, and offer to re-download ReShade
    /// (using the highest local version found, or the latest version if no valid DLL remains).
    /// </summary>
    /// <returns>True if the installation is valid, or was successfully repaired. False otherwise.</returns>
    public async Task<bool> ValidateReShadeInstallation()
    {
        var dlls = _reShadeUpdater.GetLocalReShadeDlls();
        if (dlls.All(dll => dll.Status == ReShadeDllStatus.Valid))
            return true;

        // All DLLs with status Valid share the highest local version.
        string? highestVersion = dlls.FirstOrDefault(dll => dll.Status == ReShadeDllStatus.Valid)?.Version;
        if (highestVersion != null)
            VersionLabelContent = highestVersion;

        string dllList = string.Join(Environment.NewLine, dlls.Select(dll => dll.Status switch
        {
            ReShadeDllStatus.Valid => $"✓ {dll.Name}: {dll.Version}",
            ReShadeDllStatus.Missing => $"✗ {dll.Name}: {UIStrings.ReShadeValidation_Missing}",
            ReShadeDllStatus.InvalidFile => $"✗ {dll.Name}: {UIStrings.ReShadeValidation_InvalidFile}",
            ReShadeDllStatus.VersionMismatch => $"✗ {dll.Name}: {string.Format(UIStrings.ReShadeValidation_VersionMismatch, dll.Version)}",
            _ => throw new ArgumentOutOfRangeException(nameof(dll.Status), dll.Status, null)
        }));

        string question = highestVersion != null
            ? string.Format(UIStrings.ReShadeValidation_Redownload, highestVersion)
            : UIStrings.ReShadeValidation_RedownloadLatest;

        var result = await _messageBox.Show(UIStrings.ReShadeValidation_Title,
            string.Format(UIStrings.ReShadeValidation_Content, dllList, question),
            primaryButtonText: UIStrings.ReShadeValidation_RedownloadButton,
            closeButtonText: UIStrings.Cancel);

        if (result != MessageBoxResult.Primary)
            return false;

        await DownloadReShade(highestVersion);
        return _reShadeUpdater.TryGetLocalReShadeVersionNumber(out _);
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

        bool success = await _reShadeUpdater.Update(specificVersion);

        if (success && _reShadeUpdater.TryGetLocalReShadeVersionNumber(out string version))
        {
            VersionLabelContent = version;

            var versionComparison = VersionParser.Compare(version, _config.LatestReShadeVersionNumber);
            if (versionComparison is VersionParser.VersionComparisonResult.Newer or VersionParser.VersionComparisonResult.Equal)
            {
                _config.LatestReShadeVersionNumber = version;
                _config.LatestReShadeVersionNumberCheckDate = DateTime.Now;
                
                UpdateButtonContent = string.Empty;
                UpdateButtonVisibility = Visibility.Collapsed;
            }
            else
            {
                UpdateButtonContent = _config.LatestReShadeVersionNumber;
                UpdateButtonVisibility = Visibility.Visible;
            }

            ReShadeDownloadCompleted?.Invoke();
            _snackbarService.Show(string.Format(UIStrings.ReShadeUpdate_Success, version), ControlAppearance.Info, SymbolRegular.ArrowDownload20);
        }

        IsUpdateEnabled = true;
    }
}