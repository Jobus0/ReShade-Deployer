using System.Collections.Generic;
using System.IO.Abstractions;
using IniParser.Model;
using IniParser.Parser;

namespace ReShadeDeployer;

public class IniDeployer(IFileSystem fileSystem)
{
    /// <summary>
    /// Create a new ReShade.ini at the given path using the values of a local ReShade.ini file, or fallback to a new minimal one.
    /// </summary>
    /// <param name="executableContext">Context for the executable to deploy.</param>
    /// <param name="addons">List of addons being deployed. Used to update EffectSearchPaths and TextureSearchPaths.</param>
    /// <param name="presetPath">Path to the centralized preset folder, if used at all. Deploys to game's folder if null.</param>
    public void Deploy(ExecutableContext executableContext, IList<AddonItem> addons, string? presetPath = null)
    {
        string path = fileSystem.Path.Combine(executableContext.DirectoryPath, "ReShade.ini");
        
        if (fileSystem.File.Exists(path))
            fileSystem.File.Delete(path);
        
        var iniParser = new IniDataParser();
        iniParser.Configuration.AssigmentSpacer = "";
        
        IniData ini = fileSystem.File.Exists(Paths.ReShadeIni)
            ? iniParser.Parse(fileSystem.File.ReadAllText(Paths.ReShadeIni))
            : new IniData { Configuration = iniParser.Configuration.Clone() };

        ApplyContextualDefaults(ini, executableContext, presetPath);

        foreach (var addon in addons)
        {
            if (addon.HasShaders)
                ini["GENERAL"]["EffectSearchPaths"] += "," + addon.ShadersPath + "\\**";
            
            if (addon.HasTextures)
                ini["GENERAL"]["TextureSearchPaths"] += "," + addon.TexturesPath + "\\**";
        }
        
        fileSystem.File.WriteAllText(path, ini.ToString());
    }
    
    private void ApplyContextualDefaults(IniData ini, ExecutableContext executableContext, string? presetPath = null)
    {
        ini["GENERAL"]["EffectSearchPaths"] = Paths.Shaders + "\\**";
        ini["GENERAL"]["TextureSearchPaths"] = Paths.Textures + "\\**";
        ini["GENERAL"]["IntermediateCachePath"] = Paths.Cache;
        if (!string.IsNullOrEmpty(presetPath))
            ini["GENERAL"]["PresetPath"] = presetPath;
        ini["GENERAL"]["PreprocessorDefinitions"] =
            $"RESHADE_DEPTH_LINEARIZATION_FAR_PLANE=1000.0," +
            $"RESHADE_DEPTH_INPUT_IS_UPSIDE_DOWN={executableContext.DepthUpsideDown}," +
            $"RESHADE_DEPTH_INPUT_IS_REVERSED={executableContext.DepthReversed}," +
            $"RESHADE_DEPTH_INPUT_IS_LOGARITHMIC={executableContext.DepthLogarithmic}";
        ini["OVERLAY"]["TutorialProgress"] = "4";
        ini["DEPTH"]["DepthCopyBeforeClears"] = executableContext.DepthCopyBeforeClears;
        ini["DEPTH"]["DepthCopyAtClearIndex"] = executableContext.DepthCopyAtClearIndex;
        ini["DEPTH"]["DrawStatsHeuristic"] = executableContext.DrawStatsHeuristic;
        ini["DEPTH"]["UseAspectRatioHeuristics"] = executableContext.UseAspectRatioHeuristics;
    }
}