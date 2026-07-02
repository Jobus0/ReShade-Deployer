using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using ReShadeDeployer.ViewModels;
using Wpf.Ui.Controls;
using Button = System.Windows.Controls.Button;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace ReShadeDeployer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly MainViewModel _viewModel;
        private readonly IMessageBox _messageBox;

        public MainWindow(MainViewModel viewModel, IMessageBox messageBox)
        {
            _viewModel = viewModel;
            _messageBox = messageBox;

            DataContext = _viewModel;

            InitializeComponent();

            // Wire up View-level events from the ViewModel
            _viewModel.RequestHide += Hide;
            _viewModel.RequestShow += Show;
            _viewModel.Updater.RequestFirstTimeSetup += FirstTimeSetup;
            _viewModel.Updater.RequestPromptForUpdate += () => _viewModel.Updater.PromptForUpdate();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SettingsButton.ContextMenu.DataContext = _viewModel;

            var startupArgs = ((App)Application.Current).StartupArgs;

            _viewModel.Initialize(startupArgs);

            SingleInstanceManager.OnArgumentsReceived += args =>
            {
                if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
                    _viewModel.TargetExecutable(args[0]);

                if (WindowState == WindowState.Minimized)
                    WindowState = WindowState.Normal;

                Activate();
            };

            Dispatcher.UnhandledException += DispatcherOnUnhandledException;

#if DEBUG
            ExecutableInfoButton.Visibility = Visibility.Visible;
#endif
        }

        private async void DispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Hide();
            await _messageBox.ShowException(UIStrings.CriticalError_Title, e.Exception, UIStrings.Exit, ControlAppearance.Danger);

            if (!System.Diagnostics.Debugger.IsAttached)
            {
                e.Handled = true;
                Application.Current.Shutdown();
            }
        }

        private void SelectGameButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (TrySelectExecutable(out string executablePath))
                _viewModel.SelectGameCommand.Execute(executablePath);
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

        /// <summary>
        /// Creates a message box explaining that the required folder structure will be
        /// created in the current folder, and if accepted, downloads the latest ReShade.
        /// </summary>
        private async void FirstTimeSetup()
        {
            // Disable buttons until ReShade is downloaded
            _viewModel.IsDeployEnabled = false;

            var result = await _messageBox.Show(UIStrings.FirstTimeSetup_Title,
                UIStrings.FirstTimeSetup,
                primaryButtonText: UIStrings.Continue,
                closeButtonText: UIStrings.Exit);

            if (result == MessageBoxResult.Primary)
                _viewModel.Updater.AcceptFirstTimeSetup();
            else
                Application.Current.Shutdown();
        }

        private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            button.ContextMenu!.IsOpen = true;
        }

        private void AddonItem_OnUserClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: AddonItem item })
            {
                _viewModel.Addons.OnAddonItemClicked(item);
            }

            e.Handled = true;
        }
    }
}