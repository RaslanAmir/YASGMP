using CommunityToolkit.Mvvm.ComponentModel;

namespace YasGMP.Wpf.ViewModels;

/// <summary>Status bar view-model displayed along the bottom edge of the shell.</summary>
public partial class ShellStatusBarViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _activeModule = string.Empty;
}
