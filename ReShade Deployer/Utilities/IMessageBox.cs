using System;
using Wpf.Ui.Common;

namespace ReShadeDeployer;

public interface IMessageBox
{
    public enum Result { Close, Primary, Secondary }
    
    void Show(string contentText, string title);
    Result Show(string contentText, string title, string primaryButtonText, string secondaryButtonText, ControlAppearance primaryButtonAppearance = ControlAppearance.Primary);
    void Show(Exception exception, string title, string primaryButtonText, ControlAppearance primaryButtonAppearance = ControlAppearance.Primary);
}