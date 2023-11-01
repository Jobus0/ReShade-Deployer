using System.IO;

namespace ReShadeInstaller;

public static class IniDeployer
{
    public static void Deploy(string gameDirectoryPath)
    {
        string path = gameDirectoryPath + "\\ReShade.ini";
        
        if (File.Exists(path))
            File.Delete(path);
        
        WriteReShadeIni(path);
    }

    private static void WriteReShadeIni(string reShadeIniPath)
    {
        if (File.Exists(".\\ReShade.ini"))
        {
            using StreamReader streamReader = new StreamReader(".\\ReShade.ini");
            using StreamWriter streamWriter = new StreamWriter(reShadeIniPath);
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
            WriteReShadeIniFallback(reShadeIniPath);
        }
    }

    private static void WriteReShadeIniFallback(string reShadeIniPath)
    {
        File.WriteAllText(reShadeIniPath, $"""
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