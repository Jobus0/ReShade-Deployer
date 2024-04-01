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

        /// <summary>
        /// Executable path from the launch arguments. Used when deployed from the context menu.
        /// </summary>
        public string? TargetExecutablePath
        {
            get => ((App)Application.Current).TargetExecutablePath;
            set => ((App)Application.Current).TargetExecutablePath = value;
        }

        private readonly Config config;
        
        public MainWindow()
        {
            config = new Config();
            
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TargetExecutablePath))
            {
                TargetedContentAutoSelectAPI();
                TargetedContentSelectGameButton();
            }
            
            if (Downloader.TryGetLocalReShadeVersionNumber(out string version))
            {
                VersionLabel.Content = version;
                CheckForNewVersion(version);
            }
            else
            {
                FirstTimeSetup();
            }
        }

        private async void CheckForNewVersion(string localVersion)
        {
            string latestVersion = config.LatestVersionNumber;
            if (DateTime.Now - config.LatestVersionNumberCheckDate > TimeSpan.FromDays(7))
            {
                try
                {
                    latestVersion = await Downloader.GetLatestOnlineReShadeVersionNumber();
                    
                    config.LatestVersionNumber = latestVersion;
                    config.LatestVersionNumberCheckDate = DateTime.Now;
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
            
            if (string.IsNullOrEmpty(TargetExecutablePath))
                Deployer.SelectExecutableAndDeployReShade(GetCheckedApi(), AddonSupportCheckBox.IsChecked == true, config.AlwaysExitOnDeploy);
            else
                Deployer.DeployReShadeForExecutable(GetCheckedApi(), AddonSupportCheckBox.IsChecked == true, TargetExecutablePath, config.AlwaysExitOnDeploy);
            
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

            await Downloader.DownloadReShade();
            
            if (Downloader.TryGetLocalReShadeVersionNumber(out string version))
            {
                VersionLabel.Content = version;

                config.LatestVersionNumber = version;
                config.LatestVersionNumberCheckDate = DateTime.Now;
                
                // Remove version label from the update button, keeping only the icon
                UpdateButton.Content = string.Empty;
                
                if (!string.IsNullOrEmpty(TargetExecutablePath))
                    DeployButton.IsEnabled = true;
            }
            
            UpdateButton.IsEnabled = true;
        }

        private void SetCheckedApi(string api)
        {
            VulkanRadioButton.IsChecked = api == "vulkan";
            DxgiRadioButton.IsChecked = api == "dxgi";
            D3d9RadioButton.IsChecked = api == "d3d9";
            OpenglRadioButton.IsChecked = api == "opengl32";
        }
        
        private void HighlightApi(string api)
        {
            VulkanRadioButton.FontWeight = api == "vulkan" ? FontWeights.Bold : FontWeights.Normal;
            DxgiRadioButton.FontWeight = api == "dxgi" ? FontWeights.Bold : FontWeights.Normal;
            D3d9RadioButton.FontWeight = api == "d3d9" ? FontWeights.Bold : FontWeights.Normal;
            OpenglRadioButton.FontWeight = api == "opengl32" ? FontWeights.Bold : FontWeights.Normal;
        }
        
        private string GetCheckedApi()
        {
            if (DxgiRadioButton.IsChecked == true)
                return "dxgi";

            if (D3d9RadioButton.IsChecked == true)
                return "d3d9";

            if (OpenglRadioButton.IsChecked == true)
                return "opengl32";

            if (VulkanRadioButton.IsChecked == true)
                return "vulkan";
            
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
            WpfMessageBox.Show(string.Format(UIStrings.About_Info, Assembly.GetExecutingAssembly().GetName().Version), UIStrings.About);
        }

        private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            button.ContextMenu!.IsOpen = true;
        }

        private void TargetedContentSelectGameButton()
        {
            SelectGameButton.Content = TargetExecutablePath;
            SelectGameButton.Appearance = ControlAppearance.Secondary;
            
            DeployButton.Content = string.Format(UIStrings.DeployButton_Targeted, Path.GetFileNameWithoutExtension(TargetExecutablePath));
            DeployButton.IsEnabled = true;
        }

        private void TargetedContentAutoSelectAPI()
        {
            var parser = new ExecutableParser(TargetExecutablePath!);

            bool isD3D9 = parser.ModulesContain("d3d9");
            bool isDXGI = parser.ModulesContain("dxgi", "d3d1", "GFSDK"); // Assume DXGI when GameWorks SDK is in use
            bool isOpenGL = parser.ModulesContain("opengl32");
            bool isVulkan = parser.ModulesContain("vulkan-1");
                
            // A game might include multiple APIs, so prioritize: Vulkan > DXGI > D3D9 > OpenGL
            if (isVulkan)
            {
                SetCheckedApi("vulkan");
                HighlightApi("vulkan");
            }
            else if (isDXGI)
            {
                SetCheckedApi("dxgi");
                HighlightApi("dxgi");
            }
            else if (isD3D9)
            {
                SetCheckedApi("d3d9");
                HighlightApi("d3d9");
            }
            else if (isOpenGL)
            {
                SetCheckedApi("opengl32");
                HighlightApi("opengl32");
            }
            else
            {
                HighlightApi(string.Empty);
            }
        }

        private void SelectGameButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (Deployer.TrySelectExecutable(out string executablePath))
            {
                TargetExecutablePath = executablePath;
                TargetedContentAutoSelectAPI();
                TargetedContentSelectGameButton();
            }
        }
    }
}