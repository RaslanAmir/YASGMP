using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Defines the shell service responsible for surfacing SAP Business One style choose-from-list (CFL)
/// dialogs when a Golden Arrow navigation, field trigger, or form mode transition requires a user
/// selection.
/// </summary>
/// <remarks>
/// <para>
/// Shell components should invoke the service whenever a module needs a user-driven lookup to
/// continue a Find/Add/Update cycle, particularly when Golden Arrow relationships or dependent
/// master data must be resolved. Invocations are expected to originate on background threads but are
/// marshalled to the UI dispatcher by implementations.
/// </para>
/// <para>
/// Requests must provide localization-ready data; <see cref="CflRequest.Title"/> and
/// <see cref="CflItem.Label"/> values should be sourced from the shell's resource dictionaries so
/// downstream consumers receive fully translated text. Implementations are expected to return the
/// user's selection so form modes can update state and so audit logging pipelines (see
/// <see cref="CflResult"/>) can record the pick as part of the Golden Arrow and immutable audit
/// flows.
/// </para>
/// </remarks>
public interface ICflDialogService
{
    Task<CflResult?> ShowAsync(CflRequest request);
}

/// <summary>
/// Describes a CFL invocation including localized title text and selectable rows supplied by the
/// caller.
/// </summary>
/// <remarks>
/// The <see cref="Title"/> property should already be localized using the same resource providers
/// consumed by ribbon, form mode, and audit surfaces so that modal dialogs are consistent across the
/// shell. Items provided in <see cref="Items"/> must respect Golden Arrow conventions, meaning they
/// include all information needed for downstream audit logging to correlate the selection with the
/// initiating document.
/// </remarks>
public sealed class CflRequest
{
    public CflRequest(string title, IReadOnlyList<CflItem> items)
    {
        Title = title;
        Items = items;
    }

    public string Title { get; }

    public IReadOnlyList<CflItem> Items { get; }
}

/// <summary>
/// Represents a single row displayed in a CFL dialog including the record key, localized label, and
/// optional descriptive text.
/// </summary>
/// <remarks>
/// <see cref="Label"/> and <see cref="Description"/> should originate from localized catalogs and
/// include sufficient context (e.g., document numbers, revision codes) so that when a selection is
/// emitted it can be written verbatim into audit trails and Golden Arrow navigation logs without
/// additional lookups.
/// </remarks>
public sealed class CflItem
{
    public CflItem(string key, string label, string? description = null)
    {
        Key = key;
        Label = label;
        Description = description ?? string.Empty;
    }

    public string Key { get; }

    public string Label { get; }

    public string Description { get; }
}

/// <summary>
/// Captures the outcome of a CFL dialog, supplying the selected item for form mode transitions and
/// audit logging.
/// </summary>
/// <remarks>
/// Consumers should forward the <see cref="Selected"/> payload into the shell's audit logging
/// pipeline (e.g., the audit appenders used by save/confirm flows) alongside the form mode state so
/// that Golden Arrow history and immutable audit ledgers reflect the user's pick.
/// </remarks>
public sealed class CflResult
{
    public CflResult(CflItem selected)
    {
        Selected = selected;
    }

    public CflItem Selected { get; }
}
