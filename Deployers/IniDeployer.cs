using System.IO;

namespace ReShadeDeployer;

public static class IniDeployer
{
    /// <summary>
    /// Deploy ReShade.ini to a directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory to write ReShade.ini to.</param>
    public static void Deploy(string directoryPath)
    {
        string path = directoryPath + "\\ReShade.ini";
        
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
        if (File.Exists(Paths.ReShadeIni))
        {
            using StreamReader streamReader = new StreamReader(Paths.ReShadeIni);
            using StreamWriter streamWriter = new StreamWriter(directoryPath);
            while (!streamReader.EndOfStream)
            {
                string str = streamReader.ReadLine()!;
                if (str.StartsWith("EffectSearchPaths="))
                    str = "EffectSearchPaths=" + Paths.Shaders;
                else if (str.StartsWith("IntermediateCachePath="))
                    str = "IntermediateCachePath=" + Paths.Cache;
                else if (str.StartsWith("TextureSearchPaths="))
                    str = "TextureSearchPaths=" + Paths.Textures;
                streamWriter.WriteLine(str);
            }
        }
        else
        {
            WriteReShadeIniFallback(directoryPath);
        }
    }

    /// <summary>
    /// Create a new minimal ReShade.ini at the given path.
    /// </summary>
    /// <param name="directoryPath">Path of the directory to write ReShade.ini to.</param>
    private static void WriteReShadeIniFallback(string directoryPath)
    {
        File.WriteAllText(directoryPath, $"""
            [GENERAL]
            EffectSearchPaths={Paths.Shaders}
            TextureSearchPaths={Paths.Textures}
            IntermediateCachePath={Paths.Cache}
            PresetPath=.\ReShadePreset.ini

            [OVERLAY]
            TutorialProgress=4
            """);
    }
}