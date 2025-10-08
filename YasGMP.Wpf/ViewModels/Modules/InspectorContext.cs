using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Payload describing the currently selected record for the inspector pane.
/// </summary>
public sealed class InspectorContext
{
    /// <summary>
    /// Initializes a new instance of the InspectorContext class.
    /// </summary>
    public InspectorContext(
        string moduleKey,
        string moduleTitle,
        string? recordKey,
        string recordTitle,
        IReadOnlyList<InspectorField> fields)
    {
        ModuleKey = moduleKey;
        ModuleTitle = moduleTitle;
        RecordKey = recordKey ?? string.Empty;
        RecordTitle = recordTitle;
        Fields = NormalizeFields(moduleKey, moduleTitle, recordKey, recordTitle, fields);
    }

    /// <summary>Gets the module key.</summary>
    public string ModuleKey { get; }

    /// <summary>Gets the module title.</summary>
    public string ModuleTitle { get; }

    /// <summary>Gets the record key.</summary>
    public string RecordKey { get; }

    /// <summary>Gets the record title.</summary>
    public string RecordTitle { get; }

    /// <summary>Gets the inspector fields associated with the context.</summary>
    public IReadOnlyList<InspectorField> Fields { get; }

    private static IReadOnlyList<InspectorField> NormalizeFields(
        string moduleKey,
        string moduleTitle,
        string? recordKey,
        string recordTitle,
        IReadOnlyList<InspectorField> fields)
    {
        if (fields is null || fields.Count == 0)
        {
            return Array.Empty<InspectorField>();
        }

        var normalized = new List<InspectorField>(fields.Count);
        foreach (var field in fields)
        {
            if (field is null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(field.AutomationId))
            {
                normalized.Add(InspectorField.Create(moduleKey, moduleTitle, recordKey, recordTitle, field.Label, field.Value));
            }
            else
            {
                normalized.Add(field);
            }
        }

        return normalized;
    }
}

/// <summary>Single key/value row rendered inside the inspector pane.</summary>
public sealed class InspectorField
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InspectorField"/> class.
    /// </summary>
    public InspectorField(string label, string? value)
        : this(label, value, string.Empty, string.Empty, string.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InspectorField"/> class with explicit automation metadata.
    /// </summary>
    public InspectorField(string label, string? value, string automationName, string automationId, string automationTooltip)
    {
        Label = label;
        Value = value ?? string.Empty;
        AutomationName = automationName ?? string.Empty;
        AutomationId = automationId ?? string.Empty;
        AutomationTooltip = automationTooltip ?? string.Empty;
    }

    /// <summary>Gets the label shown inside the inspector grid.</summary>
    public string Label { get; }

    /// <summary>Gets the formatted value rendered next to <see cref="Label"/>.</summary>
    public string Value { get; }

    /// <summary>Gets the automation name for accessibility tooling.</summary>
    public string AutomationName { get; }

    /// <summary>Gets the automation identifier for UI automation consumers.</summary>
    public string AutomationId { get; }

    /// <summary>Gets the automation tooltip exposed to assistive technologies.</summary>
    public string AutomationTooltip { get; }

    /// <summary>
    /// Creates an <see cref="InspectorField"/> using contextual metadata from the module and record.
    /// </summary>
    /// <param name="moduleKey">Stable module key registered inside the shell.</param>
    /// <param name="moduleTitle">Localized module title shown in the UI.</param>
    /// <param name="recordKey">Unique record key (falls back to <paramref name="recordTitle"/> when empty).</param>
    /// <param name="recordTitle">Display title describing the record the inspector row belongs to.</param>
    /// <param name="label">Inspector label.</param>
    /// <param name="value">Inspector value.</param>
    /// <returns>The populated <see cref="InspectorField"/>.</returns>
    public static InspectorField Create(
        string moduleKey,
        string moduleTitle,
        string? recordKey,
        string? recordTitle,
        string label,
        string? value)
    {
        var moduleToken = NormalizeAutomationToken(moduleKey, "Module");
        var recordToken = NormalizeAutomationToken(recordKey, "Record");
        var labelToken = NormalizeAutomationToken(label, "Field");

        var displayModule = string.IsNullOrWhiteSpace(moduleTitle) ? moduleKey : moduleTitle;
        var displayRecord = string.IsNullOrWhiteSpace(recordTitle)
            ? (string.IsNullOrWhiteSpace(recordKey) ? "Record" : recordKey)
            : recordTitle;

        var automationName = string.Format(
            CultureInfo.CurrentCulture,
            "{0} — {1} ({2})",
            displayModule,
            label,
            displayRecord);

        var automationId = string.Format(
            CultureInfo.InvariantCulture,
            "Dock.Inspector.{0}.{1}.{2}",
            moduleToken,
            recordToken,
            labelToken);

        var automationTooltip = string.Format(
            CultureInfo.CurrentCulture,
            "{0} for {1} in {2}.",
            label,
            displayRecord,
            displayModule);

        return new InspectorField(label, value, automationName, automationId, automationTooltip);
    }

    private static string NormalizeAutomationToken(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var normalized = new string(value.Where(char.IsLetterOrDigit).ToArray());
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}
