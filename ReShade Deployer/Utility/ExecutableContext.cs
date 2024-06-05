using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ReShade.Setup.Utilities;
using IniParser;
using IniParser.Model;

namespace ReShadeDeployer;

/// <summary>
/// Represents the context of an executable file, providing information about the file such as name, directory path, and the graphics APIs it uses.
/// </summary>
public class ExecutableContext
{
    public readonly string FileName;
    public readonly string DirectoryPath;
    public readonly bool IsX64;

    // Note that an executable can contain more than one graphics API.
    public readonly bool IsD3D8;
    public readonly bool IsD3D9;
    public readonly bool IsDXGI;
    public readonly bool IsOpenGL;
    public readonly bool IsVulkan;

    // Ini settings
    public readonly string DepthReversed;
    public readonly string DepthUpsideDown;
    public readonly string DepthLogarithmic;
    public readonly string DepthCopyBeforeClears;
    public readonly string UseAspectRatioHeuristics;

    private readonly IEnumerable<string>? modules;

    private static readonly FileIniDataParser IniParser = new()
    {
        Parser =
        {
            Configuration =
            {
                AllowDuplicateKeys = true,
                AllowDuplicateSections = true,
                CommentString = "#"
            }
        }
    };

    public ExecutableContext(string executablePath)
    {
        try
        {
            var pe = new PEInfo(executablePath);
            modules = pe.Modules;

            // In case this is the bootstrap executable of an Unreal Engine game, try and find the actual game executable for it
            string targetPathUnrealEngine = PEInfo.ReadResourceString(executablePath, 201); // IDI_EXEC_FILE (see BootstrapPackagedGame.cpp in Unreal Engine source code)
            if (!string.IsNullOrEmpty(targetPathUnrealEngine) && File.Exists(targetPathUnrealEngine = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(executablePath)!, targetPathUnrealEngine))))
            {
                FileName = Path.GetFileName(targetPathUnrealEngine);
                DirectoryPath = Path.GetDirectoryName(targetPathUnrealEngine)!;
            }
            else
            {
                FileName = Path.GetFileName(executablePath);
                DirectoryPath = Path.GetDirectoryName(executablePath)!;
            }

            foreach (var module in modules)
            {
                bool HasModule(string name) => module.StartsWith(name, StringComparison.OrdinalIgnoreCase);

                if (HasModule("d3d8"))
                    IsD3D9 = IsD3D8 = true; // D3D8 will use d3d9.dll but inform user to use wrapper
                if (HasModule("d3d9"))
                    IsD3D9 = true;
                else if (HasModule("dxgi") || HasModule("d3d1") || HasModule("GFSDK"))
                    IsDXGI = true;
                else if (HasModule("opengl32"))
                    IsOpenGL = true;
                else if (HasModule("vulkan-1"))
                    IsVulkan = true;
            }

            IsX64 = pe.Type == PEInfo.BinaryType.IMAGE_FILE_MACHINE_AMD64;

            if (File.Exists(Paths.CompatibilityIni))
            {
                IniData ini = IniParser.ReadFile(Paths.CompatibilityIni);

                string? installTarget = ini[FileName]["InstallTarget"];
                if (installTarget != null)
                    DirectoryPath = Path.Combine(DirectoryPath, installTarget);
                
                string? renderApi = ini[FileName]["RenderApi"];
                if (renderApi != null)
                {
                    if (renderApi == "D3D8")
                        IsD3D9 = IsD3D8 = true;
                    else if (renderApi == "D3D9")
                        IsD3D9 = true;
                    else if (renderApi == "DXGI" || renderApi.StartsWith("D3D1"))
                        IsDXGI = true;
                    else if (renderApi == "OpenGL")
                        IsOpenGL = true;
                    else if (renderApi == "Vulkan")
                        IsVulkan = true;
                }

                DepthReversed = ini[FileName]["DepthReversed"]
                                ?? (TryGetFileCopyrightYear(executablePath, out int year) && year >= 2012 ? "1" : "0"); // Modern games usually use reversed depth
                DepthUpsideDown = ini[FileName]["DepthUpsideDown"] ?? "0";
                DepthLogarithmic = ini[FileName]["DepthLogarithmic"] ?? "0";
                DepthCopyBeforeClears = ini[FileName]["DepthCopyBeforeClears"] ?? "0";
                UseAspectRatioHeuristics = ini[FileName]["UseAspectRatioHeuristics"] ?? "1";
            }
            else
            {
                DepthReversed = TryGetFileCopyrightYear(executablePath, out int year) && year >= 2012 ? "1" : "0";
                DepthUpsideDown = "0";
                DepthLogarithmic = "0";
                DepthCopyBeforeClears = "0";
                UseAspectRatioHeuristics = "1";
            }

            string nvRemixPath = Path.Combine(DirectoryPath, ".trex", "NvRemixBridge.exe");
            if (File.Exists(nvRemixPath))
            {
                IsVulkan = true;
                IsX64 = true;
                DirectoryPath = Path.GetDirectoryName(nvRemixPath)!;
            }
        }
        catch (Exception)
        {
            FileName = Path.GetFileName(executablePath);
            DirectoryPath = Path.GetDirectoryName(executablePath)!;

            DepthReversed = TryGetFileCopyrightYear(executablePath, out int year) && year >= 2012 ? "1" : "0";
            DepthUpsideDown = "0";
            DepthLogarithmic = "0";
            DepthCopyBeforeClears = "0";
            UseAspectRatioHeuristics = "1";
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.AppendLine("Target");
        sb.Append("    ");
        sb.Append(FileName);
        sb.AppendLine(IsX64 ? " (x64)" : " (x86)");
        sb.AppendLine();

        sb.AppendLine("Deployment Path");
        sb.Append("    ");
        sb.AppendLine(DirectoryPath);
        sb.AppendLine();

        sb.AppendLine("Graphics APIs");
        if (IsD3D8)
            sb.AppendLine("    D3D8");
        if (IsD3D9)
            sb.AppendLine("    D3D9");
        if (IsDXGI)
            sb.AppendLine("    DXGI");
        if (IsOpenGL)
            sb.AppendLine("    OpenGL");
        if (IsVulkan)
            sb.AppendLine("    Vulkan");
        sb.AppendLine();
        
        sb.AppendLine(".ini Defaults");
        sb.AppendLine("    DepthReversed: " + DepthReversed);
        sb.AppendLine("    DepthUpsideDown: " + DepthUpsideDown);
        sb.AppendLine("    DepthLogarithmic: " + DepthLogarithmic);
        sb.AppendLine("    DepthCopyBeforeClears: " + DepthCopyBeforeClears);
        sb.AppendLine("    UseAspectRatioHeuristics: " + UseAspectRatioHeuristics);
        sb.AppendLine();

        if (modules != null)
        {
            sb.AppendLine("Modules");
            foreach (var module in modules)
            {
                sb.Append("    ");
                sb.AppendLine(module);
            }
        }

        return sb.ToString().Trim();
    }

    private static bool TryGetFileCopyrightYear(string executablePath, out int year)
    {
        var info = FileVersionInfo.GetVersionInfo(executablePath);
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