using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Dialogs;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.ViewModels.Modules;
/// <summary>
/// Represents the warehouse module view model value.
/// </summary>

public sealed partial class WarehouseModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>
    /// Represents the module key value.
    /// </summary>
    public new const string ModuleKey = "Warehouse";

    private readonly IWarehouseCrudService _warehouseService;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IFilePicker _filePicker;
    private readonly IAuthContext _authContext;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private readonly ILocalizationService _localization;
    private readonly ObservableCollection<WarehouseZoneSummaryItem> _zoneSummaries;
    private readonly ObservableCollection<string> _transactionAlerts;
    private readonly ICollectionView _zoneSummariesView;
    private readonly Dictionary<ZoneClassification, string> _zoneLabels;
    private readonly string _zoneAllLabel;
    private Warehouse? _loadedWarehouse;
    private WarehouseEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;
    /// <summary>
    /// Initializes a new instance of the WarehouseModuleViewModel class.
    /// </summary>

    public WarehouseModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IWarehouseCrudService warehouseService,
        IAttachmentWorkflowService attachmentWorkflow,
        IFilePicker filePicker,
        IAuthContext authContext,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, localization.GetString("Module.Title.Warehouse"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _warehouseService = warehouseService ?? throw new ArgumentNullException(nameof(warehouseService));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        _zoneSummaries = new ObservableCollection<WarehouseZoneSummaryItem>();
        _transactionAlerts = new ObservableCollection<string>();
        _zoneLabels = new Dictionary<ZoneClassification, string>
        {
            [ZoneClassification.Critical] = GetZoneLabel("Module.Warehouse.ZoneFilter.Critical", "Critical"),
            [ZoneClassification.Warning] = GetZoneLabel("Module.Warehouse.ZoneFilter.Warning", "Warning"),
            [ZoneClassification.Healthy] = GetZoneLabel("Module.Warehouse.ZoneFilter.Healthy", "Healthy"),
            [ZoneClassification.Overflow] = GetZoneLabel("Module.Warehouse.ZoneFilter.Overflow", "Overflow")
        };
        _zoneAllLabel = GetZoneLabel("Module.Warehouse.ZoneFilter.All", "All Zones");
        ZoneFilters = new ObservableCollection<string>(new[]
        {
            _zoneAllLabel,
            _zoneLabels[ZoneClassification.Critical],
            _zoneLabels[ZoneClassification.Warning],
            _zoneLabels[ZoneClassification.Healthy],
            _zoneLabels[ZoneClassification.Overflow]
        });
        SelectedZoneFilter = _zoneAllLabel;
        _zoneSummariesView = CollectionViewSource.GetDefaultView(_zoneSummaries);
        _zoneSummariesView.Filter = FilterZoneSummary;

        Editor = WarehouseEditor.CreateEmpty();
        StatusOptions = new ReadOnlyCollection<string>(new[]
        {
            _localization.GetString("Module.Warehouse.Status.Qualified"),
            _localization.GetString("Module.Warehouse.Status.InQualification"),
            _localization.GetString("Module.Warehouse.Status.Maintenance"),
            _localization.GetString("Module.Warehouse.Status.Inactive")
        });
        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
        ReceiveStockCommand = new AsyncRelayCommand(() => ExecuteInventoryTransactionAsync(InventoryTransactionType.Receive), CanExecuteInventoryTransaction);
        IssueStockCommand = new AsyncRelayCommand(() => ExecuteInventoryTransactionAsync(InventoryTransactionType.Issue), CanExecuteInventoryTransaction);
        AdjustStockCommand = new AsyncRelayCommand(() => ExecuteInventoryTransactionAsync(InventoryTransactionType.Adjust), CanExecuteInventoryTransaction);
    }

    [ObservableProperty]
    private WarehouseEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;

    [ObservableProperty]
    private ObservableCollection<WarehouseStockSnapshot> _stockSnapshot = new();

    [ObservableProperty]
    private ObservableCollection<InventoryMovementEntry> _recentMovements = new();

    [ObservableProperty]
    private bool _hasStockAlerts;
    [ObservableProperty]
    private string _selectedZoneFilter = string.Empty;
    /// <summary>
    /// Gets or sets the status options.
    /// </summary>

    public IReadOnlyList<string> StatusOptions { get; }
    /// <summary>
    /// Gets or sets the attach document command.
    /// </summary>

    public IAsyncRelayCommand AttachDocumentCommand { get; }

    public ObservableCollection<WarehouseZoneSummaryItem> ZoneSummaries => _zoneSummaries;

    public ICollectionView ZoneSummariesView => _zoneSummariesView;

    public ObservableCollection<string> TransactionAlerts => _transactionAlerts;

    public ObservableCollection<string> ZoneFilters { get; }

    public IAsyncRelayCommand ReceiveStockCommand { get; }

    public IAsyncRelayCommand IssueStockCommand { get; }

    public IAsyncRelayCommand AdjustStockCommand { get; }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var warehouses = await _warehouseService.GetAllAsync().ConfigureAwait(false);
        return warehouses.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var sample = new[]
        {
            new Warehouse
            {
                Id = 1,
                Name = "Main Warehouse",
                Location = "Building B",
                Status = StatusOptions[0],
                LegacyResponsibleName = "John Doe",
                Note = "Primary GMP warehouse"
            },
            new Warehouse
            {
                Id = 2,
                Name = "Cold Storage",
                Location = "Building C",
                Status = StatusOptions[0],
                LegacyResponsibleName = "Jane Smith",
                ClimateMode = "2-8°C"
            }
        };

        return sample.Select(ToRecord).ToList();
    }

    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var warehouses = await _warehouseService.GetAllAsync().ConfigureAwait(false);
        var items = warehouses.Select(warehouse =>
        {
            var key = warehouse.Id.ToString(CultureInfo.InvariantCulture);
            var label = string.IsNullOrWhiteSpace(warehouse.Name) ? key : warehouse.Name;
            var descriptionParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(warehouse.Location))
            {
                descriptionParts.Add(warehouse.Location!);
            }

            if (!string.IsNullOrWhiteSpace(warehouse.Status))
            {
                descriptionParts.Add(warehouse.Status!);
            }

            return new CflItem(key, label, descriptionParts.Count > 0 ? string.Join(" • ", descriptionParts) : null);
        }).ToList();

        return new CflRequest("Select Warehouse", items);
    }

    protected override Task OnCflSelectionAsync(CflResult result)
    {
        var match = Records.FirstOrDefault(r => r.Key == result.Selected.Key);
        if (match is not null)
        {
            SelectedRecord = match;
            SearchText = match.Title;
        }
        else
        {
            SearchText = result.Selected.Label;
        }

        StatusMessage = _localization.GetString("Module.Status.Filtered", Title, SearchText ?? string.Empty);
        return Task.CompletedTask;
    }

    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedWarehouse = null;
            SetEditor(WarehouseEditor.CreateEmpty());
            ClearInsights();
            UpdateAttachmentCommandState();
            NotifyInventoryCommands();
            return;
        }

        if (IsInEditMode)
        {
            return;
        }

        if (!int.TryParse(record.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return;
        }

        var warehouse = await _warehouseService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (warehouse is null)
        {
            StatusMessage = $"Unable to locate warehouse #{id}.";
            return;
        }

        _loadedWarehouse = warehouse;
        LoadEditor(warehouse);
        await LoadInsightsAsync(id).ConfigureAwait(false);
        UpdateAttachmentCommandState();
        NotifyInventoryCommands();
    }

    protected override Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        switch (mode)
        {
            case FormMode.Add:
                _snapshot = null;
                _loadedWarehouse = null;
                SetEditor(WarehouseEditor.CreateForNew(_warehouseService.NormalizeStatus("qualified")));
                ClearInsights();
                break;
            case FormMode.Update:
                _snapshot = Editor.Clone();
                break;
            case FormMode.View:
                if (_loadedWarehouse is not null)
                {
                    LoadEditor(_loadedWarehouse);
                    if (_loadedWarehouse.Id > 0)
                    {
                        _ = LoadInsightsAsync(_loadedWarehouse.Id);
                    }
                }
                break;
        }

        UpdateAttachmentCommandState();
        NotifyInventoryCommands();
        return Task.CompletedTask;
    }

    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var warehouse = Editor.ToWarehouse(_loadedWarehouse);
        var errors = new List<string>();
        try
        {
            _warehouseService.Validate(warehouse);
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(ex.Message);
        }
        catch (Exception ex)
        {
            errors.Add($"Unexpected validation error: {ex.Message}");
        }

        return await Task.FromResult(errors).ConfigureAwait(false);
    }

    protected override async Task<bool> OnSaveAsync()
    {
        var warehouse = Editor.ToWarehouse(_loadedWarehouse);
        warehouse.Status = _warehouseService.NormalizeStatus(warehouse.Status);

        if (Mode == FormMode.Update && _loadedWarehouse is null)
        {
            StatusMessage = "Select a warehouse before saving.";
            return false;
        }

        var recordId = Mode == FormMode.Update ? _loadedWarehouse!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("warehouses", recordId))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Electronic signature failed: {ex.Message}";
            return false;
        }

        if (signatureResult is null)
        {
            StatusMessage = "Electronic signature cancelled. Save aborted.";
            return false;
        }

        if (signatureResult.Signature is null)
        {
            StatusMessage = "Electronic signature was not captured.";
            return false;
        }

        warehouse.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;
        warehouse.LastModified = DateTime.UtcNow;
        warehouse.LastModifiedById = _authContext.CurrentUser?.Id;
        warehouse.LastModifiedByName = _authContext.CurrentUser?.FullName ?? warehouse.LastModifiedByName;
        warehouse.SourceIp = _authContext.CurrentIpAddress ?? warehouse.SourceIp ?? string.Empty;
        warehouse.SessionId = _authContext.CurrentSessionId ?? warehouse.SessionId ?? string.Empty;

        var context = WarehouseCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        Warehouse adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _warehouseService.CreateAsync(warehouse, context).ConfigureAwait(false);
                adapterResult = warehouse;
            }
            else if (Mode == FormMode.Update)
            {
                warehouse.Id = _loadedWarehouse!.Id;
                saveResult = await _warehouseService.UpdateAsync(warehouse, context).ConfigureAwait(false);
                adapterResult = warehouse;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist warehouse: {ex.Message}", ex);
        }

        _loadedWarehouse = warehouse;
        LoadEditor(warehouse);

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "warehouses",
            recordId: adapterResult.Id,
            metadata: saveResult.SignatureMetadata,
            fallbackSignatureHash: adapterResult.DigitalSignature,
            fallbackMethod: context.SignatureMethod,
            fallbackStatus: context.SignatureStatus,
            fallbackNote: context.SignatureNote,
            signedAt: signatureResult.Signature.SignedAt,
            fallbackDeviceInfo: context.DeviceInfo,
            fallbackIpAddress: context.Ip,
            fallbackSessionId: context.SessionId);

        try
        {
            await SignaturePersistenceHelper
                .PersistIfRequiredAsync(_signatureDialog, signatureResult)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to persist electronic signature: {ex.Message}";
            Mode = FormMode.Update;
            return false;
        }

        StatusMessage = $"Electronic signature captured ({signatureResult.ReasonDisplay}).";
        return true;
    }

    protected override void OnCancel()
    {
        if (Mode == FormMode.Add)
        {
            if (_loadedWarehouse is not null)
            {
                LoadEditor(_loadedWarehouse);
            }
            else
            {
                SetEditor(WarehouseEditor.CreateEmpty());
            }
        }
        else if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
        }

        UpdateAttachmentCommandState();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is nameof(IsBusy) or nameof(Mode) or nameof(SelectedRecord) or nameof(IsDirty))
        {
            UpdateAttachmentCommandState();
            NotifyInventoryCommands();
        }
    }

    partial void OnEditorChanging(WarehouseEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(WarehouseEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged += OnEditorPropertyChanged;
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressEditorDirtyNotifications)
        {
            return;
        }

        if (IsInEditMode)
        {
            MarkDirty();
        }
    }

    partial void OnSelectedZoneFilterChanged(string value)
    {
        _zoneSummariesView.Refresh();
    }

    private bool FilterZoneSummary(object obj)
    {
        if (obj is not WarehouseZoneSummaryItem item)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SelectedZoneFilter)
            || string.Equals(SelectedZoneFilter, _zoneAllLabel, StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(item.ZoneLabel, SelectedZoneFilter, StringComparison.Ordinal))
        {
            return true;
        }

        return _zoneLabels.TryGetValue(item.Classification, out var label)
            && string.Equals(label, SelectedZoneFilter, StringComparison.Ordinal);
    }

    private void LoadEditor(Warehouse warehouse)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = WarehouseEditor.FromWarehouse(warehouse, _warehouseService.NormalizeStatus);
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    private void SetEditor(WarehouseEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    private async Task LoadInsightsAsync(int warehouseId)
    {
        try
        {
            var stock = await _warehouseService.GetStockSnapshotAsync(warehouseId).ConfigureAwait(false);
            var stockList = stock.ToList();
            StockSnapshot = new ObservableCollection<WarehouseStockSnapshot>(stockList);
            HasStockAlerts = StockSnapshot.Any(s => s.IsBelowMinimum || s.IsAboveMaximum);

            var alertCount = 0;
            await RunOnUiThreadAsync(() =>
            {
                _zoneSummaries.Clear();
                _transactionAlerts.Clear();

                foreach (var entry in stockList)
                {
                    var netQuantity = entry.Quantity - entry.Reserved - entry.Blocked;
                    var classification = ClassifyZone(netQuantity, entry.MinThreshold, entry.MaxThreshold);
                    var zoneLabel = _zoneLabels.TryGetValue(classification, out var label)
                        ? label
                        : classification.ToString();

                    _zoneSummaries.Add(new WarehouseZoneSummaryItem(
                        entry.PartId,
                        entry.PartName,
                        entry.PartCode,
                        zoneLabel,
                        classification,
                        netQuantity,
                        entry.MinThreshold,
                        entry.MaxThreshold));

                    if (classification == ZoneClassification.Critical)
                    {
                        var minText = entry.MinThreshold.HasValue
                            ? entry.MinThreshold.Value.ToString(CultureInfo.InvariantCulture)
                            : "n/a";
                        _transactionAlerts.Add($"{entry.PartName} below minimum ({netQuantity}/{minText}).");
                    }
                    else if (classification == ZoneClassification.Overflow)
                    {
                        var maxText = entry.MaxThreshold.HasValue
                            ? entry.MaxThreshold.Value.ToString(CultureInfo.InvariantCulture)
                            : "n/a";
                        _transactionAlerts.Add($"{entry.PartName} above maximum ({netQuantity}/{maxText}).");
                    }
                }

                alertCount = _transactionAlerts.Count;
                _zoneSummariesView.Refresh();
            }).ConfigureAwait(false);

            var movements = await _warehouseService.GetRecentMovementsAsync(warehouseId, 15).ConfigureAwait(false);
            RecentMovements = new ObservableCollection<InventoryMovementEntry>(movements);

            var baseMessage = HasStockAlerts
                ? $"{Title}: stock alerts detected for {StockSnapshot.Count(s => s.IsBelowMinimum)} item(s)."
                : $"{Title}: stock overview refreshed ({StockSnapshot.Count} tracked parts).";

            StatusMessage = alertCount > 0
                ? $"{baseMessage} {alertCount} alert(s) highlighted."
                : baseMessage;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to load warehouse insight: {ex.Message}";
        }
    }

    private void ClearInsights()
    {
        StockSnapshot.Clear();
        RecentMovements.Clear();
        HasStockAlerts = false;
        _zoneSummaries.Clear();
        _transactionAlerts.Clear();
        _zoneSummariesView.Refresh();
    }

    private bool CanAttachDocument()
        => !IsBusy
           && !IsEditorEnabled
           && _loadedWarehouse is not null
           && _loadedWarehouse.Id > 0;

    private async Task AttachDocumentAsync()
    {
        if (_loadedWarehouse is null || _loadedWarehouse.Id <= 0)
        {
            return;
        }

        var files = await _filePicker.PickFilesAsync(new FilePickerRequest
        {
            Title = "Attach file to warehouse"
        }).ConfigureAwait(false);

        if (files.Count == 0)
        {
            return;
        }

        var processed = 0;
        var deduplicated = 0;

        foreach (var file in files)
        {
            await using var stream = await file.OpenReadAsync().ConfigureAwait(false);
            var request = new AttachmentUploadRequest
            {
                EntityType = "warehouses",
                EntityId = _loadedWarehouse.Id,
                FileName = file.FileName,
                ContentType = file.ContentType,
                UploadedById = _authContext.CurrentUser?.Id,
                SourceIp = _authContext.CurrentIpAddress,
                SourceHost = _authContext.CurrentDeviceInfo,
                Reason = $"Warehouse attachment via WPF on {DateTime.UtcNow:O}"
            };

            var result = await _attachmentWorkflow.UploadAsync(stream, request).ConfigureAwait(false);
            processed++;
            if (result.Deduplicated)
            {
                deduplicated++;
            }
        }

        StatusMessage = AttachmentStatusFormatter.Format(processed, deduplicated);
    }

    private void NotifyInventoryCommands()
    {
        ReceiveStockCommand.NotifyCanExecuteChanged();
        IssueStockCommand.NotifyCanExecuteChanged();
        AdjustStockCommand.NotifyCanExecuteChanged();
    }

    private bool CanExecuteInventoryTransaction()
        => !IsBusy
           && Mode == FormMode.View
           && _loadedWarehouse is not null
           && _loadedWarehouse.Id > 0;

    private async Task ExecuteInventoryTransactionAsync(InventoryTransactionType type)
    {
        if (_loadedWarehouse is null || _loadedWarehouse.Id <= 0)
        {
            return;
        }

        List<Part> parts;
        try
        {
            parts = await Database.GetAllPartsAsync().ConfigureAwait(false) ?? new List<Part>();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load parts: {ex.Message}";
            return;
        }

        var snapshotLookup = StockSnapshot.ToDictionary(s => s.PartId, s => s);

        var partOptions = parts
            .Select(part =>
            {
                snapshotLookup.TryGetValue(part.Id, out var snapshot);
                var quantity = snapshot?.Quantity ?? 0;
                var min = snapshot?.MinThreshold;
                var max = snapshot?.MaxThreshold;
                var display = string.IsNullOrWhiteSpace(part.Name)
                    ? part.Code ?? $"Part #{part.Id}"
                    : string.IsNullOrWhiteSpace(part.Code)
                        ? part.Name
                        : $"{part.Name} ({part.Code})";
                return new WarehouseStockPartOption(
                    part.Id,
                    display ?? $"Part #{part.Id}",
                    part.Code ?? string.Empty,
                    quantity,
                    min,
                    max);
            })
            .OrderBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var snapshot in StockSnapshot)
        {
            if (partOptions.Any(option => option.PartId == snapshot.PartId))
            {
                continue;
            }

            var display = string.IsNullOrWhiteSpace(snapshot.PartName)
                ? snapshot.PartCode
                : string.IsNullOrWhiteSpace(snapshot.PartCode)
                    ? snapshot.PartName
                    : $"{snapshot.PartName} ({snapshot.PartCode})";

            partOptions.Add(new WarehouseStockPartOption(
                snapshot.PartId,
                display ?? $"Part #{snapshot.PartId}",
                snapshot.PartCode,
                snapshot.Quantity - snapshot.Reserved - snapshot.Blocked,
                snapshot.MinThreshold,
                snapshot.MaxThreshold));
        }

        if (partOptions.Count == 0)
        {
            StatusMessage = "No parts available for inventory transaction.";
            return;
        }

        InventoryTransactionRequest? submittedRequest = null;
        ElectronicSignatureDialogResult? submittedSignature = null;

        await RunOnUiThreadAsync(() =>
        {
            var warehouseName = string.IsNullOrWhiteSpace(_loadedWarehouse.Name)
                ? $"Warehouse #{_loadedWarehouse.Id}"
                : _loadedWarehouse.Name;

            var dialogVm = new WarehouseStockTransactionDialogViewModel(
                _loadedWarehouse.Id,
                warehouseName ?? $"Warehouse #{_loadedWarehouse.Id}",
                type,
                _signatureDialog,
                async (request, signature) =>
                {
                    var context = WarehouseCrudContext.Create(
                        _authContext.CurrentUser?.Id ?? 0,
                        _authContext.CurrentIpAddress,
                        _authContext.CurrentDeviceInfo,
                        _authContext.CurrentSessionId,
                        signature);
                    await _warehouseService.ExecuteInventoryTransactionAsync(request, context, signature)
                        .ConfigureAwait(false);
                });

            dialogVm.LoadParts(partOptions);

            var dialog = new WarehouseStockTransactionDialog
            {
                Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow,
                DataContext = dialogVm
            };

            var result = dialog.ShowDialog();
            if (result == true)
            {
                submittedRequest = dialogVm.SubmittedRequest;
                submittedSignature = dialogVm.SubmittedSignature;
            }
        }).ConfigureAwait(false);

        if (submittedRequest is null || submittedSignature is null)
        {
            return;
        }

        await LoadInsightsAsync(_loadedWarehouse.Id).ConfigureAwait(false);

        var delta = type switch
        {
            InventoryTransactionType.Adjust => submittedRequest.Value.AdjustmentDelta ?? 0,
            InventoryTransactionType.Issue => -submittedRequest.Value.Quantity,
            _ => submittedRequest.Value.Quantity
        };

        var verb = type switch
        {
            InventoryTransactionType.Receive => "received",
            InventoryTransactionType.Issue => "issued",
            InventoryTransactionType.Adjust => "adjusted",
            _ => "processed"
        };

        var deltaDisplay = delta >= 0
            ? $"+{delta}"
            : delta.ToString(CultureInfo.InvariantCulture);

        var warehouseLabel = string.IsNullOrWhiteSpace(_loadedWarehouse.Name)
            ? $"warehouse #{_loadedWarehouse.Id}"
            : _loadedWarehouse.Name;

        var message = $"{verb} {deltaDisplay} units for {warehouseLabel}.";
        StatusMessage = message;
        _shellInteraction.UpdateStatus(message);

        NotifyInventoryCommands();
    }

    private string GetZoneLabel(string resourceKey, string fallback)
    {
        var label = _localization.GetString(resourceKey);
        return string.IsNullOrWhiteSpace(label) ? fallback : label;
    }

    private ZoneClassification ClassifyZone(int quantity, int? minimum, int? maximum)
    {
        if (minimum.HasValue && quantity < minimum.Value)
        {
            return ZoneClassification.Critical;
        }

        if (maximum.HasValue && quantity > maximum.Value)
        {
            return ZoneClassification.Overflow;
        }

        if (minimum.HasValue)
        {
            var warningThreshold = (int)Math.Ceiling(minimum.Value * 1.2);
            if (quantity <= warningThreshold)
            {
                return ZoneClassification.Warning;
            }
        }

        return ZoneClassification.Healthy;
    }

    private Task RunOnUiThreadAsync(Action action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(action, DispatcherPriority.DataBind).Task;
    }

    private void UpdateAttachmentCommandState()
        => AttachDocumentCommand.NotifyCanExecuteChanged();

    private ModuleRecord ToRecord(Warehouse warehouse)
    {
        var recordKey = warehouse.Id.ToString(CultureInfo.InvariantCulture);
        var recordTitle = warehouse.Name;

        InspectorField Field(string label, string? value) => CreateInspectorField(recordKey, recordTitle, label, value);

        var fields = new List<InspectorField>
        {
            Field("Location", warehouse.Location ?? "-"),
            Field("Responsible", warehouse.LegacyResponsibleName ?? "-"),
            Field("Status", warehouse.Status ?? "-"),
            Field("Qualified", warehouse.IsQualified ? "Yes" : "No")
        };

        return new ModuleRecord(
            recordKey,
            recordTitle,
            warehouse.Name,
            warehouse.Status,
            warehouse.Note,
            fields,
            null,
            warehouse.Id);
    }

    public sealed class WarehouseZoneSummaryItem
    {
        public WarehouseZoneSummaryItem(
            int partId,
            string partName,
            string partCode,
            string zoneLabel,
            ZoneClassification classification,
            int quantity,
            int? minimum,
            int? maximum)
        {
            PartId = partId;
            PartName = partName;
            PartCode = partCode;
            ZoneLabel = zoneLabel;
            Classification = classification;
            Quantity = quantity;
            Minimum = minimum;
            Maximum = maximum;
        }

        public int PartId { get; }

        public string PartName { get; }

        public string PartCode { get; }

        public string ZoneLabel { get; }

        public ZoneClassification Classification { get; }

        public int Quantity { get; }

        public int? Minimum { get; }

        public int? Maximum { get; }
    }

    public enum ZoneClassification
    {
        Critical,
        Warning,
        Healthy,
        Overflow
    }
    /// <summary>
    /// Represents the warehouse editor value.
    /// </summary>

    public sealed partial class WarehouseEditor : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _location = string.Empty;

        [ObservableProperty]
        private string _status = "qualified";

        [ObservableProperty]
        private string _responsible = string.Empty;

        [ObservableProperty]
        private string _note = string.Empty;

        [ObservableProperty]
        private string _qrCode = string.Empty;

        [ObservableProperty]
        private string _climateMode = string.Empty;

        [ObservableProperty]
        private bool _isQualified = true;

        [ObservableProperty]
        private DateTime? _lastQualified = DateTime.UtcNow.Date;
        /// <summary>
        /// Executes the create empty operation.
        /// </summary>

        public static WarehouseEditor CreateEmpty() => new();
        /// <summary>
        /// Executes the create for new operation.
        /// </summary>

        public static WarehouseEditor CreateForNew(string normalizedStatus)
            => new() { Status = normalizedStatus, IsQualified = normalizedStatus == "qualified" };
        /// <summary>
        /// Executes the from warehouse operation.
        /// </summary>

        public static WarehouseEditor FromWarehouse(Warehouse warehouse, Func<string?, string> normalizer)
        {
            return new WarehouseEditor
            {
                Id = warehouse.Id,
                Name = warehouse.Name ?? string.Empty,
                Location = warehouse.Location ?? string.Empty,
                Status = normalizer(warehouse.Status),
                Responsible = warehouse.LegacyResponsibleName ?? string.Empty,
                Note = warehouse.Note ?? string.Empty,
                QrCode = warehouse.QrCode ?? string.Empty,
                ClimateMode = warehouse.ClimateMode ?? string.Empty,
                IsQualified = warehouse.IsQualified,
                LastQualified = warehouse.LastQualified
            };
        }
        /// <summary>
        /// Executes the clone operation.
        /// </summary>

        public WarehouseEditor Clone()
            => new()
            {
                Id = Id,
                Name = Name,
                Location = Location,
                Status = Status,
                Responsible = Responsible,
                Note = Note,
                QrCode = QrCode,
                ClimateMode = ClimateMode,
                IsQualified = IsQualified,
                LastQualified = LastQualified
            };
        /// <summary>
        /// Executes the to warehouse operation.
        /// </summary>

        public Warehouse ToWarehouse(Warehouse? existing)
        {
            var warehouse = existing is null ? new Warehouse() : existing;
            warehouse.Id = Id;
            warehouse.Name = Name?.Trim() ?? string.Empty;
            warehouse.Location = Location?.Trim() ?? string.Empty;
            warehouse.Status = Status?.Trim() ?? string.Empty;
            warehouse.LegacyResponsibleName = Responsible?.Trim() ?? string.Empty;
            warehouse.Note = Note?.Trim() ?? string.Empty;
            warehouse.QrCode = QrCode?.Trim() ?? string.Empty;
            warehouse.ClimateMode = ClimateMode?.Trim() ?? string.Empty;
            warehouse.IsQualified = IsQualified;
            warehouse.LastQualified = LastQualified;
            return warehouse;
        }
    }
}
