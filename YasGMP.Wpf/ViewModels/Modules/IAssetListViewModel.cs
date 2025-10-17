using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Abstraction over the shared asset list view-model surfaced to the WPF shell.
/// </summary>
public interface IAssetListViewModel : INotifyPropertyChanged
{
    /// <summary>All known assets.</summary>
    ObservableCollection<Asset> Assets { get; }

    /// <summary>Filtered subset exposed to UI lists.</summary>
    ObservableCollection<Asset> FilteredAssets { get; }

    /// <summary>Currently selected asset in the filtered list.</summary>
    Asset? SelectedAsset { get; set; }

    /// <summary>Free-text search filter.</summary>
    string? SearchTerm { get; set; }

    /// <summary>Status filter (nullable).</summary>
    string? StatusFilter { get; set; }

    /// <summary>Risk filter (nullable).</summary>
    string? RiskFilter { get; set; }

    /// <summary>Type filter (nullable).</summary>
    string? TypeFilter { get; set; }

    /// <summary>Loads assets into the backing collections.</summary>
    Task LoadAssetsAsync();
}
