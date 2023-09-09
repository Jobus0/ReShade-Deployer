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

        public static void WriteReShadeIni(string reShadeIniPath)
        {
            if (File.Exists(".\\ReShade.ini"))
            {
                using (StreamReader streamReader = new StreamReader(".\\ReShade.ini"))
                {
                    using (StreamWriter streamWriter = new StreamWriter(reShadeIniPath))
                    {
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
                }
            }
            else
            {
                WriteReShadeIniFallback(reShadeIniPath);
            }
        }

        public static void WriteReShadeIniFallback(string reShadeIniPath)
        {
            File.WriteAllText(reShadeIniPath, $"[GENERAL]\nEffectSearchPaths={Paths.Shaders}\nTextureSearchPaths={Paths.Textures}\nIntermediateCachePath={Paths.Cache}\nPresetPath=.\\ReShadePreset.ini\n\n[OVERLAY]\nTutorialProgress=4");
        }
}