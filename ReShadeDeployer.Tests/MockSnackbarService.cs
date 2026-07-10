using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ReShadeDeployer.Tests;

public class MockSnackbarService : ISnackbarService
{
    public List<(string Title, string Message, ControlAppearance Appearance, IconElement? Icon, TimeSpan Duration)> ShowCalls { get; } = new();

    public TimeSpan DefaultTimeOut { get; set; } = TimeSpan.FromSeconds(3);

    public void Show(string title, string message, ControlAppearance appearance, IconElement? icon, TimeSpan duration)
    {
        ShowCalls.Add((title, message, appearance, icon, duration));
    }

    public void SetSnackbarPresenter(SnackbarPresenter snackbarPresenter)
    {
        // Mock implementation - no-op
    }

    public SnackbarPresenter? GetSnackbarPresenter()
    {
        return null;
    }
}
