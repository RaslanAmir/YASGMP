using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Services;

/// <summary>Service used to display SAP Business One style choose-from-list dialogs.</summary>
public interface ICflDialogService
{
    Task<CflResult?> ShowAsync(CflRequest request);
}

/// <summary>Descriptor for CFL invocations.</summary>
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

/// <summary>Single CFL row.</summary>
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

/// <summary>Result returned from a CFL dialog.</summary>
public sealed class CflResult
{
    public CflResult(CflItem selected)
    {
        Selected = selected;
    }

    public CflItem Selected { get; }
}
