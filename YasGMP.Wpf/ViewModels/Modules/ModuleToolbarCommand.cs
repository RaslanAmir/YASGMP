using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Represents a toolbar command in the SAP B1 style command strip.
/// </summary>
public partial class ModuleToolbarCommand : ObservableObject
{
    public ModuleToolbarCommand(string caption, ICommand command)
    {
        Caption = caption;
        Command = command;
    }

    /// <summary>Display text rendered in the toolbar.</summary>
    public string Caption { get; }

    /// <summary>Gets the command executed when the toolbar button is clicked.</summary>
    public ICommand Command { get; }

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _isChecked;
}
