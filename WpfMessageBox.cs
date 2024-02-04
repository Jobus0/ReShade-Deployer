using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Hyperlink = Wpf.Ui.Controls.Hyperlink;

namespace ReShadeDeployer;

public static partial class WpfMessageBox
{
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
            MinWidth = 320,
            MaxWidth = 480,
            MinHeight = 160
        };
        
        var footer = new StackPanel();
        footer.HorizontalAlignment = HorizontalAlignment.Right;
        var button = new Wpf.Ui.Controls.Button {
            Content = UIStrings.OK,
            Width = 120
        };
        button.Click += (o, args) => messageBox.Close();
        footer.Children.Add(button);
        messageBox.Footer = footer;
        
        messageBox.ShowDialog();
    }

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