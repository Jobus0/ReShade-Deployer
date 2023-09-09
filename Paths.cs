using System.IO;

namespace ReShadeInstaller;

static class Paths
{
    public static readonly string Shaders = Path.Combine(Directory.GetCurrentDirectory(), "Shaders");
    public static readonly string Textures = Path.Combine(Directory.GetCurrentDirectory(), "Textures");
    public static readonly string Dlls = Path.Combine(Directory.GetCurrentDirectory(), "lib", "ReShade");
    public static readonly string AddonDlls = Path.Combine(Directory.GetCurrentDirectory(), "lib", "ReShade-Addons");
    public static readonly string Cache = Path.Combine(Directory.GetCurrentDirectory(), "lib", "Cache");
}