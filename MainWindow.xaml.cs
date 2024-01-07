using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace ReShadeDeployer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private bool closing;
        private static string? TargetExecutablePath => ((App)Application.Current).TargetExecutablePath;
        
        public MainWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TargetExecutablePath))
            {
                SelectGameButton.Content = "Deploy to " + Path.GetFileNameWithoutExtension(TargetExecutablePath);
                SelectGameButton.ToolTip = """
                    Make sure you have picked the correct Target API above first.
                    
                    If you are unsure, check the API section of the game's PCGamingWiki page.
                    """;
            }
                
            if (Downloader.TryGetLocalReShadeVersion(out string? version))
                VersionLabel.Content = version;
            else
                FirstTimeSetup();
        }

        private void SelectGameButtonOnClick(object sender, RoutedEventArgs e)
        {
            Hide();
            
            if (string.IsNullOrEmpty(TargetExecutablePath))
                Deployer.SelectExecutableAndDeployReShade(GetCheckedApi(), AddonSupportCheckBox.IsChecked == true);
            else
                Deployer.DeployReShadeForExecutable(GetCheckedApi(), AddonSupportCheckBox.IsChecked == true, TargetExecutablePath);
            
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

        private void FirstTimeSetup()
        {
            SelectGameButton.IsEnabled = false;
            
            string message = """
                ReShade Deployer will now download ReShade and set up the required folder structure into the current folder.

                This includes the Shaders and Textures folders, where you will place your shader and texture files.

                If you wish to keep these files elsewhere, press 'Exit' and move the program first.
                """;
            
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "First-Time Setup",
                Content = new TextBlock {Text = message, TextWrapping = TextWrapping.Wrap},
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.Height,
                ButtonLeftName = "Continue",
                ButtonRightName = "Exit",
                Width = 360
            };
            messageBox.ButtonLeftClick += (_, _) => { messageBox.Close(); DownloadReShade(); };
            messageBox.ButtonRightClick += (_, _) => Application.Current.Shutdown();
            messageBox.ShowDialog();
        }

        private async void DownloadReShade()
        {
            SelectGameButton.IsEnabled = false;
            UpdateButton.IsEnabled = false;
            await Downloader.DownloadReShade();
            
            if (Downloader.TryGetLocalReShadeVersion(out string? version))
            {
                VersionLabel.Content = version;
                SelectGameButton.IsEnabled = true;
            }
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

        private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            (sender as Button)!.ContextMenu.IsOpen = true;
        }
    }
}