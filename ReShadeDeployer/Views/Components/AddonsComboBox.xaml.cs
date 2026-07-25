using System.Windows;
using System.Windows.Controls;
using ReShadeDeployer.ViewModels;

namespace ReShadeDeployer.Views.Components;

public partial class AddonsComboBox : UserControl
{
    public AddonsComboBox()
    {
        InitializeComponent();
    }
    
    private void AddonItem_OnUserClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AddonItem item } && DataContext is MainViewModel viewModel)
            viewModel.Addons.OnAddonItemClicked(item);

        e.Handled = true;
    }
}