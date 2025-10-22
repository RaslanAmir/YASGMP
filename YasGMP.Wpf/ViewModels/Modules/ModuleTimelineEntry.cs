using System;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Represents a lightweight timeline entry surfaced by module view-models.
/// </summary>
public sealed class ModuleTimelineEntry
{
    public ModuleTimelineEntry(DateTime timestamp, string summary, string details)
    {
        Timestamp = timestamp;
        Summary = summary ?? string.Empty;
        Details = details ?? string.Empty;
    }

    /// <summary>
    /// Gets the point in time associated with the timeline entry.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the concise description for the timeline entry.
    /// </summary>
    public string Summary { get; }

    /// <summary>
    /// Gets additional detail text for the timeline entry.
    /// </summary>
    public string Details { get; }
}
