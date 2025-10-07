using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Represents a SAP Business One style toolbar command that caches localization resource keys so the shell can refresh the caption, tooltip, automation name, and automation ID bindings whenever an <c>ILocalizationService.LanguageChanged</c> notification occurs, keeping the Fluent.Ribbon surface and FlaUI smoke harness synchronized across language switches.
/// </summary>
/// <remarks>
/// SAP Business One form-mode semantics dictate when toolbar buttons enable, disable, or latch. The optional <c>AssociatedMode</c> flag captures that mapping so Find/Add/View/Update toggles behave exactly like their SAP B1 counterparts. The <c>automationIdKey</c> feeds the Golden Arrow navigation registry, which combines the key with the module context to generate deterministic UI Automation identifiers for cross-module jumps and smoke-test lookups.
/// </remarks>
public partial class ModuleToolbarCommand : ObservableObject
{
    /// <summary>
    /// Creates a toolbar command that caches localization resource keys and optional form-mode affinity so the shell can keep ribbon bindings and automation identifiers aligned with SAP B1 semantics.
    /// </summary>
    /// <param name="captionKey">Localization resource key (or fallback literal) used to resolve the ribbon caption that Fluent.Ribbon renders.</param>
    /// <param name="command">Executable invoked when the toolbar item is activated; also used by the FormMode engine to determine enablement.</param>
    /// <param name="localization">Optional localization service instance expected to implement <c>ILocalizationService</c>; the shell subscribes to its <c>LanguageChanged</c> event to repopulate caption and automation bindings.</param>
    /// <param name="toolTipKey">Resource key for the tooltip text that surfaces in the ribbon and status bar when the button is focused.</param>
    /// <param name="automationNameKey">Resource key providing the accessibility name consumed by screen readers and FlaUI lookups.</param>
    /// <param name="automationIdKey">Resource key or suffix used to produce a stable UI Automation identifier for Golden Arrow navigation and smoke automation.</param>
    /// <param name="associatedMode">SAP Business One <see cref="FormMode"/> that this command activates, allowing the toolbar state machine to mirror Find/Add/View/Update wiring.</param>
    public ModuleToolbarCommand(
        string captionKey,
        ICommand command,
        object? localization = null,
        string? toolTipKey = null,
        string? automationNameKey = null,
        string? automationIdKey = null,
        FormMode? associatedMode = null)
    {
        Caption = captionKey;
        Command = command;
        CaptionKey = captionKey;
        LocalizationContext = localization;
        ToolTipKey = toolTipKey;
        AutomationNameKey = automationNameKey ?? captionKey;
        AutomationIdKey = automationIdKey ?? AutomationNameKey;
        AssociatedMode = associatedMode;
    }

    /// <summary>Display text rendered in the toolbar.</summary>
    public string Caption { get; }

    /// <summary>Gets the command executed when the toolbar button is clicked.</summary>
    public ICommand Command { get; }

    /// <summary>Resource key backing the <see cref="Caption"/> value so localization updates can rehydrate the binding.</summary>
    public string CaptionKey { get; }

    /// <summary>Localization service reference stored for tooling; expected to be an <c>ILocalizationService</c> instance.</summary>
    public object? LocalizationContext { get; }

    /// <summary>Resource key used to resolve the tooltip displayed for this command.</summary>
    public string? ToolTipKey { get; }

    /// <summary>Resource key used to produce the automation name consumed by accessibility clients and FlaUI smoke tests.</summary>
    public string AutomationNameKey { get; }

    /// <summary>Resource key or suffix used to generate a deterministic automation identifier for Golden Arrow navigation.</summary>
    public string AutomationIdKey { get; }

    /// <summary>Optional SAP Business One form mode associated with this command.</summary>
    public FormMode? AssociatedMode { get; }

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _isChecked;
}



