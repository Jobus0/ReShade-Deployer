using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ReShadeDeployer.Registry;
using ReShadeDeployer.ViewModels;
using Testably.Abstractions;
using Wpf.Ui;
using Wpf.Ui.Appearance;

namespace ReShadeDeployer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
[ExcludeFromCodeCoverage]
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
        services.AddSingleton<IRegistry, WindowsRegistry>();
        services.AddSingleton<RegistryHelper>();
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
        services.AddSingleton<IConfig, IniFileConfig>();
        services.AddSingleton<IPeInfoReader, PeInfoReader>();
        services.AddSingleton<ExecutableContextFactory>();
        services.AddSingleton<IMessageBox, WpfMessageBox>();
        services.AddSingleton<IIconExtractor, IconExtractor>();
        services.AddSingleton<ISnackbarService, SnackbarService>();
        services.AddHttpClient<IDownloadService, DownloadService>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "ReShade Deployer");
        });
        
        ApplicationThemeManager.Changed += OnThemeChanged;
        ApplicationThemeManager.ApplySystemTheme();
        
        ServiceProvider = services.BuildServiceProvider();
        ServiceProvider.GetRequiredService<MainWindow>().Show();
        
        base.OnStartup(e);
    }
    
    private void OnThemeChanged(ApplicationTheme currentTheme, System.Windows.Media.Color accentColor)
    {
        // Select target dictionary based on current theme
        var newSource = currentTheme switch
        {
            ApplicationTheme.Light => new Uri("Themes/LightBrushes.xaml", UriKind.Relative),
            _ => new Uri("Themes/DarkBrushes.xaml", UriKind.Relative)
        };

        // Find the existing custom dictionary and swap its Source
        var mergedDictionaries = Current.Resources.MergedDictionaries;
        var customDict = mergedDictionaries.FirstOrDefault(d => d.Contains("ShadedBackgroundBrush"));

        if (customDict != null)
            customDict.Source = newSource;
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        SingleInstanceManager.Cleanup();
        ServiceProvider?.Dispose();
        
        base.OnExit(e);
    }
}