using System;
using System.Windows;
using System.Windows.Threading;

namespace ReShadeDeployer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public string[] StartupArgs = null!;
        
    protected override void OnStartup(StartupEventArgs e)
    {
        StartupArgs = e.Args;
        
        Dispatcher.UnhandledException += DispatcherOnUnhandledException;

        base.OnStartup(e);
    }
    
    private void DispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (Current?.MainWindow != null)
        {
            Current.MainWindow.Hide();
            WpfMessageBox.Show(e.Exception);
        }
    }
}