using System.Windows;
using System.Windows.Controls;

namespace ReShadeDeployer;

using System;
using Wpf.Ui.Controls;

public class ExceptionMessageBox : MessageBox
{
    private readonly string _exceptionDetails;

    public ExceptionMessageBox(Exception exception)
    {
        SetResourceReference(StyleProperty, typeof(MessageBox));
        
        _exceptionDetails = exception.ToString();
        
        Title = "An Error Occurred";
        PrimaryButtonText = "Copy Error";
        CloseButtonText = "Close";
        
        StackPanel contentStack = new StackPanel();
        
        string? message = exception.Message;
        if (string.IsNullOrEmpty(message))
            message = exception.InnerException?.Message;

        if (!string.IsNullOrEmpty(message))
        {
            contentStack.Children.Add(new System.Windows.Controls.TextBlock {Text = "Error Message", FontWeight = FontWeights.Bold});
            contentStack.Children.Add(new System.Windows.Controls.TextBlock {Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12)});
        }
        
        if (exception.StackTrace != null)
        {
            contentStack.Children.Add(new System.Windows.Controls.TextBlock {Text = "Callstack", FontWeight = FontWeights.Bold});
            contentStack.Children.Add(new ScrollViewer
            {
                Content = new System.Windows.Controls.TextBlock
                {
                    Text = _exceptionDetails,
                    FontSize = 11
                },
                MaxHeight = 500,
                Padding = new Thickness(0, 0, 0, 12),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            });
        }
        
        Content = contentStack;
    }

    protected override void OnButtonClick(MessageBoxButton button)
    {
        if (button == MessageBoxButton.Primary)
        {
            try
            {
                Clipboard.SetText(_exceptionDetails);
                
                // Optional: Change the button text temporarily to show it worked
                PrimaryButtonText = "Copied!";
            }
            catch (Exception)
            {
                PrimaryButtonText = "Failed to copy";
            }

            // Return early! Bypassing base.OnButtonClick prevents Tcs from setting a result 
            // and blocks the base.Close() call, keeping the window alive.
            return; 
        }

        // Let the Close or Secondary buttons behave normally
        base.OnButtonClick(button);
    }
}