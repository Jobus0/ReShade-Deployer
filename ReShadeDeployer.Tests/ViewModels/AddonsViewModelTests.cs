using Testably.Abstractions.Testing;
using ReShadeDeployer.ViewModels;
using static ReShadeDeployer.Tests.TestHelpers;

namespace ReShadeDeployer.Tests.ViewModels;

public class AddonsViewModelTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly MockConfig _config;
    private readonly AddonsDeployer _addonsDeployer;

    public AddonsViewModelTests()
    {
        _fileSystem = new MockFileSystem();
        var fileHelper = new FileHelper(_fileSystem);
        _addonsDeployer = new AddonsDeployer(_fileSystem, fileHelper);
        _config = new MockConfig();

        // Create standard paths
        _fileSystem.Directory.CreateDirectory(Paths.Root);
        _fileSystem.Directory.CreateDirectory(Paths.Addons);
        
        // Write dummy addon files to detect
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(Paths.Addons, "AddonA.addon64"), "64bit");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(Paths.Addons, "AddonB.addon32"), "32bit");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(Paths.Addons, "AddonC.addon64"), "64bit");
        _fileSystem.File.WriteAllText(_fileSystem.Path.Combine(Paths.Addons, "AddonC.addon32"), "32bit");
    }

    [Fact]
    public void Constructor_RestoresLastSelectedAddonsFromConfig()
    {
        // Arrange
        _config.LastSelectedAddons = new[] { "AddonA", "AddonC" };

        // Act
        var viewModel = new AddonsViewModel(_addonsDeployer, _config);

        // Assert
        Assert.Equal(3, viewModel.Addons.Count);
        
        var addonA = viewModel.Addons.First(a => a.Name == "AddonA");
        var addonB = viewModel.Addons.First(a => a.Name == "AddonB");
        var addonC = viewModel.Addons.First(a => a.Name == "AddonC");

        Assert.True(addonA.IsSelected);
        Assert.False(addonB.IsSelected);
        Assert.True(addonC.IsSelected);

        // Verify summary text lists selected addons
        Assert.Contains("AddonA", viewModel.AddonsSummaryText);
        Assert.Contains("AddonC", viewModel.AddonsSummaryText);
        Assert.DoesNotContain("AddonB", viewModel.AddonsSummaryText);
    }

    [Fact]
    public void OnAddonItemClicked_TogglesSelection_UpdatesSummaryAndConfig()
    {
        // Arrange
        var viewModel = new AddonsViewModel(_addonsDeployer, _config);
        var addonA = viewModel.Addons.First(a => a.Name == "AddonA");
        var addonB = viewModel.Addons.First(a => a.Name == "AddonB");

        Assert.False(addonA.IsSelected);
        Assert.False(addonB.IsSelected);

        // Act - Click AddonA
        viewModel.OnAddonItemClicked(addonA);

        // Assert
        Assert.True(addonA.IsSelected);
        Assert.Equal(new[] { "AddonA" }, _config.LastSelectedAddons);
        Assert.Equal("AddonA", viewModel.AddonsSummaryText);

        // Act - Click AddonB
        viewModel.OnAddonItemClicked(addonB);

        // Assert
        Assert.True(addonB.IsSelected);
        Assert.Equal(new[] { "AddonA", "AddonB" }, _config.LastSelectedAddons);
        Assert.Contains("AddonA", viewModel.AddonsSummaryText);
        Assert.Contains("AddonB", viewModel.AddonsSummaryText);

        // Act - Click AddonA again (to deselect)
        viewModel.OnAddonItemClicked(addonA);

        // Assert
        Assert.False(addonA.IsSelected);
        Assert.Equal(new[] { "AddonB" }, _config.LastSelectedAddons);
        Assert.Equal("AddonB", viewModel.AddonsSummaryText);
    }

    [Theory]
    // (addonSupportChecked, executableContextIsX64, expectAddonASupported, expectAddonBSupported)
    [InlineData(true, true, true, false)]   // AddonA has x64, AddonB has x32
    [InlineData(true, false, false, true)]  // AddonA has x64, AddonB has x32
    [InlineData(false, true, false, false)] // Addon support disabled globally
    [InlineData(false, false, false, false)]
    public void Refresh_FiltersSupportedAddons_AndSetsComboVisibility(
        bool addonSupportChecked, 
        bool isX64, 
        bool expectAddonASupported, 
        bool expectAddonBSupported)
    {
        // Arrange
        var viewModel = new AddonsViewModel(_addonsDeployer, _config);
        var context = CreateExecutableContext(isX64: isX64);

        // Act
        viewModel.Refresh(addonSupportChecked, context);

        // Assert
        var addonA = viewModel.Addons.First(a => a.Name == "AddonA");
        var addonB = viewModel.Addons.First(a => a.Name == "AddonB");

        Assert.Equal(expectAddonASupported, addonA.IsSupported);
        Assert.Equal(expectAddonBSupported, addonB.IsSupported);

        // Combo is visible if addon support is checked AND there is at least one supported addon
        bool expectedComboVisible = addonSupportChecked && (expectAddonASupported || expectAddonBSupported);
        Assert.Equal(expectedComboVisible, viewModel.IsAddonsComboVisible);
    }
}
