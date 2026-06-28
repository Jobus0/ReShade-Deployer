using Wpf.Ui.Common;

namespace ReShadeDeployer.Tests;

public class MockMessageBox : IMessageBox
{
    public List<(string Content, string Title)> ShowCalls { get; } = new();
    public List<(string Content, string Title, string PrimaryText, string SecondaryText, ControlAppearance Appearance)> ShowWithButtonsCalls { get; } = new();
    public List<(Exception Exception, string Title, string PrimaryText, ControlAppearance Appearance)> ShowExceptionCalls { get; } = new();

    public IMessageBox.Result NextResult { get; set; } = IMessageBox.Result.Close;

    public void Show(string contentText, string title)
    {
        ShowCalls.Add((contentText, title));
    }

    public IMessageBox.Result Show(string contentText, string title, string primaryButtonText, string secondaryButtonText, ControlAppearance primaryButtonAppearance = ControlAppearance.Primary)
    {
        ShowWithButtonsCalls.Add((contentText, title, primaryButtonText, secondaryButtonText, primaryButtonAppearance));
        return NextResult;
    }

    public List<(Func<object> ContentFactory, string Title, string PrimaryText, string SecondaryText, ControlAppearance Appearance)> ShowWithFuncCalls { get; } = new();

    public IMessageBox.Result Show(Func<object> contentFactory, string title, string primaryButtonText, string secondaryButtonText, ControlAppearance primaryButtonAppearance = ControlAppearance.Primary)
    {
        ShowWithFuncCalls.Add((contentFactory, title, primaryButtonText, secondaryButtonText, primaryButtonAppearance));
        return NextResult;
    }

    public void Show(Exception exception, string title, string primaryButtonText, ControlAppearance primaryButtonAppearance = ControlAppearance.Primary)
    {
        ShowExceptionCalls.Add((exception, title, primaryButtonText, primaryButtonAppearance));
    }
}
