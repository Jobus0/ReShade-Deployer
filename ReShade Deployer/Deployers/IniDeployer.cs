﻿using System.IO;
using IniParser.Model;

namespace ReShadeDeployer;

public static class IniDeployer
{
    /// <summary>
    /// Create a new ReShade.ini at the given path using the values of a local ReShade.ini file, or fallback to a new minimal one.
    /// </summary>
    /// <param name="executableContext">Context for the executable to deploy.</param>
    public static void Deploy(ExecutableContext executableContext)
    {
        string path = Path.Combine(executableContext.DirectoryPath, "ReShade.ini");
        
        if (File.Exists(path))
            File.Delete(path);
        
        IniParser.FileIniDataParser iniParser = new();
        iniParser.Parser.Configuration.AssigmentSpacer = "";
        IniData ini = File.Exists(Paths.ReShadeIni)
            ? iniParser.ReadFile(Paths.ReShadeIni)
            : new IniData();

        ini["GENERAL"]["EffectSearchPaths"] = Paths.Shaders;
        ini["GENERAL"]["TextureSearchPaths"] = Paths.Textures;
        ini["GENERAL"]["IntermediateCachePath"] = Paths.Cache;
        ini["GENERAL"]["PresetPath"] = @".\ReShadePreset.ini";
        ini["GENERAL"]["PreprocessorDefinitions"] = $"RESHADE_DEPTH_LINEARIZATION_FAR_PLANE=1000.0," +
                                                    $"RESHADE_DEPTH_INPUT_IS_UPSIDE_DOWN={executableContext.DepthUpsideDown}," +
                                                    $"RESHADE_DEPTH_INPUT_IS_REVERSED={executableContext.DepthReversed}," +
                                                    $"RESHADE_DEPTH_INPUT_IS_LOGARITHMIC={executableContext.DepthLogarithmic}";
        ini["OVERLAY"]["TutorialProgress"] = "4";
        ini["DEPTH"]["DepthCopyBeforeClears"] = executableContext.DepthCopyBeforeClears;
        ini["DEPTH"]["UseAspectRatioHeuristics"] = executableContext.UseAspectRatioHeuristics;
        
        iniParser.WriteFile(path, ini);
    }
}