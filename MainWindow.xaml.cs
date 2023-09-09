using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace ReShadeInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            if (Deployer.TryGetReShadeVersion(out string? version))
                VersionLabel.Content = version;
            else
                FirstTimeSetup();
            
            UpdateButton.Click += UpdateButtonOnClick;
            SelectGameButton.Click += SelectGameButtonOnClick;
        }

        private void SelectGameButtonOnClick(object sender, RoutedEventArgs e)
        {
            Deployer.InstallReShadeForExecutable(GetCheckedApi(), AddonSupportCheckBox.IsChecked == true);
        }

        private void UpdateButtonOnClick(object sender, RoutedEventArgs e)
        {
            DownloadReShade();
        }

        private void FirstTimeSetup()
        {
            string message = """
                ReShade Installer will now download ReShade and set up the required folder structure into the current folder.

                This includes the Shaders and Textures folders, where you will place your shader and texture files.

                If you wish to keep these files elsewhere, press 'Cancel' and move the program to another folder first.
                """;
            
            if (MessageBox.Show(message, "First-Time Setup", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                DownloadReShade();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private async void DownloadReShade()
        {
            IsEnabled = false;
            await Downloader.DownloadReShade();
            IsEnabled = true;
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