using Microsoft.Win32;

namespace ReShadeDeployer.Tests;

public class MockRegistry : IRegistry
{
    public IRegistryKey ClassesRoot { get; } = new MockRegistryKey("HKEY_CLASSES_ROOT");
    public IRegistryKey LocalMachine { get; } = new MockRegistryKey("HKEY_LOCAL_MACHINE");
    public IRegistryKey CurrentUser { get; } = new MockRegistryKey("HKEY_CURRENT_USER");
}

public class MockRegistryKey : IRegistryKey
{
    public string Name { get; }
    public Dictionary<string, MockRegistryKey> SubKeys { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, (object Value, RegistryValueKind Kind)> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

    public MockRegistryKey(string name)
    {
        Name = name;
    }

    public IRegistryKey? OpenSubKey(string name, bool writable = false)
    {
        string[] parts = name.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        MockRegistryKey current = this;
        foreach (var part in parts)
        {
            if (!current.SubKeys.TryGetValue(part, out var next))
            {
                return null;
            }
            current = next;
        }
        return current;
    }

    public IRegistryKey CreateSubKey(string name)
    {
        string[] parts = name.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        MockRegistryKey current = this;
        foreach (var part in parts)
        {
            if (!current.SubKeys.TryGetValue(part, out var next))
            {
                next = new MockRegistryKey(part);
                current.SubKeys[part] = next;
            }
            current = next;
        }
        return current;
    }

    public void DeleteSubKeyTree(string subkey, bool throwOnMissingSubKey = true)
    {
        string[] parts = subkey.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        MockRegistryKey current = this;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (!current.SubKeys.TryGetValue(parts[i], out var next))
            {
                if (throwOnMissingSubKey)
                    throw new ArgumentException("Subkey not found.");
                return;
            }
            current = next;
        }

        string lastPart = parts[^1];
        if (!current.SubKeys.Remove(lastPart) && throwOnMissingSubKey)
        {
            throw new ArgumentException("Subkey not found.");
        }
    }

    public void DeleteValue(string name, bool throwOnMissingValue = true)
    {
        if (!Values.Remove(name) && throwOnMissingValue)
        {
            throw new ArgumentException("Value not found.");
        }
    }

    public void SetValue(string name, object value)
    {
        SetValue(name, value, RegistryValueKind.Unknown);
    }

    public void SetValue(string name, object value, RegistryValueKind valueKind)
    {
        Values[name] = (value, valueKind);
    }

    public object? GetValue(string name)
    {
        return Values.TryGetValue(name, out var val) ? val.Value : null;
    }

    public void Dispose()
    {
        // No-op for mock simplicity so assertions can run post-disposal
    }
}
