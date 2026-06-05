using System.Collections.Generic;
using ReShade.Setup.Utilities;

namespace ReShadeDeployer;

public interface IPeInfoReader
{
    PeInfoData Read(string executablePath);
    string? ReadResourceString(string executablePath, ushort id);
}

public class PeInfoData(PEInfo.BinaryType type, IEnumerable<string> modules)
{
    public PEInfo.BinaryType Type { get; } = type;
    public IEnumerable<string> Modules { get; } = modules;
}