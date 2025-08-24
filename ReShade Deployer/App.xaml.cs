using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace ReShadeDeployer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public ServiceProvider? ServiceProvider { get; private set; }
    public string[] StartupArgs = null!;
        
    protected override void OnStartup(StartupEventArgs e)
    {
        StartupArgs = e.Args;
        
        var services = new ServiceCollection();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<Deployer>();
        services.AddSingleton<DllDeployer>();
        services.AddSingleton<IniDeployer>();
        services.AddSingleton<PresetDeployer>();
        services.AddSingleton<AppUpdater>();
        services.AddSingleton<ReShadeUpdater>();
        services.AddSingleton<DownloadService>();
        services.AddSingleton<IConfig, IniFileConfig>();
        services.AddSingleton<IMessageBox, WpfMessageBox>();
        
        ServiceProvider = services.BuildServiceProvider();
        
        Dispatcher.UnhandledException += (_, _) => ServiceProvider?.Dispose();
        
        ServiceProvider.GetRequiredService<MainWindow>().Show();
        
        base.OnStartup(e);
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        ServiceProvider?.Dispose();
        
        base.OnExit(e);
    }
}