using System;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using IniParser.Model;
using IniParser.Parser;
using ReShade.Setup.Utilities;

namespace ReShadeDeployer;

public class ExecutableContextFactory(IFileSystem fileSystem, IPeInfoReader peInfoReader)
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

    public ExecutableContextFactory(IFileSystem fileSystem) : this(fileSystem, new PeInfoReader()) { }
    
    public ExecutableContext Create(string executablePath)
    {
        ContextBuilder builder;
        try
        {
            executablePath = ResolveUnrealEngineExecutablePath(executablePath);
            
            var pe = peInfoReader.Read(executablePath);

            string fileName = fileSystem.Path.GetFileName(executablePath);
            string directoryPath = fileSystem.Path.GetDirectoryName(executablePath)!;
            
            builder = new ContextBuilder(fileName, directoryPath);

            DetectRenderApis(pe, builder);
            ApplyCompatibilitySettings(executablePath, builder);
            ApplyNvRemixBridgeOverride(builder);
        }
        catch (Exception)
        {
            string fileName = fileSystem.Path.GetFileName(executablePath);
            string directoryPath = fileSystem.Path.GetDirectoryName(executablePath)!;
            builder = new ContextBuilder(fileName, directoryPath)
            {
                DepthReversed = DetermineDepthReversedFallback(executablePath)
            };
        }

        return builder.Build();
    }

    private string ResolveUnrealEngineExecutablePath(string executablePath)
    {
        string? targetPathUnrealEngine = peInfoReader.ReadResourceString(executablePath, 201);
        if (!string.IsNullOrEmpty(targetPathUnrealEngine))
        {
            string? directoryName = fileSystem.Path.GetDirectoryName(executablePath);
            if (directoryName != null)
            {
                string combinedPath = fileSystem.Path.Combine(directoryName, targetPathUnrealEngine);
                string fullPath = fileSystem.Path.GetFullPath(combinedPath);
                if (fileSystem.File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }
        return executablePath;
    }

    private static void DetectRenderApis(PeInfoData pe, ContextBuilder builder)
    {
        foreach (var module in pe.Modules)
        {
            bool HasModule(string name) => module.StartsWith(name, StringComparison.OrdinalIgnoreCase);

            if (HasModule("d3d8"))
                builder.IsD3D9 = builder.IsD3D8 = true; // D3D8 will use d3d9.dll but inform user to use wrapper
            if (HasModule("d3d9"))
                builder.IsD3D9 = true;
            else if (HasModule("dxgi") || HasModule("d3d1") || HasModule("GFSDK"))
                builder.IsDXGI = true;
            else if (HasModule("opengl32"))
                builder.IsOpenGL = true;
            else if (HasModule("vulkan-1"))
                builder.IsVulkan = true;
        }

        builder.IsX64 = pe.Type == PeInfo.BinaryType.IMAGE_FILE_MACHINE_AMD64;
    }

    private void ApplyCompatibilitySettings(string executablePath, ContextBuilder builder)
    {
        if (!fileSystem.File.Exists(Paths.CompatibilityIni))
        {
            builder.DepthReversed = DetermineDepthReversedFallback(executablePath);
            return;
        }

        IniData ini = IniParser.Parse(fileSystem.File.ReadAllText(Paths.CompatibilityIni));
        string sectionKey = builder.FileName;

        string? installTarget = ini[sectionKey]["InstallTarget"];
        if (installTarget != null)
            builder.DirectoryPath = fileSystem.Path.Combine(builder.DirectoryPath, installTarget);
        
        string? is64Bit = ini[sectionKey]["Is64Bit"];
        if (is64Bit != null)
            builder.IsX64 = is64Bit == "1";
        
        string? renderApi = ini[sectionKey]["RenderApi"];
        if (renderApi != null)
            ApplyRenderApiOverride(renderApi, builder);
        
        string? banned = ini[sectionKey]["Banned"];
        if (banned != null)
            builder.Banned = banned == "1";

        builder.DepthReversed = ini[sectionKey]["DepthReversed"] ?? DetermineDepthReversedFallback(executablePath);
        builder.DepthUpsideDown = ini[sectionKey]["DepthUpsideDown"] ?? "0";
        builder.DepthLogarithmic = ini[sectionKey]["DepthLogarithmic"] ?? "0";
        builder.DepthCopyBeforeClears = ini[sectionKey]["DepthCopyBeforeClears"] ?? "0";
        builder.DepthCopyAtClearIndex = ini[sectionKey]["DepthCopyAtClearIndex"] ?? "0";
        builder.DrawStatsHeuristic = ini[sectionKey]["DrawStatsHeuristic"] ?? "0";
        builder.UseAspectRatioHeuristics = ini[sectionKey]["UseAspectRatioHeuristics"] ?? "1";
    }

    private static void ApplyRenderApiOverride(string renderApi, ContextBuilder builder)
    {
        if (renderApi == "D3D8")
            builder.IsD3D9 = builder.IsD3D8 = true;
        else if (renderApi == "D3D9")
            builder.IsD3D9 = true;
        else if (renderApi == "DXGI" || renderApi.StartsWith("D3D1"))
            builder.IsDXGI = true;
        else if (renderApi == "OpenGL")
            builder.IsOpenGL = true;
        else if (renderApi == "Vulkan")
            builder.IsVulkan = true;
    }

    private void ApplyNvRemixBridgeOverride(ContextBuilder builder)
    {
        string nvRemixPath = fileSystem.Path.Combine(builder.DirectoryPath, ".trex", "NvRemixBridge.exe");
        if (fileSystem.File.Exists(nvRemixPath))
        {
            builder.IsVulkan = true;
            builder.IsX64 = true;
            builder.DirectoryPath = fileSystem.Path.GetDirectoryName(nvRemixPath)!;
        }
    }

    private string DetermineDepthReversedFallback(string executablePath)
    {
        return TryGetFileCopyrightYear(executablePath, out int year) && year >= 2012 ? "1" : "0";
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

    private class ContextBuilder(string fileName, string directoryPath)
    {
        public string FileName { get; } = fileName;
        public string DirectoryPath { get; set; } = directoryPath;
        public bool IsX64 { get; set; } = true;
        public bool IsD3D8 { get; set; } = false;
        public bool IsD3D9 { get; set; } = false;
        public bool IsDXGI { get; set; } = false;
        public bool IsOpenGL { get; set; } = false;
        public bool IsVulkan { get; set; } = false;
        public string DepthReversed { get; set; } = "0";
        public string DepthUpsideDown { get; set; } = "0";
        public string DepthLogarithmic { get; set; } = "0";
        public string DepthCopyBeforeClears { get; set; } = "0";
        public string DepthCopyAtClearIndex { get; set; } = "0";
        public string DrawStatsHeuristic { get; set; } = "0";
        public string UseAspectRatioHeuristics { get; set; } = "1";
        public bool Banned { get; set; } = false;

        public ExecutableContext Build() => new(
            FileName,
            DirectoryPath,
            IsX64,
            IsD3D8,
            IsD3D9,
            IsDXGI,
            IsOpenGL,
            IsVulkan,
            DepthReversed,
            DepthUpsideDown,
            DepthLogarithmic,
            DepthCopyBeforeClears,
            DepthCopyAtClearIndex,
            DrawStatsHeuristic,
            UseAspectRatioHeuristics,
            Banned);
    }
}