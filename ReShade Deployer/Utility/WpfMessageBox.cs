using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using TextBlock = System.Windows.Controls.TextBlock;

namespace ReShadeDeployer;

public static partial class WpfMessageBox
{
    public enum Result { Close, Primary, Secondary }
    
    public static void Show(string contentText, string title)
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
    
    public static Result Show(string contentText, string title, string primaryButtonText, string secondaryButtonText, ControlAppearance primaryButtonAppearance = ControlAppearance.Primary)
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

    /// <summary>
    /// Convert any string http links in a TextBlock to clickable hyperlinks.
    /// </summary>
    private static void ConvertStringLinksToHyperlinks(TextBlock textBlock)
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