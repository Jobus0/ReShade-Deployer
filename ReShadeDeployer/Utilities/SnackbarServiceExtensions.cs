using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ReShadeDeployer;

public static class SnackbarServiceExtensions
{
    public static void Show(this ISnackbarService snackbarService, string title, ControlAppearance appearance, SymbolRegular symbol)
    {
        IconElement? iconElement = null;

        if (snackbarService is SnackbarService)
            iconElement = new SymbolIcon(symbol, 20);
        
        snackbarService.Show(title, string.Empty, appearance, iconElement, snackbarService.DefaultTimeOut);
    }
    
    public static void Show(this ISnackbarService snackbarService, string title, ControlAppearance appearance)
    {
        snackbarService.Show(title, string.Empty, appearance, null, snackbarService.DefaultTimeOut);
    }
}