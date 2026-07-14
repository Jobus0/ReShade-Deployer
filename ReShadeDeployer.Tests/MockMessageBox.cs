using Wpf.Ui.Controls;

namespace ReShadeDeployer.Tests;

public class MockMessageBox : IMessageBox
{
    public List<(string Title, string ContentText, string PrimaryButtonTex, ControlAppearance PrimaryButtonAppearance, string SecondaryButtonText, ControlAppearance SecondaryButtonAppearance, string CloseButtonText)> ShowCalls { get; } = new();
    public List<(string Title, string ContentText, string PrimaryButtonText, string SecondaryButtonText, string DefaultTextBoxText)> ShowWithTextBoxCalls { get; } = new();
    public List<(string Title, Exception Exception, string PrimaryButtonText, ControlAppearance PrimaryButtonAppearance)> ShowExceptionCalls { get; } = new();

    public MessageBoxResult NextResult { get; set; } = MessageBoxResult.None;
    public string NextTextBox { get; set; } = string.Empty;

    public Task<MessageBoxResult> Show(string title, string contentText, string primaryButtonText = "", ControlAppearance primaryButtonAppearance = ControlAppearance.Primary, string secondaryButtonText = "", ControlAppearance secondaryButtonAppearance = ControlAppearance.Secondary, string closeButtonText = "OK")
    {
        ShowCalls.Add((title, contentText, primaryButtonText, primaryButtonAppearance, secondaryButtonText, secondaryButtonAppearance, closeButtonText));
        return Task.FromResult(NextResult);
    }

    public Task<(MessageBoxResult Result, string TextBoxText)> ShowWithTextBox(string title, string contentText, string primaryButtonText, string secondaryButtonText)
    {
        ShowWithTextBoxCalls.Add((title, contentText, primaryButtonText, secondaryButtonText));
        return Task.FromResult((NextResult, NextTextBox));
        ShowWithTextBoxCalls.Add((title, contentText, primaryButtonText, secondaryButtonText, defaultTextBoxText));
    }

    public Task<MessageBoxResult> ShowException(string title, Exception exception, string primaryButtonText, ControlAppearance primaryButtonAppearance = ControlAppearance.Secondary)
    {
        ShowExceptionCalls.Add((title, exception, primaryButtonText, primaryButtonAppearance));
        return Task.FromResult(NextResult);
    }
}
