﻿using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
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
        private static string? TargetExecutablePath => ((App)Application.Current).TargetExecutablePath;

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
                SelectGameButton.Content = string.Format(UIStrings.DeployButton_Targeted, Path.GetFileNameWithoutExtension(TargetExecutablePath));
                SelectGameButton.ToolTip = UIStrings.DeployButton_Targeted_Tooltip;
            }
            
            if (Downloader.TryGetLocalReShadeVersion(out string version))
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
            string latestVersion = config.LatestVersion;
            if (DateTime.Now - config.LatestVersionCheckDate > TimeSpan.FromDays(7))
            {
                try
                {
                    latestVersion = await Downloader.GetLatestOnlineReShadeVersion();
                    
                    config.LatestVersion = latestVersion;
                    config.LatestVersionCheckDate = DateTime.Now;
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
            SelectGameButton.IsEnabled = false;
            UpdateButton.IsEnabled = false;

            await Downloader.DownloadReShade();
            
            if (Downloader.TryGetLocalReShadeVersion(out string version))
            {
                VersionLabel.Content = version;
                SelectGameButton.IsEnabled = true;
                UpdateButton.Content = string.Empty;

                config.LatestVersion = version;
                config.LatestVersionCheckDate = DateTime.Now;
            }
            
            UpdateButton.IsEnabled = true;
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
        
        private void AboutMenuItem_OnClicked(object sender, RoutedEventArgs e)
        {
            WpfMessageBox.Show(string.Format(UIStrings.About_Info, Assembly.GetExecutingAssembly().GetName().Version), UIStrings.About);
        }

        private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            (sender as Button)!.ContextMenu.IsOpen = true;
        }
    }
}