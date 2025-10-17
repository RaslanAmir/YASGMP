using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Wraps the shared MAUI asset view-model so the WPF shell can observe list updates.
/// </summary>
public sealed class SharedAssetListViewModelAdapter : IAssetListViewModel, IDisposable
{
    private readonly YasGMP.ViewModels.AssetViewModel _inner;

    public SharedAssetListViewModelAdapter(YasGMP.ViewModels.AssetViewModel inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _inner.PropertyChanged += ForwardPropertyChanged;
    }

    public ObservableCollection<Asset> Assets => _inner.Assets;

    public ObservableCollection<Asset> FilteredAssets => _inner.FilteredAssets;

    public Asset? SelectedAsset
    {
        get => _inner.SelectedAsset;
        set => _inner.SelectedAsset = value;
    }

    public string? SearchTerm
    {
        get => _inner.SearchTerm;
        set => _inner.SearchTerm = value;
    }

    public string? StatusFilter
    {
        get => _inner.StatusFilter;
        set => _inner.StatusFilter = value;
    }

    public string? RiskFilter
    {
        get => _inner.RiskFilter;
        set => _inner.RiskFilter = value;
    }

    public string? TypeFilter
    {
        get => _inner.TypeFilter;
        set => _inner.TypeFilter = value;
    }

    public Task LoadAssetsAsync() => _inner.LoadAssetsAsync();

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose()
    {
        _inner.PropertyChanged -= ForwardPropertyChanged;
        GC.SuppressFinalize(this);
    }

    private void ForwardPropertyChanged(object? sender, PropertyChangedEventArgs e)
        => PropertyChanged?.Invoke(this, e);
}
