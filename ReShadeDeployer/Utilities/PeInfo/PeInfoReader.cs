using ReShade.Setup.Utilities;

namespace ReShadeDeployer;

public class PeInfoReader : IPeInfoReader
{
    public PeInfoData Read(string executablePath)
    {
        var pe = new PeInfo(executablePath);
        return new PeInfoData(pe.Type, pe.Modules);
    }

    public string? ReadResourceString(string executablePath, ushort id)
    {
        return PeInfo.ReadResourceString(executablePath, id);
    }
}