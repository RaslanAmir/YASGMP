using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using YasGMP.Models;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.ViewModels;

/// <summary>
/// Shared asset editor payload surfaced to the WPF shell.
/// </summary>
public sealed partial class AssetViewModel : SignatureAwareEditor
{
    private readonly IMachineCrudService _machineService;

    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);

    [ObservableProperty]
    private ObservableCollection<Asset> _assets = new();

    [ObservableProperty]
    private ObservableCollection<Asset> _filteredAssets = new();

    [ObservableProperty]
    private Asset? _selectedAsset;

    [ObservableProperty]
    private string? _searchTerm;

    [ObservableProperty]
    private string? _statusFilter;

    [ObservableProperty]
    private string? _riskFilter;

    [ObservableProperty]
    private string? _typeFilter;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    public AssetViewModel(IMachineCrudService machineService)
    {
        _machineService = machineService ?? throw new ArgumentNullException(nameof(machineService));
    }

    public event EventHandler? EditorChanged;

    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _model = string.Empty;

    [ObservableProperty]
    private string _manufacturer = string.Empty;

    [ObservableProperty]
    private string _location = string.Empty;

    [ObservableProperty]
    private string _status = "active";

    [ObservableProperty]
    private string _ursDoc = string.Empty;

    [ObservableProperty]
    private DateTime? _installDate = DateTime.UtcNow.Date;

    [ObservableProperty]
    private DateTime? _procurementDate;

    [ObservableProperty]
    private DateTime? _warrantyUntil;

    [ObservableProperty]
    private bool _isCritical;

    [ObservableProperty]
    private string _serialNumber = string.Empty;

    [ObservableProperty]
    private string _lifecyclePhase = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _qrCode = string.Empty;

    [ObservableProperty]
    private string _qrPayload = string.Empty;

    partial void OnSearchTermChanged(string? value)
        => ApplyFilters();

    partial void OnStatusFilterChanged(string? value)
        => ApplyFilters();

    partial void OnRiskFilterChanged(string? value)
        => ApplyFilters();

    partial void OnTypeFilterChanged(string? value)
        => ApplyFilters();

    partial void OnSelectedAssetChanged(Asset? value)
    {
        if (value is null)
        {
            return;
        }

        LoadFromAsset(value);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is null)
        {
            return;
        }

        if (ShouldRaiseEditorChanged(e.PropertyName))
        {
            EditorChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static bool ShouldRaiseEditorChanged(string propertyName)
        => propertyName is nameof(Id)
            or nameof(Code)
            or nameof(Name)
            or nameof(Description)
            or nameof(Model)
            or nameof(Manufacturer)
            or nameof(Location)
            or nameof(Status)
            or nameof(UrsDoc)
            or nameof(InstallDate)
            or nameof(ProcurementDate)
            or nameof(WarrantyUntil)
            or nameof(IsCritical)
            or nameof(SerialNumber)
            or nameof(LifecyclePhase)
            or nameof(Notes)
            or nameof(QrCode)
            or nameof(QrPayload)
            or nameof(SignatureHash)
            or nameof(SignatureReason)
            or nameof(SignatureNote)
            or nameof(SignatureTimestampUtc)
            or nameof(SignerUserId)
            or nameof(SignerUserName)
            or nameof(LastModifiedUtc)
            or nameof(LastModifiedById)
            or nameof(LastModifiedByName)
            or nameof(SourceIp)
            or nameof(SessionId)
            or nameof(DeviceInfo);

    /// <summary>
    /// Loads the latest assets from the CRUD service and reapplies search/filters.
    /// </summary>
    public async Task LoadAssetsAsync(CancellationToken cancellationToken = default)
    {
        await _loadSemaphore.WaitAsync(cancellationToken);

        try
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;

            IReadOnlyList<Machine> machines;
            try
            {
                machines = await _machineService.GetAllAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading assets: {ex.Message}";
                return;
            }

            var projected = machines.Select(ProjectToAsset).ToList();
            ReplaceCollection(Assets, projected);
            ApplyFilters();
            StatusMessage = $"Loaded {Assets.Count} assets.";
        }
        finally
        {
            IsBusy = false;
            _loadSemaphore.Release();
        }
    }

    /// <summary>
    /// Retrieves an asset from the filtered collection or loads it from the service when missing.
    /// </summary>
    public async Task<Asset?> EnsureAssetAsync(int? id, string? code, CancellationToken cancellationToken = default)
    {
        Asset? match = null;
        if (id is > 0)
        {
            match = FilteredAssets.FirstOrDefault(asset => asset.Id == id)
                ?? Assets.FirstOrDefault(asset => asset.Id == id);
        }

        if (match is null && !string.IsNullOrWhiteSpace(code))
        {
            var normalized = code.Trim();
            match = FilteredAssets.FirstOrDefault(asset => string.Equals(asset.AssetCode, normalized, StringComparison.OrdinalIgnoreCase))
                ?? Assets.FirstOrDefault(asset => string.Equals(asset.AssetCode, normalized, StringComparison.OrdinalIgnoreCase));
        }

        if (match is not null)
        {
            SelectedAsset = match;
            return match;
        }

        if (id is not > 0)
        {
            return null;
        }

        try
        {
            var machine = await _machineService.TryGetByIdAsync(id.Value);
            if (machine is null)
            {
                return null;
            }

            var asset = ProjectToAsset(machine);
            UpsertAsset(asset);
            SelectedAsset = asset;
            return asset;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Clears the shared selection reference.
    /// </summary>
    public void ClearSelection() => SelectedAsset = null;

    /// <summary>
    /// Resets the editor for Add mode and aligns the shared selection placeholder.
    /// </summary>
    public void PrepareForNew(string normalizedStatus)
    {
        Reset();
        Status = normalizedStatus ?? string.Empty;
        LastModifiedUtc = DateTime.UtcNow;
        SelectedAsset = null;
    }

    /// <summary>
    /// Loads the editor from an existing machine snapshot while synchronizing collections.
    /// </summary>
    public void PrepareForExisting(Machine machine, Func<string?, string> normalizer)
    {
        LoadFromMachine(machine, normalizer);
        var asset = ProjectToAsset(machine);
        UpsertAsset(asset);
        SelectedAsset = asset;
    }

    /// <summary>
    /// Uses the shared CRUD service to validate the current machine snapshot.
    /// </summary>
    public IReadOnlyList<string> ValidateMachine(Machine machine)
    {
        var errors = new List<string>();
        try
        {
            _machineService.Validate(machine);
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(ex.Message);
        }
        catch (Exception ex)
        {
            errors.Add($"Unexpected validation failure: {ex.Message}");
        }

        return errors;
    }

    /// <summary>
    /// Persists a new machine record via the CRUD service.
    /// </summary>
    public Task<CrudSaveResult> AddAsync(Machine machine, MachineCrudContext context)
        => _machineService.CreateAsync(machine, context);

    /// <summary>
    /// Updates an existing machine record via the CRUD service.
    /// </summary>
    public Task<CrudSaveResult> UpdateAsync(Machine machine, MachineCrudContext context)
        => _machineService.UpdateAsync(machine, context);

    /// <summary>
    /// Synchronizes the selected asset snapshot with the current editor fields.
    /// </summary>
    public void SyncSelectedAssetFromEditor()
    {
        if (SelectedAsset is null)
        {
            SelectedAsset = BuildSnapshotFromEditor();
            return;
        }

        var asset = SelectedAsset;
        asset.Id = Id;
        asset.AssetCode = Code;
        asset.AssetName = Name;
        asset.Description = string.IsNullOrWhiteSpace(Description) ? null : Description;
        asset.Model = string.IsNullOrWhiteSpace(Model) ? null : Model;
        asset.Manufacturer = string.IsNullOrWhiteSpace(Manufacturer) ? null : Manufacturer;
        asset.Location = string.IsNullOrWhiteSpace(Location) ? null : Location;
        asset.Status = string.IsNullOrWhiteSpace(Status) ? null : Status;
        asset.UrsDoc = string.IsNullOrWhiteSpace(UrsDoc) ? null : UrsDoc;
        asset.InstallDate = InstallDate;
        asset.ProcurementDate = ProcurementDate;
        asset.WarrantyUntil = WarrantyUntil;
        asset.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes;
        asset.QrCode = string.IsNullOrWhiteSpace(QrCode) ? null : QrCode;
        asset.QrPayload = string.IsNullOrWhiteSpace(QrPayload) ? null : QrPayload;
        asset.DigitalSignature = string.IsNullOrWhiteSpace(SignatureHash) ? null : SignatureHash;
        asset.LastModified = LastModifiedUtc;
    }

    /// <summary>
    /// Clears all fields to their neutral defaults.
    /// </summary>
    public void Reset()
    {
        Id = 0;
        Code = string.Empty;
        Name = string.Empty;
        Description = string.Empty;
        Model = string.Empty;
        Manufacturer = string.Empty;
        Location = string.Empty;
        Status = "active";
        UrsDoc = string.Empty;
        InstallDate = DateTime.UtcNow.Date;
        ProcurementDate = null;
        WarrantyUntil = null;
        IsCritical = false;
        SerialNumber = string.Empty;
        LifecyclePhase = string.Empty;
        Notes = string.Empty;
        QrCode = string.Empty;
        QrPayload = string.Empty;
        SignatureHash = string.Empty;
        SignatureReason = string.Empty;
        SignatureNote = string.Empty;
        SignatureTimestampUtc = null;
        SignerUserId = null;
        SignerUserName = string.Empty;
        LastModifiedUtc = null;
        LastModifiedById = null;
        LastModifiedByName = string.Empty;
        SourceIp = string.Empty;
        SessionId = string.Empty;
        DeviceInfo = string.Empty;
    }

    /// <summary>
    /// Prepares the view-model for Add mode.
    /// </summary>
    public void InitializeForNew(string normalizedStatus)
    {
        Reset();
        Status = normalizedStatus ?? string.Empty;
        LastModifiedUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Populates the view-model from an existing machine entity.
    /// </summary>
    public void LoadFromMachine(Machine machine, Func<string?, string> normalizer)
    {
        if (machine is null)
        {
            throw new ArgumentNullException(nameof(machine));
        }

        if (normalizer is null)
        {
            throw new ArgumentNullException(nameof(normalizer));
        }

        Id = machine.Id;
        Code = machine.Code ?? string.Empty;
        Name = machine.Name ?? string.Empty;
        Description = machine.Description ?? string.Empty;
        Model = machine.Model ?? string.Empty;
        Manufacturer = machine.Manufacturer ?? string.Empty;
        Location = machine.Location ?? string.Empty;
        Status = normalizer(machine.Status);
        UrsDoc = machine.UrsDoc ?? string.Empty;
        InstallDate = machine.InstallDate;
        ProcurementDate = machine.ProcurementDate;
        WarrantyUntil = machine.WarrantyUntil;
        IsCritical = machine.IsCritical;
        SerialNumber = machine.SerialNumber ?? string.Empty;
        LifecyclePhase = machine.LifecyclePhase ?? string.Empty;
        Notes = machine.Note ?? string.Empty;
        QrCode = machine.QrCode ?? string.Empty;
        QrPayload = machine.QrPayload ?? string.Empty;
        SignatureHash = machine.DigitalSignature ?? string.Empty;
        SignatureTimestampUtc = machine.LastModified;
        SignerUserId = machine.LastModifiedById;
        SignerUserName = machine.LastModifiedBy?.FullName ?? string.Empty;
        LastModifiedUtc = machine.LastModified;
        LastModifiedById = machine.LastModifiedById;
        LastModifiedByName = machine.LastModifiedBy?.FullName ?? string.Empty;
        SignatureReason = string.Empty;
        SignatureNote = string.Empty;
        SourceIp = string.Empty;
        SessionId = string.Empty;
        DeviceInfo = string.Empty;
    }

    /// <summary>
    /// Converts the view-model into a machine entity for persistence.
    /// </summary>
    public Machine ToMachine(Machine? existing)
    {
        var machine = existing is null ? new Machine() : CloneMachine(existing);
        machine.Id = Id;
        machine.Code = Code;
        machine.Name = Name;
        machine.Description = Description;
        machine.Model = Model;
        machine.Manufacturer = Manufacturer;
        machine.Location = Location;
        machine.Status = Status;
        machine.UrsDoc = UrsDoc;
        machine.InstallDate = InstallDate;
        machine.ProcurementDate = ProcurementDate;
        machine.WarrantyUntil = WarrantyUntil;
        machine.IsCritical = IsCritical;
        machine.SerialNumber = string.IsNullOrWhiteSpace(SerialNumber) ? machine.SerialNumber : SerialNumber;
        machine.LifecyclePhase = LifecyclePhase;
        machine.Note = Notes;
        machine.QrCode = string.IsNullOrWhiteSpace(QrCode) ? null : QrCode.Trim();
        machine.QrPayload = string.IsNullOrWhiteSpace(QrPayload) ? null : QrPayload.Trim();
        machine.DigitalSignature = SignatureHash;
        machine.LastModified = LastModifiedUtc ?? DateTime.UtcNow;
        machine.LastModifiedById = LastModifiedById ?? machine.LastModifiedById;
        return machine;
    }

    private static Machine CloneMachine(Machine source)
    {
        return new Machine
        {
            Id = source.Id,
            Code = source.Code,
            Name = source.Name,
            Description = source.Description,
            Model = source.Model,
            Manufacturer = source.Manufacturer,
            Location = source.Location,
            Status = source.Status,
            UrsDoc = source.UrsDoc,
            InstallDate = source.InstallDate,
            ProcurementDate = source.ProcurementDate,
            WarrantyUntil = source.WarrantyUntil,
            IsCritical = source.IsCritical,
            SerialNumber = source.SerialNumber,
            LifecyclePhase = source.LifecyclePhase,
            Note = source.Note,
            QrCode = source.QrCode,
            QrPayload = source.QrPayload
        };
    }

    private void LoadFromAsset(Asset asset)
    {
        if (asset is null)
        {
            throw new ArgumentNullException(nameof(asset));
        }

        Id = asset.Id;
        Code = asset.AssetCode ?? string.Empty;
        Name = asset.AssetName ?? string.Empty;
        Description = asset.Description ?? string.Empty;
        Model = asset.Model ?? string.Empty;
        Manufacturer = asset.Manufacturer ?? string.Empty;
        Location = asset.Location ?? string.Empty;
        Status = asset.Status ?? string.Empty;
        UrsDoc = asset.UrsDoc ?? string.Empty;
        InstallDate = asset.InstallDate;
        ProcurementDate = asset.ProcurementDate;
        WarrantyUntil = asset.WarrantyUntil;
        Notes = asset.Notes ?? string.Empty;
        QrCode = asset.QrCode ?? string.Empty;
        QrPayload = asset.QrPayload ?? string.Empty;
        SignatureHash = asset.DigitalSignature ?? string.Empty;
        LastModifiedUtc = asset.LastModified;
    }

    private void ApplyFilters()
    {
        if (Assets.Count == 0)
        {
            ReplaceCollection(FilteredAssets, Array.Empty<Asset>());
            return;
        }

        IEnumerable<Asset> query = Assets;

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim();
            query = query.Where(asset =>
                (!string.IsNullOrWhiteSpace(asset.AssetName) && asset.AssetName.Contains(term, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(asset.AssetCode) && asset.AssetCode.Contains(term, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(asset.Location) && asset.Location.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(StatusFilter))
        {
            query = query.Where(asset => string.Equals(asset.Status, StatusFilter, StringComparison.OrdinalIgnoreCase));
        }

        ReplaceCollection(FilteredAssets, query);
    }

    private void ReplaceCollection(ObservableCollection<Asset> target, IEnumerable<Asset> items)
    {
        target.Clear();
        foreach (var asset in items)
        {
            target.Add(asset);
        }
    }

    private void UpsertAsset(Asset asset)
    {
        var existing = Assets.FirstOrDefault(a => a.Id == asset.Id);
        if (existing is not null)
        {
            UpdateAsset(existing, asset);
        }
        else
        {
            Assets.Add(asset);
        }

        ApplyFilters();
    }

    private static void UpdateAsset(Asset target, Asset source)
    {
        target.AssetCode = source.AssetCode;
        target.AssetName = source.AssetName;
        target.Description = source.Description;
        target.Model = source.Model;
        target.Manufacturer = source.Manufacturer;
        target.Location = source.Location;
        target.Status = source.Status;
        target.UrsDoc = source.UrsDoc;
        target.InstallDate = source.InstallDate;
        target.ProcurementDate = source.ProcurementDate;
        target.WarrantyUntil = source.WarrantyUntil;
        target.Notes = source.Notes;
        target.QrCode = source.QrCode;
        target.QrPayload = source.QrPayload;
        target.DigitalSignature = source.DigitalSignature;
        target.LastModified = source.LastModified;
    }

    private static Asset ProjectToAsset(Machine machine)
    {
        return new Asset
        {
            Id = machine.Id,
            Code = machine.Code ?? string.Empty,
            Name = machine.Name ?? string.Empty,
            Description = machine.Description,
            Model = machine.Model,
            Manufacturer = machine.Manufacturer,
            Location = machine.Location,
            Status = machine.Status,
            UrsDoc = machine.UrsDoc,
            InstallDate = machine.InstallDate,
            ProcurementDate = machine.ProcurementDate,
            WarrantyUntil = machine.WarrantyUntil,
            Notes = machine.Note,
            QrCode = machine.QrCode,
            QrPayload = machine.QrPayload,
            DigitalSignature = machine.DigitalSignature,
            LastModified = machine.LastModified
        };
    }

    private Asset BuildSnapshotFromEditor()
    {
        return new Asset
        {
            Id = Id,
            Code = Code,
            Name = Name,
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description,
            Model = string.IsNullOrWhiteSpace(Model) ? null : Model,
            Manufacturer = string.IsNullOrWhiteSpace(Manufacturer) ? null : Manufacturer,
            Location = string.IsNullOrWhiteSpace(Location) ? null : Location,
            Status = string.IsNullOrWhiteSpace(Status) ? null : Status,
            UrsDoc = string.IsNullOrWhiteSpace(UrsDoc) ? null : UrsDoc,
            InstallDate = InstallDate,
            ProcurementDate = ProcurementDate,
            WarrantyUntil = WarrantyUntil,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes,
            QrCode = string.IsNullOrWhiteSpace(QrCode) ? null : QrCode,
            QrPayload = string.IsNullOrWhiteSpace(QrPayload) ? null : QrPayload,
            DigitalSignature = string.IsNullOrWhiteSpace(SignatureHash) ? null : SignatureHash,
            LastModified = LastModifiedUtc
        };
    }
}
