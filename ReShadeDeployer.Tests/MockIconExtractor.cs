using System.Windows.Media;

namespace ReShadeDeployer.Tests;

public class MockIconExtractor : IIconExtractor
{
    public ImageSource? Extract(string filePath)
    {
        return null;
    }
}