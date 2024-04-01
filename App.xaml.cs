using System.Windows;

namespace ReShadeDeployer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Executable path from the launch arguments. Used when deployed from the context menu.
    /// </summary>
    public string? TargetExecutablePath { get; set; }
        
    protected override void OnStartup(StartupEventArgs e)
    {
        TargetExecutablePath = e.Args.Length > 0 ? e.Args[0] : null;

        base.OnStartup(e);
    }
}