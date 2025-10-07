using System.Collections.Generic;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Payload describing the currently selected record for the inspector pane.
/// </summary>
public sealed class InspectorContext
{
    /// <summary>
    /// Initializes a new instance of the InspectorContext class.
    /// </summary>
    public InspectorContext(string moduleTitle, string recordTitle, IReadOnlyList<InspectorField> fields)
    {
        ModuleTitle = moduleTitle;
        RecordTitle = recordTitle;
        Fields = fields;
    }
    /// <summary>
    /// Gets or sets the module title.
    /// </summary>

    public string ModuleTitle { get; }
    /// <summary>
    /// Gets or sets the record title.
    /// </summary>

    public string RecordTitle { get; }
    /// <summary>
    /// Gets or sets the fields.
    /// </summary>

    public IReadOnlyList<InspectorField> Fields { get; }
}

/// <summary>Single key/value row rendered inside the inspector pane.</summary>
public sealed class InspectorField
{
    /// <summary>
    /// Initializes a new instance of the InspectorField class.
    /// </summary>
    public InspectorField(string label, string? value)
    {
        Label = label;
        Value = value ?? string.Empty;
    }
    /// <summary>
    /// Gets or sets the label.
    /// </summary>

    public string Label { get; }
    /// <summary>
    /// Gets or sets the value.
    /// </summary>

    public string Value { get; }
}
