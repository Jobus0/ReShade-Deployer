using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ReShadeDeployer;

/// <summary>
/// Controls how files are deployed for an addon (symlinks vs. copies). Note that copies are not supported for undeployment.
/// </summary>
public enum OverrideCopyMode
{
    /// <summary>Default behaviour: addon binaries use symlinks, config files are copied.</summary>
    Default,

    /// <summary>Always copy all files instead of creating symlinks.</summary>
    AlwaysCopy,

    /// <summary>Always use symlinks, even for AdditionalConfigFiles.</summary>
    AlwaysSymlinks,
}

public class AddonItem : INotifyPropertyChanged
{
    public string Name { get; init; } = string.Empty;
    public List<string> X32Paths { get; } = new();
    public List<string> X64Paths { get; } = new();
    
    public bool HasX32Addon => X32Paths.Count > 0;
    public bool HasX64Addon => X64Paths.Count > 0;
    public bool HasAnyAddon => HasX32Addon || HasX64Addon;

    public string ShadersPath { get; set; } = string.Empty;
    public string TexturesPath { get; set; } = string.Empty;

    public bool HasShaders => !string.IsNullOrEmpty(ShadersPath);
    public bool HasTextures => !string.IsNullOrEmpty(TexturesPath);
    
    public List<string>? AdditionalFiles { get; set; }
    public List<string>? AdditionalConfigFiles { get; set; }

    // addon.ini config properties:

    /// <summary>
    /// Optional relative path to a runnable setup script (.bat, .ps1, .exe)
    /// executed at the end of the addon deployment phase.
    /// Only populated for addons living in a sub-directory of the Add-ons folder.
    /// </summary>
    public string? SetupFile { get; set; }

    /// <summary>
    /// Optional name or relative path for the ReShade DLL symlink.
    /// When set, overrides the default API-derived name (e.g. "dxgi.dll").
    /// A path like "Custom\MyReShade.dll" places the symlink inside a "Custom"
    /// sub-directory (created if necessary) with the specified file name.
    /// Only populated for addons living in a sub-directory of the Add-ons folder.
    /// </summary>
    public string? ReShadeDllNameOverride { get; set; }

    /// <summary>
    /// Controls how files are deployed for this addon.
    /// Only populated for addons living in a sub-directory of the Add-ons folder.
    /// </summary>
    public OverrideCopyMode OverrideCopy { get; set; } = OverrideCopyMode.Default;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }
    
    private bool _isSupported = true;
    public bool IsSupported
    {
        get => _isSupported;
        set
        {
            if (_isSupported == value) return;
            _isSupported = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}