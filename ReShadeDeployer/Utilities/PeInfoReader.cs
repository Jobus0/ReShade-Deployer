using ReShade.Setup.Utilities;

namespace ReShadeDeployer;

public class PeInfoReader : IPeInfoReader
{
    public PeInfoData Read(string executablePath)
    {
        var pe = new PEInfo(executablePath);
        return new PeInfoData(pe.Type, pe.Modules);
    }

    public string? ReadResourceString(string executablePath, ushort id)
    {
        return PEInfo.ReadResourceString(executablePath, id);
    }
}