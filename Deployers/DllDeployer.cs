using System;
using System.IO;
using System.Windows;

namespace ReShadeDeployer;

public static class DllDeployer
{
    /// <summary>
    /// Deploy ReShade DLL to a game directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory to put the DLL symlink in.</param>
    /// <param name="executablePath">Path of the game executable.</param>
    /// <param name="dllPath">Path of the local downloaded ReShade DLL.</param>
    /// <param name="api">Target API name (dxgi, d3d9, opengl32, vulkan).</param>
    public static void Deploy(string directoryPath, string executablePath, string dllPath, string api)
    {
        bool flag = GetMachineType(executablePath) == MachineType.x64;

        if (api == "vulkan")
        {
            WpfMessageBox.Show(UIStrings.Vulkan_Info, UIStrings.Notice);
            return;
        }

        string symlinkPath = Path.Combine(directoryPath, api + ".dll");
            
        if (File.Exists(symlinkPath))
            File.Delete(symlinkPath);
            
        SymbolicLink.CreateSymbolicLink(symlinkPath, Path.Combine(dllPath, flag ? "ReShade64.dll" : "ReShade32.dll"), 0);
    }

    private static MachineType GetMachineType(string fileName)
    {
        byte[] buffer = new byte[4096];
        using (Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            stream.Read(buffer, 0, 4096);
        int int32 = BitConverter.ToInt32(buffer, 60);
        return (MachineType)BitConverter.ToUInt16(buffer, int32 + 4);
    }

    private enum MachineType
    {
        Native = 0,
        I386 = 332, // 0x0000014C
        Itanium = 512, // 0x00000200
        x64 = 34404, // 0x00008664
    }
}