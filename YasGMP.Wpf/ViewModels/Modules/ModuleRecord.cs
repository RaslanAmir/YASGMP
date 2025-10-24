using System;
using System.Collections.Generic;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Canonical representation of a module list record surfaced in the WPF shell.
/// </summary>
public sealed class ModuleRecord
{
    public ModuleRecord(
        string key,
        string title,
        string? code = null,
        string? status = null,
        string? description = null,
        IReadOnlyList<InspectorField>? inspectorFields = null,
        string? relatedModuleKey = null,
        object? relatedParameter = null)
    {
        Key = key;
        Title = title;
        Code = code;
        Status = status;
        Description = description;
        InspectorFields = inspectorFields ?? Array.Empty<InspectorField>();
        RelatedModuleKey = relatedModuleKey;
        RelatedParameter = relatedParameter;
    }

    /// <summary>Unique identifier for the record.</summary>
    public string Key { get; }

    /// <summary>Human readable title rendered in lists.</summary>
    public string Title { get; }

    /// <summary>Optional system code.</summary>
    public string? Code { get; }

    /// <summary>Optional workflow/status indicator.</summary>
    public string? Status { get; }

    /// <summary>Optional long-form description.</summary>
    public string? Description { get; }

    /// <summary>Inspector payload describing the record in more detail.</summary>
    public IReadOnlyList<InspectorField> InspectorFields { get; }

    /// <summary>Module key invoked by the golden arrow navigation.</summary>
    public string? RelatedModuleKey { get; }

    /// <summary>Optional payload forwarded to the related module when navigating.</summary>
    public object? RelatedParameter { get; }
}



