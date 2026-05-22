using System;
using Microsoft.Win32;

namespace ReShadeDeployer;

public class WindowsRegistryKey(RegistryKey registryKey) : IRegistryKey
{
    private readonly RegistryKey _registryKey = registryKey ?? throw new ArgumentNullException(nameof(registryKey));

    public IRegistryKey? OpenSubKey(string name, bool writable = false)
    {
        RegistryKey? subKey = _registryKey.OpenSubKey(name, writable);
        return subKey != null ? new WindowsRegistryKey(subKey) : null;
    }

    public IRegistryKey CreateSubKey(string name)
    {
        RegistryKey subKey = _registryKey.CreateSubKey(name);
        return new WindowsRegistryKey(subKey);
    }

    public void DeleteSubKeyTree(string subkey, bool throwOnMissingSubKey = true)
    {
        _registryKey.DeleteSubKeyTree(subkey, throwOnMissingSubKey);
    }

    public void DeleteValue(string name, bool throwOnMissingValue = true)
    {
        _registryKey.DeleteValue(name, throwOnMissingValue);
    }

    public void SetValue(string name, object value)
    {
        _registryKey.SetValue(name, value);
    }

    public void SetValue(string name, object value, RegistryValueKind valueKind)
    {
        _registryKey.SetValue(name, value, valueKind);
    }

    public object? GetValue(string name)
    {
        return _registryKey.GetValue(name);
    }

    public void Dispose()
    {
        _registryKey.Dispose();
    }
}
