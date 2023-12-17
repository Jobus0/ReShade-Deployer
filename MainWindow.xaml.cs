using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ReShadeInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private bool closing;
        
        public MainWindow()
        {
            InitializeComponent();
            
            UpdateButton.Click += UpdateButtonOnClick;
            SelectGameButton.Click += SelectGameButtonOnClick;
            
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Downloader.TryGetInstalledReShadeVersion(out string? version))
                VersionLabel.Content = version;
            else
                FirstTimeSetup();
        }

        private void SelectGameButtonOnClick(object sender, RoutedEventArgs e)
        {
            Hide();
            Deployer.SelectExecutableAndInstallReShade(GetCheckedApi(), AddonSupportCheckBox.IsChecked == true);
            
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
                ReShade Installer will now download ReShade and set up the required folder structure into the current folder.

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
            SelectGameButton.IsEnabled = true;
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
    }
}