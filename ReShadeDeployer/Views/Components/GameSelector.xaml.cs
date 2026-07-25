using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ReShadeDeployer.ViewModels;

namespace ReShadeDeployer.Views.Components;

public partial class GameSelector : UserControl
{
    public GameSelector()
    {
        InitializeComponent();
    }
    
    private void SelectGameButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TrySelectExecutable(out string executablePath) && DataContext is MainViewModel viewModel)
            viewModel.SelectGameCommand.Execute(executablePath);
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
}