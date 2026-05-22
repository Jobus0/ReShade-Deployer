using Microsoft.Win32;

namespace ReShadeDeployer;

public class WindowsRegistry : IRegistry
{
    public IRegistryKey ClassesRoot => new WindowsRegistryKey(Registry.ClassesRoot);
    public IRegistryKey LocalMachine => new WindowsRegistryKey(Registry.LocalMachine);
    public IRegistryKey CurrentUser => new WindowsRegistryKey(Registry.CurrentUser);
}
