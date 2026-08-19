using System;
using Microsoft.Win32;

namespace ReShadeDeployer.Registry;

public interface IRegistryKey : IDisposable
{
    IRegistryKey? OpenSubKey(string name, bool writable = false);
    IRegistryKey CreateSubKey(string name);
    void DeleteSubKeyTree(string subkey, bool throwOnMissingSubKey = true);
    void DeleteValue(string name, bool throwOnMissingValue = true);
    void SetValue(string name, object value);
    void SetValue(string name, object value, RegistryValueKind valueKind);
    object? GetValue(string name);
}
