using System.IO;
using IniParser.Model;

namespace ReShadeDeployer;

public static class IniDeployer
{
    /// <summary>
    /// Deploy ReShade.ini to a directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory to write ReShade.ini to.</param>
    public static void Deploy(string directoryPath)
    {
        string path = Path.Combine(directoryPath, "ReShade.ini");
        
        if (File.Exists(path))
            File.Delete(path);
        
        WriteReShadeIni(path);
    }

    /// <summary>
    /// Create a new ReShade.ini at the given path using the values of a local ReShade.ini file, or fallback to a new minimal one.
    /// </summary>
    /// <param name="directoryPath">Path of the directory to write ReShade.ini to.</param>
    private static void WriteReShadeIni(string directoryPath)
    {
        IniParser.FileIniDataParser iniParser = new();
        iniParser.Parser.Configuration.AssigmentSpacer = "";
        IniData ini = File.Exists(Paths.ReShadeIni)
            ? iniParser.ReadFile(Paths.ReShadeIni)
            : new IniData();

        ini["GENERAL"]["EffectSearchPaths"] = Paths.Shaders;
        ini["GENERAL"]["TextureSearchPaths"] = Paths.Textures;
        ini["GENERAL"]["IntermediateCachePath"] = Paths.Cache;
        ini["GENERAL"]["PresetPath"] = @".\ReShadePreset.ini";
        ini["OVERLAY"]["TutorialProgress"] = "4";
        
        iniParser.WriteFile(directoryPath, ini);
    }
}