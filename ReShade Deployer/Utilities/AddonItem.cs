using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ReShadeDeployer;

public class AddonItem : INotifyPropertyChanged
{
    public string Name { get; init; } = string.Empty;
    public string X32Path { get; set; } = string.Empty;
    public string X64Path { get; set; } = string.Empty;
    
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }
    
    private bool _isSupported = true;
    public bool IsSupported
    {
        get => _isSupported;
        set
        {
            if (_isSupported == value) return;
            _isSupported = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}