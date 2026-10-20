using Testably.Abstractions.Testing;

namespace ReShadeDeployer.Tests;

public static class TestHelpers
{
    public static ExecutableContext CreateExecutableContext(
        string fileName = "game.exe",
        string directoryPath = @"C:\Games\MyGame",
        bool isX64 = true,
        bool isD3D8 = false,
        bool isD3D9 = false,
        bool isDXGI = false,
        bool isOpenGL = false,
        bool isVulkan = false,
        string depthReversed = "0",
        string depthUpsideDown = "0",
        string depthLogarithmic = "0",
        string depthCopyBeforeClears = "0",
        string depthCopyAtClearIndex = "0",
        string drawStatsHeuristic = "0",
        string useAspectRatioHeuristics = "1",
        bool banned = false)
    {
        return new ExecutableContext(
            FileName: fileName,
            DirectoryPath: directoryPath,
            IsX64: isX64,
            IsD3D8: isD3D8,
            IsD3D9: isD3D9,
            IsDXGI: isDXGI,
            IsOpenGL: isOpenGL,
            IsVulkan: isVulkan,
            DepthReversed: depthReversed,
            DepthUpsideDown: depthUpsideDown,
            DepthLogarithmic: depthLogarithmic,
            DepthCopyBeforeClears: depthCopyBeforeClears,
            DepthCopyAtClearIndex: depthCopyAtClearIndex,
            DrawStatsHeuristic: drawStatsHeuristic,
            UseAspectRatioHeuristics: useAspectRatioHeuristics,
            Banned: banned
        );
    }

    public static MockFileSystem CreateMockFileSystem(string directoryPath)
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory(directoryPath);
        fileSystem.Directory.CreateDirectory(AppContext.BaseDirectory);
        return fileSystem;
    }

    /// <summary>
    /// Writes a mock ReShade DLL with the given file version and description.
    /// </summary>
    public static string CreateMockReShadeDll(MockFileSystem fileSystem, string directory, string fileName, string fileVersion,
        string fileDescription = "crosire's ReShade post-processing injector")
    {
        string path = fileSystem.Path.Combine(directory, fileName);
        fileSystem.Directory.CreateDirectory(directory);
        fileSystem.File.WriteAllText(path, "mock dll content");
        fileSystem.WithFileVersionInfo(path, v => v.SetFileVersion(fileVersion).SetFileDescription(fileDescription));
        return path;
    }

    /// <summary>
    /// Writes all four mock ReShade DLLs (32/64-bit, with and without add-on support) with the given file version.
    /// </summary>
    public static void CreateMockReShadeInstallation(MockFileSystem fileSystem, string fileVersion)
    {
        CreateMockReShadeDll(fileSystem, Paths.Dlls, "ReShade64.dll", fileVersion);
        CreateMockReShadeDll(fileSystem, Paths.Dlls, "ReShade32.dll", fileVersion);
        CreateMockReShadeDll(fileSystem, Paths.AddonDlls, "ReShade64.dll", fileVersion);
        CreateMockReShadeDll(fileSystem, Paths.AddonDlls, "ReShade32.dll", fileVersion);
    }
}
