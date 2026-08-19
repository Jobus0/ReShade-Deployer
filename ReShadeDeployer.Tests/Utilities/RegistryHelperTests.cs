using ReShadeDeployer.Registry;

namespace ReShadeDeployer.Tests.Utilities;

public class RegistryHelperTests
{
    private readonly MockRegistry _mockRegistry;
    private readonly RegistryHelper _registryHelper;

    public RegistryHelperTests()
    {
        _mockRegistry = new MockRegistry();
        _registryHelper = new RegistryHelper(_mockRegistry);
    }

    [Fact]
    public void IsContextMenuActionRegistered_ReturnsFalse_WhenNotRegistered()
    {
        // Arrange & Act
        bool result = _registryHelper.IsContextMenuActionRegistered("Deploy ReShade");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RegisterContextMenuAction_CreatesCorrectRegistryKeysAndValues()
    {
        // Arrange
        string actionName = "Deploy ReShade";
        string command = "C:\\Path\\To\\Deployer.exe \"%1\"";
        string iconPath = "C:\\Path\\To\\Deployer.exe,0";

        // Act
        _registryHelper.RegisterContextMenuAction(actionName, command, iconPath);

        // Assert
        bool result = _registryHelper.IsContextMenuActionRegistered(actionName);
        Assert.True(result);

        var key = _mockRegistry.ClassesRoot.OpenSubKey("exefile\\shell\\ZDeployReShade") as MockRegistryKey;
        Assert.NotNull(key);
        Assert.Equal(actionName, key.GetValue(""));
        Assert.Equal(iconPath, key.GetValue("Icon"));

        var commandKey = key.OpenSubKey("command") as MockRegistryKey;
        Assert.NotNull(commandKey);
        Assert.Equal(command, commandKey.GetValue(""));
    }

    [Fact]
    public void UnregisterContextMenuAction_DeletesRegistryKey()
    {
        // Arrange
        string actionName = "Deploy ReShade";
        string command = "C:\\Path\\To\\Deployer.exe \"%1\"";
        _registryHelper.RegisterContextMenuAction(actionName, command);

        // Act
        _registryHelper.UnregisterContextMenuAction(actionName);

        // Assert
        bool result = _registryHelper.IsContextMenuActionRegistered(actionName);
        Assert.False(result);
    }
}
