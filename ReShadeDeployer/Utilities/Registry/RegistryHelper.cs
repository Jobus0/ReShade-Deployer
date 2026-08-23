namespace ReShadeDeployer.Registry;

public class RegistryHelper(IRegistry registry)
{
    private string ActionNameToPath(string actionName)
    {
        return "exefile\\shell\\" + "Z" + actionName.Replace(" ", ""); // Prefix with 'Z' to put it below 'Run as administrator'
    }
    
    public void RegisterContextMenuAction(string actionName, string command, string? iconPath = null)
    {
        // Create the registry key for the action.
        using IRegistryKey key = registry.ClassesRoot.CreateSubKey(ActionNameToPath(actionName));
        
        // Set the display name for the action
        key.SetValue("", actionName);
        
        // Create the value for the icon
        if (!string.IsNullOrEmpty(iconPath))
            key.SetValue("Icon", iconPath);

        // Create the registry key for the command to be executed
        using IRegistryKey commandKey = key.CreateSubKey("command");
        commandKey.SetValue("", command);
    }
    
    public void UnregisterContextMenuAction(string actionName)
    {
        // Delete the registry key for the action
        registry.ClassesRoot.DeleteSubKeyTree(ActionNameToPath(actionName), true);
    }
    
    public bool IsContextMenuActionRegistered(string actionName)
    {
        // Check if the registry key for the action exists
        using IRegistryKey? key = registry.ClassesRoot.OpenSubKey(ActionNameToPath(actionName));
        return key != null;
    }
    
    public string? GetContextMenuActionCommand(string actionName)
    {
        // Open the registry key for the action
        using IRegistryKey? key = registry.ClassesRoot.OpenSubKey(ActionNameToPath(actionName));
        if (key == null)
            return null;
        
        // Open the command subkey
        using IRegistryKey? commandKey = key.OpenSubKey("command");
        if (commandKey == null)
            return null;
        
        // Return the command string from the default value
        return commandKey.GetValue("") as string;
    }
}