namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// SAP Business One style form modes used by legacy cockpit modules.
/// </summary>
public enum FormMode
{
    /// <summary>Query/filter mode – toolbar actions operate on search criteria.</summary>
    Find,

    /// <summary>Allows inserting a new record.</summary>
    Add,

    /// <summary>Read-only view of the active record.</summary>
    View,

    /// <summary>Editing existing record values.</summary>
    Update
}



