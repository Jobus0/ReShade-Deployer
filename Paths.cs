using System.IO;

namespace ReShadeInstaller;

/// <summary>
/// Paths to different sub-directories within the application directory.
/// </summary>
public static class Paths
{
    private static readonly string Root = System.AppContext.BaseDirectory;
    
    public static readonly string Shaders = Path.Combine(Root, "Shaders");
    public static readonly string Textures = Path.Combine(Root, "Textures");
    public static readonly string Dlls = Path.Combine(Root, "lib", "ReShade");
    public static readonly string AddonDlls = Path.Combine(Root, "lib", "ReShade-Addons");
    public static readonly string Cache = Path.Combine(Root, "lib", "Cache");
}