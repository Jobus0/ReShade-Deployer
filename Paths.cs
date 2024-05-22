using System.IO;

namespace ReShadeDeployer;

/// <summary>
/// Paths to different sub-directories within the application directory.
/// </summary>
public static class Paths
{
    private static readonly string Root = System.AppContext.BaseDirectory;
    
    public static readonly string Shaders           = Path.Combine(Root, "Shaders");
    public static readonly string Textures          = Path.Combine(Root, "Textures");
    public static readonly string ReShadeIni        = Path.Combine(Root, "ReShade.ini");
    public static readonly string ReShadePresetIni  = Path.Combine(Root, "ReShadePreset.ini");
    
    private static readonly string Lib              = Path.Combine(Root, "lib");
    public static readonly string Dlls              = Path.Combine(Lib, "ReShade");
    public static readonly string AddonDlls         = Path.Combine(Lib, "ReShade-AddonSupport");
    public static readonly string Cache             = Path.Combine(Lib, "Cache");
    public static readonly string ConfigIni         = Path.Combine(Lib, "Config.ini");
    public static readonly string CompatibilityIni  = Path.Combine(Lib, "Compatibility.ini");
}