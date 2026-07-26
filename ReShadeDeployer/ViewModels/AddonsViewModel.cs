using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReShadeDeployer.ViewModels;

public partial class AddonsViewModel : ObservableObject
{
    private readonly IConfig _config;

    public IList<AddonItem> Addons { get; }

    [ObservableProperty]
    private bool _isAddonSupportChecked;

    public event Action<bool>? AddonSupportCheckedChanged;

    [ObservableProperty]
    private bool _isAddonsComboVisible;

    [ObservableProperty]
    private string _addonsSummaryText = UIStrings.AddonSummaryPlaceholder;

    public AddonsViewModel(AddonsDeployer addonsDeployer, IConfig config)
    {
        _config = config;

        Addons = addonsDeployer.GetAvailableAddons();

        var lastSelectedAddons = _config.LastSelectedAddons;
        foreach (var addonItem in Addons)
            if (lastSelectedAddons.Contains(addonItem.Name))
                addonItem.IsSelected = true;

        UpdateAddonsSummaryText();
    }

    /// <summary>
    /// Called by the code-behind when a user clicks an addon item in the ComboBox.
    /// </summary>
    public void OnAddonItemClicked(AddonItem item)
    {
        item.IsSelected = !item.IsSelected;
        UpdateAddonsSummaryText();
        _config.LastSelectedAddons = Addons.Where(a => a.IsSelected).Select(a => a.Name).ToArray();
    }

    /// <summary>
    /// Refreshes the combo visibility and summary text based on the current
    /// addon-support toggle state and the selected executable context.
    /// Must be called by MainViewModel whenever either changes.
    /// </summary>
    public void Refresh(bool addonSupportChecked, ExecutableContext? executableContext)
    {
        foreach (AddonItem addonItem in Addons)
        {
            bool hasMatchingX64 = addonSupportChecked && executableContext is { IsX64: true } && addonItem.HasX64Addon;
            bool hasMatchingX32 = addonSupportChecked && executableContext is { IsX64: false } && addonItem.HasX32Addon;
            addonItem.IsSupported = hasMatchingX64 || hasMatchingX32 || !addonItem.HasAnyAddon;
        }

        var hasSupportedAddons = Addons.Any(a => a.IsSupported);
        var hasNonAddonAddons = Addons.Any(a => !a.HasAnyAddon);
        IsAddonsComboVisible = (addonSupportChecked || hasNonAddonAddons) && hasSupportedAddons;

        UpdateAddonsSummaryText();
    }

    /// <summary>Returns the addons that are both selected and supported.</summary>
    public IList<AddonItem> GetSelectedAddons()
    {
        return Addons.Where(addon => addon.IsSelected && addon.IsSupported).ToArray();
    }

    private void UpdateAddonsSummaryText()
    {
        var selected = GetSelectedAddons();
        AddonsSummaryText = selected.Count == 0
            ? UIStrings.AddonSummaryPlaceholder
            : string.Join(", ", selected.Select(addon => addon.Name));
    }
    
    partial void OnIsAddonSupportCheckedChanged(bool value)
    {
        AddonSupportCheckedChanged?.Invoke(value);
    }
}
