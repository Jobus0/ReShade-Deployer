using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ReShadeDeployer;

public static class SnackbarServiceExtensions
{
    public static void Show(this ISnackbarService snackbarService, string title, ControlAppearance appearance, SymbolRegular symbol)
    {
        snackbarService.Show(title, string.Empty, appearance, new SymbolIcon(symbol, 20), snackbarService.DefaultTimeOut);
    }
    
    public static void Show(this ISnackbarService snackbarService, string title, ControlAppearance appearance)
    {
        snackbarService.Show(title, string.Empty, appearance, null, snackbarService.DefaultTimeOut);
    }
}