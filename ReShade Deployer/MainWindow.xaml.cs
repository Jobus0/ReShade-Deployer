using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Wpf.Ui.Common;
using Button = System.Windows.Controls.Button;
using MenuItem = System.Windows.Controls.MenuItem;

namespace ReShadeDeployer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly DeploymentOrchestrator _deploymentOrchestrator;
        private readonly VulkanSystemWideDeployer _vulkanSystemWideDeployer; // for vulkan uninstallation only
        private readonly AppUpdater _appUpdater;
        private readonly ReShadeUpdater _reShadeUpdater;
        private readonly IConfig _config;
        private readonly IMessageBox _messageBox;

        private ExecutableContext? selectedExecutableContext;

        private readonly string? assemblyVersion;
        
        public MainWindow(DeploymentOrchestrator deploymentOrchestrator, VulkanSystemWideDeployer vulkanSystemWideDeployer, AppUpdater appUpdater, ReShadeUpdater reShadeUpdater, IConfig config, IMessageBox messageBox)
        {
            _deploymentOrchestrator = deploymentOrchestrator;
            _vulkanSystemWideDeployer = vulkanSystemWideDeployer;
            _appUpdater = appUpdater;
            _reShadeUpdater = reShadeUpdater;
            _config = config;
            _messageBox = messageBox;

            InitializeComponent();

            assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var startupArgs = ((App)Application.Current).StartupArgs;
            if (startupArgs.Length > 0 && !string.IsNullOrEmpty(startupArgs[0]))
                TargetExecutable(startupArgs[0]);
            
            if (_reShadeUpdater.TryGetLocalReShadeVersionNumber(out string version))
            {
                VersionLabel.Content = version;
                CheckForNewReShadeVersion(version);
            }
            else
            {
                FirstTimeSetup();
            }
            
            _appUpdater.CleanUpOldVersion();
            
            if (assemblyVersion != null)
                CheckForNewDeployerVersion();

            Dispatcher.UnhandledException += DispatcherOnUnhandledException;

#if DEBUG
            ExecutableInfoButton.Visibility = Visibility.Visible;
#endif
        }
        
        private void DispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Hide();
            _messageBox.Show(e.Exception, UIStrings.CriticalError_Title, UIStrings.Exit, ControlAppearance.Danger);

            if (!System.Diagnostics.Debugger.IsAttached)
            {
                e.Handled = true;
                Application.Current.Shutdown();
            }
        }
        
        private void SelectGameButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (TrySelectExecutable(out string executablePath))
                TargetExecutable(executablePath);
        }
        
        /// <summary>
        /// Opens a file dialog for selecting an executable.
        /// </summary>
        /// <param name="executablePath">Path of the selected game executable.</param>
        /// <returns>True if a file was selected, false if cancelled.</returns>
        public bool TrySelectExecutable(out string executablePath)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = UIStrings.OpenFileDialog_Title;
            openFileDialog.Filter = "Select EXE|*.exe";

            bool result = openFileDialog.ShowDialog() ?? false;

            executablePath = openFileDialog.FileName;
            return result;
        }
        
        private async void CheckForNewDeployerVersion()
        {
            if (assemblyVersion == null)
                return;
            
            string latestVersion = _config.LatestDeployerVersionNumber;
            bool newerVersionFound = false;
            if (DateTime.Now - _config.LatestDeployerVersionNumberCheckDate > TimeSpan.FromDays(7))
            {
                try
                {
                    latestVersion = await _appUpdater.GetLatestOnlineVersionNumber();

                    if (_config.LatestDeployerVersionNumber != latestVersion)
                    {
                        _config.LatestDeployerVersionNumber = latestVersion;
                        newerVersionFound = true;
                    }
                    _config.LatestDeployerVersionNumberCheckDate = DateTime.Now;
                }
                catch (Exception)
                {
                    return;
                }
            }

            if (assemblyVersion != latestVersion && newerVersionFound)
                PromptForUpdate();
        }
        
        public void PromptForUpdate()
        {
            var result = _messageBox.Show(
                string.Format(UIStrings.UpdateAvailable, _config.LatestDeployerVersionNumber, assemblyVersion!),
                UIStrings.UpdateAvailable_Title,
                UIStrings.Update,
                UIStrings.Skip);

            if (result == IMessageBox.Result.Primary)
            {
                Panel.IsEnabled = false;
                UpdateButton.IsEnabled = false;
                _appUpdater.Update();
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
                UpdateButton.Content = latestVersion;
            }
        }

        private void DeployButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (selectedExecutableContext == null)
                return;
            
            Hide();

            var api = GetCheckedApi();
            var addonSupport = AddonSupportCheckBox.IsChecked == true;

            try
            {
                _deploymentOrchestrator.DeployReShadeForExecutable(selectedExecutableContext, api, addonSupport);
            }
            catch (DeploymentException ex)
            {
                _messageBox.Show(ex, UIStrings.DeployError_Title, UIStrings.OK);
                Show();
                return;
            }
            
            if (_config.AlwaysExitOnDeploy)
            {
                Application.Current.Shutdown();
            }
            else
            {
                var result = _messageBox.Show(
                    UIStrings.DeploySuccess,
                    UIStrings.DeploySuccess_Title,
                    UIStrings.Continue,
                    UIStrings.Exit);
            
                if (result == IMessageBox.Result.Secondary)
                    Application.Current.Shutdown();
                else
                    Show();
            }
        }

        private void UpdateButtonOnClick(object sender, RoutedEventArgs e)
        {
            DownloadReShade();
        }
        
        /// <summary>
        /// Creates a message box explaining that the required folder structure will be
        /// created in the current folder, and if accepted, downloads the latest ReShade.
        /// </summary>
        private void FirstTimeSetup()
        {
            // Disable buttons until ReShade is downloaded
            DeployButton.IsEnabled = false;
            
            var result = _messageBox.Show(
                UIStrings.FirstTimeSetup,
                UIStrings.FirstTimeSetup_Title,
                UIStrings.Continue,
                UIStrings.Exit);
            
            if (result == IMessageBox.Result.Primary)
                DownloadReShade();
            else
                Application.Current.Shutdown();
        }

        private async void DownloadReShade()
        {
            // Disable buttons until ReShade is downloaded
            DeployButton.IsEnabled = false;
            UpdateButton.IsEnabled = false;

            await _reShadeUpdater.Update();
            
            if (_reShadeUpdater.TryGetLocalReShadeVersionNumber(out string version))
            {
                VersionLabel.Content = version;

                _config.LatestReShadeVersionNumber = version;
                _config.LatestReShadeVersionNumberCheckDate = DateTime.Now;
                
                // Remove version label from the update button, keeping only the icon
                UpdateButton.Content = string.Empty;
                
                if (selectedExecutableContext != null)
                    DeployButton.IsEnabled = true;
            }
            
            UpdateButton.IsEnabled = true;
        }

        private void SetCheckedApi(GraphicsApi api)
        {
            VulkanRadioButton.IsChecked = api == GraphicsApi.Vulkan;
            DxgiRadioButton.IsChecked   = api == GraphicsApi.DXGI;
            D3d9RadioButton.IsChecked   = api == GraphicsApi.D3D9;
            OpenglRadioButton.IsChecked = api == GraphicsApi.OpenGL;
        }
        
        private void HighlightApi(GraphicsApi? api)
        {
            VulkanRadioButton.FontWeight = api == GraphicsApi.Vulkan ? FontWeights.Bold : FontWeights.Normal;
            DxgiRadioButton.FontWeight   = api == GraphicsApi.DXGI   ? FontWeights.Bold : FontWeights.Normal;
            D3d9RadioButton.FontWeight   = api == GraphicsApi.D3D9   ? FontWeights.Bold : FontWeights.Normal;
            OpenglRadioButton.FontWeight = api == GraphicsApi.OpenGL ? FontWeights.Bold : FontWeights.Normal;
        }
        
        private GraphicsApi GetCheckedApi()
        {
            if (DxgiRadioButton.IsChecked == true)
                return GraphicsApi.DXGI;

            if (D3d9RadioButton.IsChecked == true)
                return GraphicsApi.D3D9;

            if (OpenglRadioButton.IsChecked == true)
                return GraphicsApi.OpenGL;

            if (VulkanRadioButton.IsChecked == true)
                return GraphicsApi.Vulkan;
            
            throw new Exception("No API selected!");
        }

        private void RightClickDeployMenuItem_OnLoaded(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            item.IsChecked = RegistryHelper.IsContextMenuActionRegistered("Deploy ReShade");
        }
        
        private void RightClickDeployMenuItem_OnChecked(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            bool isChecked = item.IsChecked;
            
            if (isChecked)
                RegistryHelper.RegisterContextMenuAction("Deploy ReShade", Environment.ProcessPath! + " \"%1\"", Environment.ProcessPath! + ",0");
            else
                RegistryHelper.UnregisterContextMenuAction("Deploy ReShade");
        }
        
        private void AlwaysExitOnDeployMenuItem_OnLoaded(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            item.IsChecked = _config.AlwaysExitOnDeploy;
        }
        
        private void AlwaysExitOnDeployMenuItem_OnChecked(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            _config.AlwaysExitOnDeploy = item.IsChecked;
        }
        
        private void AboutMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            _messageBox.Show(string.Format(UIStrings.About_Info, assemblyVersion), UIStrings.About);
        }
        
        private void UpdateDeployerMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            PromptForUpdate();
        }

        private void UninstallMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var result = _messageBox.Show(
                UIStrings.UninstallDeployer_Content,
                UIStrings.UninstallDeployer,
                UIStrings.UninstallDeployer,
                UIStrings.Cancel,
                ControlAppearance.Caution);

            if (result == IMessageBox.Result.Primary)
            {
                RegistryHelper.UnregisterContextMenuAction("Deploy ReShade");
                _vulkanSystemWideDeployer.RemoveVulkanGlobally();
            }
        }

        private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            if (assemblyVersion != null)
            {
                foreach (var item in button.ContextMenu!.Items)
                {
                    if (item is MenuItem {Name: "UpdateDeployerMenuItem"} updateDeployerMenuItem)
                        updateDeployerMenuItem.Visibility = assemblyVersion == _config.LatestDeployerVersionNumber ? Visibility.Collapsed : Visibility.Visible;
                }
            }
                    
            button.ContextMenu!.IsOpen = true;
        }

        private void TargetExecutable(string executablePath)
        {
            if (!File.Exists(executablePath))
                return;
            
            selectedExecutableContext = new ExecutableContext(executablePath);
            
            SelectGameButtonText.Text = executablePath;
            SelectGameButtonText.ToolTip = executablePath;
            SelectGameButton.Appearance = ControlAppearance.Secondary;
            
            DeployButton.Content = string.Format(UIStrings.DeployButton_Targeted, selectedExecutableContext.FileName);
            DeployButton.IsEnabled = true;

            ExecutableInfoButton.IsEnabled = true;

            // A game might include multiple APIs, so prioritize: Vulkan > DXGI > D3D9 > OpenGL
            if (selectedExecutableContext.IsVulkan)
            {
                SetCheckedApi(GraphicsApi.Vulkan);
                HighlightApi(GraphicsApi.Vulkan);
            }
            else if (selectedExecutableContext.IsDXGI)
            {
                SetCheckedApi(GraphicsApi.DXGI);
                HighlightApi(GraphicsApi.DXGI);
            }
            else if (selectedExecutableContext.IsD3D9)
            {
                SetCheckedApi(GraphicsApi.D3D9);
                HighlightApi(GraphicsApi.D3D9);
            }
            else if (selectedExecutableContext.IsOpenGL)
            {
                SetCheckedApi(GraphicsApi.OpenGL);
                HighlightApi(GraphicsApi.OpenGL);
            }
            else
            {
                // Clear highlight
                HighlightApi(null);
            }
        }

        private void ExecutableInfoButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (selectedExecutableContext != null)
                _messageBox.Show(selectedExecutableContext.ToString(), "Executable Info");
        }
    }
}