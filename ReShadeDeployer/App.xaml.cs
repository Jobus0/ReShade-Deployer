using System.IO.Abstractions;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ReShadeDeployer.ViewModels;
using Testably.Abstractions;

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
        if (!SingleInstanceManager.IsFirstInstance())
        {
            SingleInstanceManager.SendArgumentsToFirstInstance(e.Args);
            Shutdown();
            return;
        }

        StartupArgs = e.Args;
        SingleInstanceManager.OnArgumentsReceived += args => StartupArgs = args;

        var services = new ServiceCollection();
        services.AddSingleton<IFileSystem, RealFileSystem>();
        services.AddSingleton<FileHelper>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<UpdaterViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<AddonsViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<DeploymentOrchestrator>();
        services.AddSingleton<DllDeployer>();
        services.AddSingleton<VulkanSystemWideDeployer>();
        services.AddSingleton<IniDeployer>();
        services.AddSingleton<PresetDeployer>();
        services.AddSingleton<AddonsDeployer>();
        services.AddSingleton<ReShadeUndeployer>();
        services.AddSingleton<AppUpdater>();
        services.AddSingleton<ReShadeUpdater>();
        services.AddSingleton<DownloadService>();
        services.AddSingleton<IConfig, IniFileConfig>();
        services.AddSingleton<ExecutableContextFactory>();
        services.AddSingleton<IMessageBox, WpfMessageBox>();
        
        ServiceProvider = services.BuildServiceProvider();
        ServiceProvider.GetRequiredService<MainWindow>().Show();
        
        base.OnStartup(e);
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        SingleInstanceManager.Cleanup();
        ServiceProvider?.Dispose();
        
        base.OnExit(e);
    }
}