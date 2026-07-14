using System;
using System.Threading.Tasks;
using Wpf.Ui.Controls;

namespace ReShadeDeployer;

public interface IMessageBox
{
    Task<MessageBoxResult> Show(string title, string contentText, string primaryButtonText = "", ControlAppearance primaryButtonAppearance = ControlAppearance.Primary, string secondaryButtonText = "", ControlAppearance secondaryButtonAppearance = ControlAppearance.Secondary, string closeButtonText = "OK");
    Task<(MessageBoxResult Result, string TextBoxText)> ShowWithTextBox(string title, string contentText, string primaryButtonText, string secondaryButtonText, string defaultTextBoxText = "");
    Task<MessageBoxResult> ShowException(string title, Exception exception, string buttonText, ControlAppearance buttonAppearance = ControlAppearance.Secondary);
}