using System;
using System.IO;
using System.Windows;

namespace ReShadeInstaller;

public static class DllDeployer
{
    public static void Deploy(string gameDirectoryPath, string dllPath, string exePath, string api)
    {
        bool flag = GetMachineType(exePath) == MachineType.x64;

        if (api == "vulkan")
        {
            MessageBox.Show("If you haven't already, install ReShade for Vulkan globally through the normal ReShade installer. This program will setup the rest.");
            return;
        }

        string symlinkPath = Path.Combine(gameDirectoryPath, api + ".dll");
            
        if (File.Exists(symlinkPath))
            File.Delete(symlinkPath);
            
        SymbolicLink.CreateSymbolicLink(symlinkPath, Path.Combine(dllPath, flag ? "ReShade64.dll" : "ReShade32.dll"), 0);
    }
    
    public static MachineType GetMachineType(string fileName)
    {
        byte[] buffer = new byte[4096];
        using (Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            stream.Read(buffer, 0, 4096);
        int int32 = BitConverter.ToInt32(buffer, 60);
        return (MachineType)BitConverter.ToUInt16(buffer, int32 + 4);
    }

    public enum MachineType
    {
        Native = 0,
        I386 = 332, // 0x0000014C
        Itanium = 512, // 0x00000200
        x64 = 34404, // 0x00008664
    }
}