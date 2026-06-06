using System.Collections.Generic;
using ReShade.Setup.Utilities;

namespace ReShadeDeployer;

public interface IPeInfoReader
{
    PeInfoData Read(string executablePath);
    string? ReadResourceString(string executablePath, ushort id);
}

public class PeInfoData(PeInfo.BinaryType type, IEnumerable<string> modules)
{
    public PeInfo.BinaryType Type { get; } = type;
    public IEnumerable<string> Modules { get; } = modules;
}