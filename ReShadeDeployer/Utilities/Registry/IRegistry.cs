namespace ReShadeDeployer;

public interface IRegistry
{
    IRegistryKey ClassesRoot { get; }
    IRegistryKey LocalMachine { get; }
    IRegistryKey CurrentUser { get; }
}
