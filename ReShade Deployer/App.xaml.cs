using System.Windows;

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

        base.OnStartup(e);
    }
}