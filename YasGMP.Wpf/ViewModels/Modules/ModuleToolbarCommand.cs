using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Represents a SAP Business One style toolbar command bound to a module document.</summary>
/// <remarks>
/// Form Modes: Hosts the canonical Find/Add/View/Update/Save/Cancel/Refresh entries whose checked state mirrors the active form mode.
/// Audit & Logging: Enables modules to gate command execution (for example, requiring attachments or signatures before `Save`) so audit trails remain intact.
/// Localization: Caption strings are currently inline (e.g. `"Find"`, `"Add"`); future RESX keys should flow through the constructor.
/// Navigation: Instances live inside each module's toolbar collection and inherit context from the owning module `ModuleKey`, keeping shell navigation cues and Golden Arrow routing in sync with button state.
/// </remarks>
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



