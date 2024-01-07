using System.Windows;

namespace ReShadeDeployer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public string? TargetExecutablePath { get; private set; }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            TargetExecutablePath = e.Args.Length > 0 ? e.Args[0] : null;
            
            base.OnStartup(e);
        }
    }
}