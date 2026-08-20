using System.Diagnostics.CodeAnalysis;

namespace ReShadeDeployer.Registry;

[ExcludeFromCodeCoverage]
public class WindowsRegistry : IRegistry
{
    public IRegistryKey ClassesRoot => new WindowsRegistryKey(Microsoft.Win32.Registry.ClassesRoot);
    public IRegistryKey LocalMachine => new WindowsRegistryKey(Microsoft.Win32.Registry.LocalMachine);
    public IRegistryKey CurrentUser => new WindowsRegistryKey(Microsoft.Win32.Registry.CurrentUser);
}
