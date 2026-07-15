using Wpf.Ui.Controls;

namespace ReShadeDeployer.Tests;

public class MockMessageBox : IMessageBox
{
    public List<(string Title, string ContentText, string PrimaryButtonTex, ControlAppearance PrimaryButtonAppearance, string SecondaryButtonText, ControlAppearance SecondaryButtonAppearance, string CloseButtonText)> ShowCalls { get; } = new();
    public List<(string Title, string ContentText, string PrimaryButtonText, string SecondaryButtonText, string DefaultTextBoxText)> ShowWithTextBoxCalls { get; } = new();
    public List<(string Title, Exception Exception, string PrimaryButtonText, ControlAppearance PrimaryButtonAppearance)> ShowExceptionCalls { get; } = new();

    public Queue<MessageBoxResult> NextResults { get; } = new();
    public string NextTextBox { get; set; } = string.Empty;

    private MessageBoxResult GetNextResult()
    {
        if (NextResults.Count > 0)
            return NextResults.Dequeue();
        
        return MessageBoxResult.None;
    }

    public Task<MessageBoxResult> Show(string title, string contentText, string primaryButtonText = "", ControlAppearance primaryButtonAppearance = ControlAppearance.Primary, string secondaryButtonText = "", ControlAppearance secondaryButtonAppearance = ControlAppearance.Secondary, string closeButtonText = "OK")
    {
        ShowCalls.Add((title, contentText, primaryButtonText, primaryButtonAppearance, secondaryButtonText, secondaryButtonAppearance, closeButtonText));
        return Task.FromResult(GetNextResult());
    }

    public Task<(MessageBoxResult Result, string TextBoxText)> ShowWithTextBox(string title, string contentText, string primaryButtonText, string secondaryButtonText, string defaultTextBoxText = "")
    {
        ShowWithTextBoxCalls.Add((title, contentText, primaryButtonText, secondaryButtonText, defaultTextBoxText));
        return Task.FromResult((GetNextResult(), NextTextBox));
    }

    public Task<MessageBoxResult> ShowException(string title, Exception exception, string primaryButtonText, ControlAppearance primaryButtonAppearance = ControlAppearance.Secondary)
    {
        ShowExceptionCalls.Add((title, exception, primaryButtonText, primaryButtonAppearance));
        return Task.FromResult(GetNextResult());
    }
}
