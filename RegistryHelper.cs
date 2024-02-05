using Microsoft.Win32;

namespace ReShadeDeployer;

public static class RegistryHelper
{
    private static string ActionNameToPath(string actionName)
    {
        return "exefile\\shell\\" + "Z" + actionName.Replace(" ", "");
    }
    
    public static void RegisterContextMenuAction(string actionName, string command, string? iconPath = null)
    {
        // Create the registry key for the action.
        using RegistryKey key = Registry.ClassesRoot.CreateSubKey(ActionNameToPath(actionName)); // Prefix with 'Z' to put it below 'Run as administrator'
        
        // Set the display name for the action
        key.SetValue("", actionName);
        
        // Create the value for the icon
        if (!string.IsNullOrEmpty(iconPath))
            key.SetValue("Icon", iconPath);

        // Create the registry key for the command to be executed
        using (RegistryKey commandKey = key.CreateSubKey("command"))
            commandKey.SetValue("", command);
    }
    
    public static void UnregisterContextMenuAction(string actionName)
    {
        // Delete the registry key for the action
        Registry.ClassesRoot.DeleteSubKeyTree(ActionNameToPath(actionName), true);
    }
    
    public static bool IsContextMenuActionRegistered(string actionName)
    {
        // Check if the registry key for the action exists
        using RegistryKey? key = Registry.ClassesRoot.OpenSubKey(ActionNameToPath(actionName));
        return key != null;
    }
}