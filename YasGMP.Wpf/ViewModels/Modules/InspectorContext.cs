using System.Collections.Generic;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Payload describing the currently selected record for the inspector pane.
/// </summary>
public sealed class InspectorContext
{
    public InspectorContext(string moduleTitle, string recordTitle, IReadOnlyList<InspectorField> fields)
    {
        ModuleTitle = moduleTitle;
        RecordTitle = recordTitle;
        Fields = fields;
    }

    public string ModuleTitle { get; }

    public string RecordTitle { get; }

    public IReadOnlyList<InspectorField> Fields { get; }
}

/// <summary>Single key/value row rendered inside the inspector pane.</summary>
public sealed class InspectorField
{
    public InspectorField(string label, string? value)
    {
        Label = label;
        Value = value ?? string.Empty;
    }

    public string Label { get; }

    public string Value { get; }
}



