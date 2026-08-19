namespace ReShadeDeployer.Registry;

public interface IRegistry
{
    IRegistryKey ClassesRoot { get; }
    IRegistryKey LocalMachine { get; }
    IRegistryKey CurrentUser { get; }
}
