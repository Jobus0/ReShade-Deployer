using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;
using MessageBoxButton = Wpf.Ui.Controls.MessageBoxButton;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;
using TextBlock = System.Windows.Controls.TextBlock;
using TextBox = System.Windows.Controls.TextBox;

namespace ReShadeDeployer;

public partial class WpfMessageBox : IMessageBox
{
    private Wpf.Ui.Controls.MessageBox Build(string title, object content, string primaryButtonText = "", ControlAppearance primaryButtonAppearance = ControlAppearance.Primary, string secondaryButtonText = "", ControlAppearance secondaryButtonAppearance = ControlAppearance.Secondary, string closeButtonText = "OK")
    {
        var messageBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = title,
            Content = content,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight,
            MinWidth = 360,
            MaxWidth = 500,
            MinHeight = 120,
            PrimaryButtonText = primaryButtonText,
            SecondaryButtonText = secondaryButtonText,
            PrimaryButtonAppearance = primaryButtonAppearance,
            SecondaryButtonAppearance = secondaryButtonAppearance,
            CloseButtonText = closeButtonText
        };
        
        if (string.IsNullOrEmpty(closeButtonText))
            messageBox.IsCloseButtonEnabled = false;
        
        messageBox.Loaded += (s, e) =>
        {
            messageBox.SizeToContent = SizeToContent.Manual;
            messageBox.SizeToContent = SizeToContent.Height;
        };

        return messageBox;
    }
    
    public Task<MessageBoxResult> Show(string title, string contentText, string primaryButtonText = "", ControlAppearance primaryButtonAppearance = ControlAppearance.Primary, string secondaryButtonText = "", ControlAppearance secondaryButtonAppearance = ControlAppearance.Secondary, string closeButtonText = "OK")
    {
        TextBlock content = new TextBlock {Text = contentText, TextWrapping = TextWrapping.Wrap};
        ConvertStringLinksToHyperlinks(content);

        var messageBox = Build(title, content, primaryButtonText, primaryButtonAppearance, secondaryButtonText, secondaryButtonAppearance, closeButtonText);
        
        return messageBox.ShowDialogAsync();
    }
    
    public async Task<(MessageBoxResult Result, string TextBoxText)> ShowWithTextBox(string title, string contentText, string primaryButtonText, string closeButtonText = "OK", string defaultTextBoxText = "")
    {
        TextBlock content = new TextBlock {Text = contentText, TextWrapping = TextWrapping.Wrap};
        ConvertStringLinksToHyperlinks(content);
        
        TextBox inputTextBox = new TextBox
        {
            Margin = new Thickness(0, 8, 0, 0),
            Padding = new Thickness(5),
            FontSize = 14,
            Text = defaultTextBoxText
        };

        StackPanel panel = new StackPanel();
        panel.Children.Add(content);
        panel.Children.Add(inputTextBox);
        
        var messageBox = Build(title, panel, primaryButtonText, closeButtonText: closeButtonText);
        
        inputTextBox.Loaded += (s, e) =>
        {
            inputTextBox.Focus();
            if (!string.IsNullOrEmpty(defaultTextBoxText))
            {
                inputTextBox.SelectAll();
            }
        };
        inputTextBox.KeyUp += (s, e) =>
        {
            if (e.Key == Key.Return)
                if (messageBox.TemplateButtonCommand.CanExecute(MessageBoxButton.Primary))
                    messageBox.TemplateButtonCommand.Execute(MessageBoxButton.Primary);
        };

        var result = await messageBox.ShowDialogAsync();

        return (result, inputTextBox.Text);
    }
    
    public Task<MessageBoxResult> ShowException(string title, Exception exception, string buttonText, ControlAppearance buttonAppearance = ControlAppearance.Secondary)
    {
        var messageBox = new ExceptionMessageBox(exception)
        {
            Title = title,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.Height,
            MinWidth = 360,
            Width = 600,
            MinHeight = 250,
            SecondaryButtonText = buttonText,
            SecondaryButtonAppearance = buttonAppearance,
            IsCloseButtonEnabled = false
        };

        return messageBox.ShowDialogAsync();
    }

    /// <summary>
    /// Convert any string http links in a TextBlock to clickable hyperlinks.
    /// </summary>
    private void ConvertStringLinksToHyperlinks(TextBlock textBlock)
    {
        string text = textBlock.Text;
        textBlock.Inlines.Clear();

        Regex urlRegex = UrlRegex();

        int lastPos = 0;
        foreach (Match match in urlRegex.Matches(text))
        {
            // Add the text up to the link
            if (match.Index != lastPos)
                textBlock.Inlines.Add(text.Substring(lastPos, match.Index - lastPos));

            // Add the hyperlink
            HyperlinkButton link = new HyperlinkButton
            {
                Content = match.Value,
                NavigateUri = match.Value
            };
            textBlock.Inlines.Add(link);

            lastPos = match.Index + match.Length;
        }

        // Add any text after the last link
        if (lastPos < text.Length)
            textBlock.Inlines.Add(text.Substring(lastPos));
    }

    [GeneratedRegex("http(s)?://([\\w-]+\\.)+[\\w-]+(/[\\w- ./?%&=]*)?")]
    private static partial Regex UrlRegex();
}