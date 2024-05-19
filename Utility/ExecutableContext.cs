using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ReShade.Setup.Utilities;

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

    private IEnumerable<string> modules;

    public ExecutableContext(string executablePath)
    {
        var pe = new PEInfo(executablePath);
        modules = pe.Modules;
        
        // In case this is the bootstrap executable of an Unreal Engine game, try and find the actual game executable for it
        string targetPathUnrealEngine = PEInfo.ReadResourceString(executablePath, 201); // IDI_EXEC_FILE (see BootstrapPackagedGame.cpp in Unreal Engine source code)
        if (!string.IsNullOrEmpty(targetPathUnrealEngine) && File.Exists(targetPathUnrealEngine = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(executablePath)!, targetPathUnrealEngine))))
        {
            FileName = Path.GetFileNameWithoutExtension(targetPathUnrealEngine);
            DirectoryPath = Path.GetDirectoryName(targetPathUnrealEngine)!;
        }
        else
        {
            FileName = Path.GetFileNameWithoutExtension(executablePath);
            DirectoryPath = Path.GetDirectoryName(executablePath)!;
        }
        
        foreach (var module in modules)
        {
            bool HasModule(string name) => module?.StartsWith(name, StringComparison.OrdinalIgnoreCase) ?? false;
            
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
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append("File: ");
        sb.Append(FileName);
        sb.AppendLine(IsX64 ? " (x64)" : " (x86)");
        
        sb.Append("Directory: ");
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

        sb.AppendLine("Modules");
        foreach (var module in modules)
        {
            sb.Append("    ");
            sb.AppendLine(module);
        }

        return sb.ToString().Trim();
    }
}