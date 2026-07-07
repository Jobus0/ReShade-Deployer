using System.Windows.Media;

namespace ReShadeDeployer;

public interface IIconExtractor
{
    ImageSource? Extract(string filePath);
}