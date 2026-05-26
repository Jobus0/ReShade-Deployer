using System;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using IniParser.Model;
using IniParser.Parser;
using ReShade.Setup.Utilities;

namespace ReShadeDeployer;

public class ExecutableContextFactory(IFileSystem fileSystem)
{
    private static readonly IniDataParser IniParser = new()
    {
        Configuration =
        {
            AllowDuplicateKeys = true,
            AllowDuplicateSections = true,
            CommentString = "#"
        }
    };
    
    public ExecutableContext Create(string executablePath)
    {
        string fileName;
        string directoryPath;
        bool isX64 = true;
        bool isD3D8 = false;
        bool isD3D9 = false;
        bool isDXGI = false;
        bool isOpenGL = false;
        bool isVulkan = false;
        string depthReversed;
        string depthUpsideDown = "0";
        string depthLogarithmic = "0";
        string depthCopyBeforeClears = "0";
        string depthCopyAtClearIndex = "0";
        string drawStatsHeuristic = "0";
        string useAspectRatioHeuristics = "1";
        
        try
        {
            var pe = new PEInfo(executablePath);
            
            // In case this is the bootstrap executable of an Unreal Engine game, try and find the actual game executable for it
            string targetPathUnrealEngine = PEInfo.ReadResourceString(executablePath, 201); // IDI_EXEC_FILE (see BootstrapPackagedGame.cpp in Unreal Engine source code)
            targetPathUnrealEngine = fileSystem.Path.GetFullPath(fileSystem.Path.Combine(fileSystem.Path.GetDirectoryName(executablePath)!, targetPathUnrealEngine));
            if (!string.IsNullOrEmpty(targetPathUnrealEngine) && fileSystem.File.Exists(targetPathUnrealEngine))
                executablePath = targetPathUnrealEngine;
            
            fileName = fileSystem.Path.GetFileName(executablePath);
            directoryPath = fileSystem.Path.GetDirectoryName(executablePath)!;

            foreach (var module in pe.Modules)
            {
                bool HasModule(string name) => module.StartsWith(name, StringComparison.OrdinalIgnoreCase);

                if (HasModule("d3d8"))
                    isD3D9 = isD3D8 = true; // D3D8 will use d3d9.dll but inform user to use wrapper
                if (HasModule("d3d9"))
                    isD3D9 = true;
                else if (HasModule("dxgi") || HasModule("d3d1") || HasModule("GFSDK"))
                    isDXGI = true;
                else if (HasModule("opengl32"))
                    isOpenGL = true;
                else if (HasModule("vulkan-1"))
                    isVulkan = true;
            }

            isX64 = pe.Type == PEInfo.BinaryType.IMAGE_FILE_MACHINE_AMD64;

            if (fileSystem.File.Exists(Paths.CompatibilityIni))
            {
                IniData ini = IniParser.Parse(fileSystem.File.ReadAllText(Paths.CompatibilityIni));

                string? installTarget = ini[fileName]["InstallTarget"];
                if (installTarget != null)
                    directoryPath = fileSystem.Path.Combine(directoryPath, installTarget);
                
                string? renderApi = ini[fileName]["RenderApi"];
                if (renderApi != null)
                {
                    if (renderApi == "D3D8")
                        isD3D9 = isD3D8 = true;
                    else if (renderApi == "D3D9")
                        isD3D9 = true;
                    else if (renderApi == "DXGI" || renderApi.StartsWith("D3D1"))
                        isDXGI = true;
                    else if (renderApi == "OpenGL")
                        isOpenGL = true;
                    else if (renderApi == "Vulkan")
                        isVulkan = true;
                }

                depthReversed = ini[fileName]["DepthReversed"]
                                ?? (TryGetFileCopyrightYear(executablePath, out int year) && year >= 2012 ? "1" : "0"); // Modern games usually use reversed depth
                depthUpsideDown = ini[fileName]["DepthUpsideDown"] ?? "0";
                depthLogarithmic = ini[fileName]["DepthLogarithmic"] ?? "0";
                depthCopyBeforeClears = ini[fileName]["DepthCopyBeforeClears"] ?? "0";
                depthCopyAtClearIndex = ini[fileName]["DepthCopyAtClearIndex"] ?? "0";
                drawStatsHeuristic = ini[fileName]["DrawStatsHeuristic"] ?? "0";
                useAspectRatioHeuristics = ini[fileName]["UseAspectRatioHeuristics"] ?? "1";
            }
            else
            {
                depthReversed = TryGetFileCopyrightYear(executablePath, out int year) && year >= 2012 ? "1" : "0";
            }

            string nvRemixPath = fileSystem.Path.Combine(directoryPath, ".trex", "NvRemixBridge.exe");
            if (fileSystem.File.Exists(nvRemixPath))
            {
                isVulkan = true;
                isX64 = true;
                directoryPath = fileSystem.Path.GetDirectoryName(nvRemixPath)!;
            }
        }
        catch (Exception)
        {
            fileName = fileSystem.Path.GetFileName(executablePath);
            directoryPath = fileSystem.Path.GetDirectoryName(executablePath)!;
            depthReversed = TryGetFileCopyrightYear(executablePath, out int year) && year >= 2012 ? "1" : "0";
        }

        return new ExecutableContext(
            fileName,
            directoryPath,
            isX64,
            isD3D8,
            isD3D9,
            isDXGI,
            isOpenGL,
            isVulkan,
            depthReversed,
            depthUpsideDown,
            depthLogarithmic,
            depthCopyBeforeClears,
            depthCopyAtClearIndex,
            drawStatsHeuristic,
            useAspectRatioHeuristics);
    }
    
    private bool TryGetFileCopyrightYear(string executablePath, out int year)
    {
        var info = fileSystem.FileVersionInfo.GetVersionInfo(executablePath);
        if (info.LegalCopyright != null)
        {
            Match match = new Regex("(20[0-9]{2})", RegexOptions.RightToLeft).Match(info.LegalCopyright);
            if (match.Success && int.TryParse(match.Groups[1].Value, out year))
                return true;
        }

        year = default;
        return false;
    }
}