using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ReShadeDeployer;

[ExcludeFromCodeCoverage]
public class IconExtractor : IIconExtractor
{
    public ImageSource? Extract(string filePath)
    {
        try
        {
            // Pull the icon from the .exe
            using Icon? icon = Icon.ExtractAssociatedIcon(filePath);
            
            if (icon == null)
                return null;

            // Convert it to a WPF-friendly ImageSource
            ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            // Freeze it so it can be safely used across threads
            imageSource.Freeze();

            return imageSource;
        }
        catch
        {
            return null;
        }
    }
}