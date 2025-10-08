namespace YasGMP.Wpf.Automation;

/// <summary>
/// Commands understood by the WPF shell to support external smoke automation.
/// </summary>
public enum SmokeAutomationCommand
{
    /// <summary>Changes the active UI language (payload: language code).</summary>
    SetLanguage = 1,

    /// <summary>Resets the inspector pane back to its placeholder state.</summary>
    ResetInspector = 2,
}
