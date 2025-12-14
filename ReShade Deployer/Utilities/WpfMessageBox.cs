using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using Clipboard = System.Windows.Clipboard;
using TextBlock = System.Windows.Controls.TextBlock;
using Result = ReShadeDeployer.IMessageBox.Result;

namespace ReShadeDeployer;

public partial class WpfMessageBox : IMessageBox
{
    public void Show(string contentText, string title)
    {
        TextBlock content = new TextBlock {Text = contentText, TextWrapping = TextWrapping.Wrap};
        ConvertStringLinksToHyperlinks(content);
        
        var messageBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = title,
            Content = content,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.Height,
            Width = 360,
            MinHeight = 120
        };
        
        var footer = new StackPanel();
        footer.HorizontalAlignment = HorizontalAlignment.Right;
        var button = new Wpf.Ui.Controls.Button {
            Content = UIStrings.OK,
            Width = 120
        };
        button.Click += (_, _) => messageBox.Close();
        footer.Children.Add(button);
        messageBox.Footer = footer;

        messageBox.ShowDialog();
    }
    
    public Result Show(string contentText, string title, string primaryButtonText, string secondaryButtonText, ControlAppearance primaryButtonAppearance = ControlAppearance.Primary)
    {
        TextBlock content = new TextBlock {Text = contentText, TextWrapping = TextWrapping.Wrap};
        ConvertStringLinksToHyperlinks(content);
        
        var messageBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = title,
            Content = content,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.Height,
            ButtonLeftName = primaryButtonText,
            ButtonRightName = secondaryButtonText,
            ButtonLeftAppearance = primaryButtonAppearance,
            Width = 360,
            MinHeight = 120
        };
        messageBox.ButtonLeftClick += (_, _) =>
        {
            messageBox.DialogResult = true;
            messageBox.Close();
        };
        messageBox.ButtonRightClick += (_, _) =>
        {
            messageBox.DialogResult = false;
            messageBox.Close();
        };

        return messageBox.ShowDialog() switch
        {
            null => Result.Close,
            true => Result.Primary,
            false => Result.Secondary
        };
    }
    
    public void Show(Exception exception, string title, string primaryButtonText, ControlAppearance primaryButtonAppearance = ControlAppearance.Primary)
    {
        StackPanel contentStack = new StackPanel();
        
        string? message = exception.Message;
        if (string.IsNullOrEmpty(message))
            message = exception.InnerException?.Message;

        if (!string.IsNullOrEmpty(message))
        {
            contentStack.Children.Add(new TextBlock {Text = "Error Message", FontWeight = FontWeights.Bold});
            contentStack.Children.Add(new TextBlock {Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12)});
        }
        
        if (exception.StackTrace != null)
        {
            contentStack.Children.Add(new TextBlock {Text = "Callstack", FontWeight = FontWeights.Bold});
            contentStack.Children.Add(new ScrollViewer
            {
                Content = new TextBlock
                {
                    Text = exception.ToString(),
                    FontSize = 11
                },
                MaxHeight = 500,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            });
        }
        
        var messageBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = title,
            Content = contentStack,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.Height,
            Width = 600,
            MinHeight = 250
        };
        
        var footer = new DockPanel();
        var copyButton = new Wpf.Ui.Controls.Button {
            Content = UIStrings.Copy,
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        copyButton.Click += (_, _) => Clipboard.SetText(exception.ToString());
        DockPanel.SetDock(copyButton, Dock.Left);
        footer.Children.Add(copyButton);
        
        var exitButton = new Wpf.Ui.Controls.Button {
            Content = primaryButtonText,
            Width = 120,
            Appearance = primaryButtonAppearance,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        exitButton.Click += (_, _) => messageBox.Close();
        DockPanel.SetDock(exitButton, Dock.Right);
        footer.Children.Add(exitButton);
        messageBox.Footer = footer;

        messageBox.ShowDialog();
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
            Hyperlink link = new Hyperlink {NavigateUri = match.Value, Content = match.Value};
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