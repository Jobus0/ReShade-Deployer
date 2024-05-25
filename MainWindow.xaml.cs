using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
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
        private bool closing;

        private readonly Config config = new();
        
        private ExecutableContext? selectedExecutableContext;

        private readonly string? assemblyVersion;
        
        public MainWindow()
        {
            InitializeComponent();

            assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var startupArgs = ((App)Application.Current).StartupArgs;
            if (startupArgs.Length > 0 && !string.IsNullOrEmpty(startupArgs[0]))
                TargetExecutable(startupArgs[0]);
            
            DeployerDownloader.CleanUpOldVersion();
            
            if (assemblyVersion != null)
                CheckForNewDeployerVersion();
            
            if (ReShadeDownloader.TryGetLocalReShadeVersionNumber(out string version))
            {
                VersionLabel.Content = version;
                CheckForNewReShadeVersion(version);
            }
            else
            {
                FirstTimeSetup();
            }
            
#if DEBUG
            ExecutableInfoButton.Visibility = Visibility.Visible;
#endif
        }
        
        private void SelectGameButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (Deployer.TrySelectExecutable(out string executablePath))
                TargetExecutable(executablePath);
        }
        
        private async void CheckForNewDeployerVersion()
        {
            if (assemblyVersion == null)
                return;
            
            string latestVersion = config.LatestDeployerVersionNumber;
            bool newerVersionFound = false;
            if (DateTime.Now - config.LatestDeployerVersionNumberCheckDate > TimeSpan.FromDays(7))
            {
                try
                {
                    latestVersion = await DeployerDownloader.GetLatestOnlineVersionNumber();

                    if (config.LatestDeployerVersionNumber != latestVersion)
                    {
                        config.LatestDeployerVersionNumber = latestVersion;
                        newerVersionFound = true;
                    }
                    config.LatestDeployerVersionNumberCheckDate = DateTime.Now;
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
            var textBlock = new TextBlock {Text = string.Format(UIStrings.UpdateAvailable, config.LatestDeployerVersionNumber, assemblyVersion!), TextWrapping = TextWrapping.Wrap};
            WpfMessageBox.ConvertStringLinksToHyperlinks(textBlock);
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = UIStrings.UpdateAvailable_Title,
                Content = textBlock,
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.Height,
                ButtonLeftName = UIStrings.Update,
                ButtonRightName = UIStrings.Skip,
                Width = 390
            };
            messageBox.ButtonLeftClick += (_, _) =>
            {
                messageBox.Close();
                Panel.IsEnabled = false;
                UpdateButton.IsEnabled = false;
                DeployerDownloader.UpdateDeployer();
            };
            messageBox.ButtonRightClick += (_, _) => messageBox.Close();
            messageBox.ShowDialog();
        }

        private async void CheckForNewReShadeVersion(string localVersion)
        {
            string latestVersion = config.LatestReShadeVersionNumber;
            if (DateTime.Now - config.LatestReShadeVersionNumberCheckDate > TimeSpan.FromDays(7))
            {
                try
                {
                    latestVersion = await ReShadeDownloader.GetLatestOnlineReShadeVersionNumber();
                    
                    config.LatestReShadeVersionNumber = latestVersion;
                    config.LatestReShadeVersionNumberCheckDate = DateTime.Now;
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
            Hide();
            
            if (selectedExecutableContext == null)
                Deployer.SelectExecutableAndDeployReShade(GetCheckedApi(), AddonSupportCheckBox.IsChecked == true, config.AlwaysExitOnDeploy);
            else
                Deployer.DeployReShadeForExecutable(selectedExecutableContext, GetCheckedApi(), AddonSupportCheckBox.IsChecked == true, config.AlwaysExitOnDeploy);
            
            if (!closing)
                Show();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            closing = true;
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

            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = UIStrings.FirstTimeSetup_Title,
                Content = new TextBlock {Text = UIStrings.FirstTimeSetup, TextWrapping = TextWrapping.Wrap},
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.Height,
                ButtonLeftName = UIStrings.Continue,
                ButtonRightName = UIStrings.Exit,
                Width = 360
            };
            messageBox.ButtonLeftClick += (_, _) => { messageBox.Close(); DownloadReShade(); };
            messageBox.ButtonRightClick += (_, _) => Application.Current.Shutdown();
            messageBox.ShowDialog();
        }

        private async void DownloadReShade()
        {
            // Disable buttons until ReShade is downloaded
            DeployButton.IsEnabled = false;
            UpdateButton.IsEnabled = false;

            await ReShadeDownloader.DownloadReShade();
            
            if (ReShadeDownloader.TryGetLocalReShadeVersionNumber(out string version))
            {
                VersionLabel.Content = version;

                config.LatestReShadeVersionNumber = version;
                config.LatestReShadeVersionNumberCheckDate = DateTime.Now;
                
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
            item.IsChecked = config.AlwaysExitOnDeploy;
        }
        
        private void AlwaysExitOnDeployMenuItem_OnChecked(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            config.AlwaysExitOnDeploy = item.IsChecked;
        }
        
        private void AboutMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            WpfMessageBox.Show(string.Format(UIStrings.About_Info, assemblyVersion), UIStrings.About);
        }
        
        private void UpdateDeployerMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            PromptForUpdate();
        }

        private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            if (assemblyVersion != null)
            {
                foreach (var item in button.ContextMenu!.Items)
                {
                    if (item is MenuItem {Name: "UpdateDeployerMenuItem"} updateDeployerMenuItem)
                        updateDeployerMenuItem.Visibility = assemblyVersion == config.LatestDeployerVersionNumber ? Visibility.Collapsed : Visibility.Visible;
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
                WpfMessageBox.Show(selectedExecutableContext.ToString(), "Executable Info");
        }
    }
}