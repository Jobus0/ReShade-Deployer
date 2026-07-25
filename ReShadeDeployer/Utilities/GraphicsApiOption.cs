using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReShadeDeployer;

public partial class GraphicsApiOption : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _tooltip = string.Empty;

    [ObservableProperty]
    private GraphicsApi _api;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isDetected;
    
    public Action<GraphicsApiOption>? SelectionChanged;

    public GraphicsApiOption(string name, string tooltip, GraphicsApi api, bool isSelected = false, bool isDetected = false)
    {
        Name = name;
        Tooltip = tooltip;
        Api = api;
        IsSelected = isSelected;
        IsDetected = isDetected;
    }

    partial void OnIsSelectedChanged(bool value)
    {
        if (value)
        {
            SelectionChanged?.Invoke(this);
        }
    }
}
