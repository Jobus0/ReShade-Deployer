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
public record ExecutableContext(
    string FileName,
    string DirectoryPath,
    bool IsX64,
    bool IsD3D8,
    bool IsD3D9,
    bool IsDXGI,
    bool IsOpenGL,
    bool IsVulkan,
    string DepthReversed,
    string DepthUpsideDown,
    string DepthLogarithmic,
    string DepthCopyBeforeClears,
    string DepthCopyAtClearIndex,
    string DrawStatsHeuristic,
    string UseAspectRatioHeuristics
)
{
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
        sb.AppendLine("    DepthCopyAtClearIndex: " + DepthCopyAtClearIndex);
        sb.AppendLine("    DrawStatsHeuristic: " + DrawStatsHeuristic);
        sb.AppendLine("    UseAspectRatioHeuristics: " + UseAspectRatioHeuristics);

        return sb.ToString().Trim();
    }
}