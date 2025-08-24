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
            null => IMessageBox.Result.Close,
            true => IMessageBox.Result.Primary,
            false => IMessageBox.Result.Secondary
        };
    }
    
    public void Show(Exception exception)
    {
        StackPanel contentStack = new StackPanel();
        contentStack.Children.Add(new TextBlock {Text = "Exception Message", FontWeight = FontWeights.Bold});
        contentStack.Children.Add(new TextBlock {Text = exception.Message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12)});
        
        contentStack.Children.Add(new TextBlock {Text = "Exception Type", FontWeight = FontWeights.Bold});
        contentStack.Children.Add(new TextBlock {Text = exception.GetType().ToString(), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12)});
        
        if (exception.StackTrace != null)
        {
            contentStack.Children.Add(new TextBlock {Text = "Callstack", FontWeight = FontWeights.Bold});
            contentStack.Children.Add(new ScrollViewer {Content = new TextBlock {Text = exception.StackTrace, FontSize = 12}, MaxHeight = 300, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto});
        }
        
        var messageBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = "Unhandled Exception",
            Content = contentStack,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.Height,
            Width = 500,
            MinHeight = 200
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
            Content = UIStrings.Exit,
            Width = 120,
            Appearance = ControlAppearance.Danger,
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