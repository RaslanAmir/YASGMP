using System;
using System.Collections.Generic;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Describes a module record as it appears in the WPF shell, including the routing
/// metadata that drives SAP Business One inspired navigation and visuals.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Key"/>, <see cref="Status"/>, and <see cref="InspectorFields"/> are consumed by the
/// shell to mirror SAP B1 behaviors: the key doubles as the automation identifier and the
/// navigation payload that Golden Arrow commands push into the target module; status values
/// colorize form-mode ribbons and toolbar enablement to match the active B1 form mode; and
/// inspector metadata feeds the right-hand pane so users can inspect the record before
/// committing form-mode transitions.
/// </para>
/// <para>
/// Shell navigation, toolbar toggles, and form-mode visuals therefore stay synchronized with
/// SAP B1 expectations—Find/Add/View/Update modes drive ribbon command enablement while the
/// inspector snapshot provides the same context B1 surfaces in its summary panes.
/// </para>
/// </remarks>
public sealed class ModuleRecord
{
    /// <summary>
    /// Initializes a module record that the module tree, inspector, and Golden Arrow
    /// navigation use to render SAP B1 style interactions.
    /// </summary>
    /// <param name="key">
    /// Localization-aware identifier used for automation IDs, module tree bindings, and the
    /// navigation payload consumed by Golden Arrow targets.
    /// </param>
    /// <param name="title">
    /// Display caption resolved from localization resources and shown in module lists.
    /// </param>
    /// <param name="code">
    /// Optional system code or secondary identifier surfaced in inspector metadata and
    /// automation traces.
    /// </param>
    /// <param name="status">
    /// Optional status/phase token that maps to SAP B1 form-mode visuals and toolbar
    /// enablement (e.g., toggling Update versus OK commands).
    /// </param>
    /// <param name="description">
    /// Optional long-form text used to prime inspector panes and tooltips.
    /// </param>
    /// <param name="inspectorFields">
    /// Inspector metadata collection that pre-populates the right-hand pane and feeds
    /// automation identifiers for SAP B1 style summary surfaces.
    /// </param>
    /// <param name="relatedModuleKey">
    /// Module key invoked when Golden Arrow navigation activates, routed through the module
    /// registry using localization-backed identifiers.
    /// </param>
    /// <param name="relatedParameter">
    /// Optional payload forwarded with navigation to seed the target module's Find/Add state
    /// (mirroring SAP B1 choose-from-list behaviors).
    /// </param>
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
