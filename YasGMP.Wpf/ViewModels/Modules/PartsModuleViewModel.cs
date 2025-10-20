using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
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
/// Represents the parts module view model value.
/// </summary>

public sealed partial class PartsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>
    /// Represents the module key value.
    /// </summary>
    public new const string ModuleKey = "Parts";

    private readonly IPartCrudService _partService;
    private readonly IInventoryTransactionService _inventoryService;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IFilePicker _filePicker;
    private readonly IAuthContext _authContext;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private readonly ILocalizationService _localization;
    private readonly ObservableCollection<ZoneSummaryItem> _zoneSummaries;
    private readonly ObservableCollection<InventoryTransactionReportItem> _recentTransactions;
    private readonly ObservableCollection<string> _transactionAlerts;
    private readonly ICollectionView _zoneSummariesView;
    private readonly string _zoneAllLabel;
    private readonly Dictionary<ZoneClassification, string> _zoneLabels;
    private Part? _loadedPart;
    private PartEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;
    /// <summary>
    /// Initializes a new instance of the PartsModuleViewModel class.
    /// </summary>

    public PartsModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IPartCrudService partService,
        IInventoryTransactionService inventoryService,
        IAttachmentWorkflowService attachmentWorkflow,
        IFilePicker filePicker,
        IAuthContext authContext,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, localization.GetString("Module.Title.PartsStock"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _partService = partService ?? throw new ArgumentNullException(nameof(partService));
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        Editor = PartEditor.CreateEmpty();
        _zoneSummaries = new ObservableCollection<ZoneSummaryItem>();
        _recentTransactions = new ObservableCollection<InventoryTransactionReportItem>();
        _transactionAlerts = new ObservableCollection<string>();
        _zoneLabels = new Dictionary<ZoneClassification, string>
        {
            [ZoneClassification.Critical] = GetZoneLabel("Module.Parts.ZoneFilter.Critical", "Critical"),
            [ZoneClassification.Warning] = GetZoneLabel("Module.Parts.ZoneFilter.Warning", "Warning"),
            [ZoneClassification.Healthy] = GetZoneLabel("Module.Parts.ZoneFilter.Healthy", "Healthy"),
            [ZoneClassification.Overflow] = GetZoneLabel("Module.Parts.ZoneFilter.Overflow", "Overflow")
        };
        _zoneAllLabel = GetZoneLabel("Module.Parts.ZoneFilter.All", "All Zones");
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
        StatusOptions = new ReadOnlyCollection<string>(new[]
        {
            _localization.GetString("Module.Parts.Status.Active"),
            _localization.GetString("Module.Parts.Status.Inactive"),
            _localization.GetString("Module.Parts.Status.Blocked")
        });
        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
        ReceiveStockCommand = new AsyncRelayCommand(() => ExecuteInventoryTransactionAsync(InventoryTransactionType.Receive), CanExecuteInventoryTransaction);
        IssueStockCommand = new AsyncRelayCommand(() => ExecuteInventoryTransactionAsync(InventoryTransactionType.Issue), CanExecuteInventoryTransaction);
        AdjustStockCommand = new AsyncRelayCommand(() => ExecuteInventoryTransactionAsync(InventoryTransactionType.Adjust), CanExecuteInventoryTransaction);
    }

    [ObservableProperty]
    private PartEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;

    [ObservableProperty]
    private IReadOnlyList<Supplier> _supplierOptions = Array.Empty<Supplier>();

    [ObservableProperty]
    private string _stockHealthMessage = string.Empty;
    /// <summary>
    /// Gets or sets the status options.
    /// </summary>

    public IReadOnlyList<string> StatusOptions { get; }
    /// <summary>
    /// Gets or sets the attach document command.
    /// </summary>

    public IAsyncRelayCommand AttachDocumentCommand { get; }

    public ObservableCollection<ZoneSummaryItem> ZoneSummaries => _zoneSummaries;

    public ICollectionView ZoneSummariesView => _zoneSummariesView;

    public ObservableCollection<InventoryTransactionReportItem> RecentTransactions => _recentTransactions;

    public ObservableCollection<string> TransactionAlerts => _transactionAlerts;

    public ObservableCollection<string> ZoneFilters { get; }

    [ObservableProperty]
    private string _selectedZoneFilter = string.Empty;

    public IAsyncRelayCommand ReceiveStockCommand { get; }

    public IAsyncRelayCommand IssueStockCommand { get; }

    public IAsyncRelayCommand AdjustStockCommand { get; }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        await EnsureSuppliersAsync().ConfigureAwait(false);
        var parts = await _partService.GetAllAsync().ConfigureAwait(false);
        return parts.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var sample = new[]
        {
            new Part
            {
                Id = 1,
                Code = "PRT-001",
                Name = "Sterile Tubing",
                Status = StatusOptions[0],
                Description = "Silicone transfer tubing",
                Location = "Main Warehouse",
                Stock = 120,
                MinStockAlert = 50,
                DefaultSupplierName = "Contoso Pharma"
            },
            new Part
            {
                Id = 2,
                Code = "FLT-010",
                Name = "HEPA Filter",
                Status = StatusOptions[1],
                Description = "Spare HEPA H14",
                Location = "Cleanroom",
                Stock = 10,
                MinStockAlert = 5,
                DefaultSupplierName = "Globex"
            }
        };

        return sample.Select(ToRecord).ToList();
    }

    protected override async Task OnActivatedAsync(object? parameter)
    {
        await EnsureSuppliersAsync().ConfigureAwait(false);
    }

    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var parts = await _partService.GetAllAsync().ConfigureAwait(false);
        var items = parts.Select(part =>
        {
            var key = part.Id.ToString(CultureInfo.InvariantCulture);
            var label = string.IsNullOrWhiteSpace(part.Name) ? key : part.Name;
            var descriptionParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(part.Code))
            {
                descriptionParts.Add(part.Code!);
            }

            if (!string.IsNullOrWhiteSpace(part.Location))
            {
                descriptionParts.Add(part.Location!);
            }

            if (part.Stock.HasValue)
            {
                descriptionParts.Add($"Stock: {part.Stock.Value}");
            }

            var description = descriptionParts.Count > 0
                ? string.Join(" • ", descriptionParts)
                : null;

            return new CflItem(key, label, description);
        }).ToList();

        return new CflRequest("Select Part", items);
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

        StatusMessage = $"Filtered {Title} by \"{SearchText}\".";
        return Task.CompletedTask;
    }

    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedPart = null;
            SetEditor(PartEditor.CreateEmpty());
            StockHealthMessage = string.Empty;
            UpdateAttachmentCommandState();
            await ClearInventoryReportsAsync().ConfigureAwait(false);
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

        var part = await _partService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (part is null)
        {
            StatusMessage = $"Unable to locate part #{id}.";
            return;
        }

        _loadedPart = part;
        LoadEditor(part);
        UpdateStockHealth();
        UpdateAttachmentCommandState();
        await RefreshInventoryReportsAsync(part.Id).ConfigureAwait(false);
    }

    protected override async Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        switch (mode)
        {
            case FormMode.Add:
                _snapshot = null;
                _loadedPart = null;
                SetEditor(PartEditor.CreateForNew(_partService.NormalizeStatus("active")));
                StockHealthMessage = "";
                await ClearInventoryReportsAsync().ConfigureAwait(false);
                break;
            case FormMode.Update:
                _snapshot = Editor.Clone();
                break;
            case FormMode.View:
                if (_loadedPart is not null)
                {
                    LoadEditor(_loadedPart);
                    UpdateStockHealth();
                    await RefreshInventoryReportsAsync(_loadedPart.Id).ConfigureAwait(false);
                }
                break;
        }

        UpdateAttachmentCommandState();
    }

    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var part = Editor.ToPart(_loadedPart);
        var errors = new List<string>();
        try
        {
            _partService.Validate(part);
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
        if (Mode == FormMode.Update && _loadedPart is null)
        {
            StatusMessage = "Select a part before saving.";
            return false;
        }

        var part = Editor.ToPart(_loadedPart);
        part.Status = _partService.NormalizeStatus(part.Status);
        if (SupplierOptions.Count > 0 && part.DefaultSupplierId.HasValue)
        {
            var supplier = SupplierOptions.FirstOrDefault(s => s.Id == part.DefaultSupplierId.Value);
            if (supplier is not null)
            {
                part.DefaultSupplierName = supplier.Name;
            }
        }

        var recordId = Mode == FormMode.Update ? _loadedPart!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("parts", recordId))
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

        part.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;

        var context = PartCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        Part adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _partService.CreateAsync(part, context).ConfigureAwait(false);
                part.Id = saveResult.Id;
                adapterResult = part;
            }
            else if (Mode == FormMode.Update)
            {
                part.Id = _loadedPart!.Id;
                saveResult = await _partService.UpdateAsync(part, context).ConfigureAwait(false);
                adapterResult = part;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist part: {ex.Message}", ex);
        }

        if (saveResult.SignatureMetadata?.Id is { } signatureId)
        {
            adapterResult.DigitalSignatureId = signatureId;
        }

        _loadedPart = part;
        LoadEditor(part);
        UpdateStockHealth();
        UpdateAttachmentCommandState();

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "parts",
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
            if (_loadedPart is not null)
            {
                LoadEditor(_loadedPart);
            }
            else
            {
                SetEditor(PartEditor.CreateEmpty());
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

    partial void OnEditorChanging(PartEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(PartEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged += OnEditorPropertyChanged;
    }

    partial void OnSelectedZoneFilterChanged(string value)
    {
        _zoneSummariesView.Refresh();
    }

    private bool FilterZoneSummary(object obj)
    {
        if (obj is not ZoneSummaryItem item)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SelectedZoneFilter) || string.Equals(SelectedZoneFilter, _zoneAllLabel, StringComparison.Ordinal))
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

    private async Task RefreshInventoryReportsAsync(int partId)
    {
        List<(int warehouseId, string warehouseName, int quantity, int? min, int? max)> stockLevels;
        try
        {
            stockLevels = await Database.GetStockLevelsForPartAsync(partId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load stock levels: {ex.Message}";
            await ClearInventoryReportsAsync().ConfigureAwait(false);
            return;
        }

        var warehouseLookup = stockLevels.ToDictionary(s => s.warehouseId, s => s.warehouseName);

        await RunOnUiThreadAsync(() =>
        {
            _zoneSummaries.Clear();
            _transactionAlerts.Clear();

            foreach (var level in stockLevels)
            {
                var classification = ClassifyZone(level.quantity, level.min, level.max);
                var zoneLabel = _zoneLabels.TryGetValue(classification, out var label)
                    ? label
                    : classification.ToString();

                _zoneSummaries.Add(new ZoneSummaryItem(
                    level.warehouseId,
                    classification,
                    zoneLabel,
                    level.warehouseName,
                    level.quantity,
                    level.min,
                    level.max));

                if (classification == ZoneClassification.Critical)
                {
                    var minText = level.min.HasValue ? level.min.Value.ToString(CultureInfo.InvariantCulture) : "n/a";
                    _transactionAlerts.Add($"{level.warehouseName} below minimum ({level.quantity}/{minText}).");
                }
                else if (classification == ZoneClassification.Overflow)
                {
                    var maxText = level.max.HasValue ? level.max.Value.ToString(CultureInfo.InvariantCulture) : "n/a";
                    _transactionAlerts.Add($"{level.warehouseName} above maximum ({level.quantity}/{maxText}).");
                }
            }

            _zoneSummariesView.Refresh();
        }).ConfigureAwait(false);

        DataTable history;
        try
        {
            history = await Database.GetInventoryTransactionsForPartAsync(partId, 50).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load inventory history: {ex.Message}";
            await RunOnUiThreadAsync(() => _recentTransactions.Clear()).ConfigureAwait(false);
            return;
        }

        await RunOnUiThreadAsync(() =>
        {
            _recentTransactions.Clear();

            foreach (DataRow row in history.Rows)
            {
                var dateValue = row["transaction_date"];
                var transactionDate = dateValue switch
                {
                    DateTime dt => dt,
                    _ => DateTime.TryParse(dateValue?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
                        ? parsed
                        : DateTime.MinValue
                };

                var typeValue = row.Table.Columns.Contains("transaction_type")
                    ? row["transaction_type"]?.ToString() ?? string.Empty
                    : string.Empty;

                var quantityValue = row.Table.Columns.Contains("quantity") && row["quantity"] != DBNull.Value
                    ? Convert.ToInt32(row["quantity"])
                    : 0;

                var warehouseId = row.Table.Columns.Contains("warehouse_id") && row["warehouse_id"] != DBNull.Value
                    ? Convert.ToInt32(row["warehouse_id"])
                    : 0;

                var warehouseName = warehouseLookup.TryGetValue(warehouseId, out var name)
                    ? name
                    : $"WH-{warehouseId}";

                var document = row.Table.Columns.Contains("related_document") && row["related_document"] != DBNull.Value
                    ? row["related_document"]?.ToString()
                    : null;

                var note = row.Table.Columns.Contains("note") && row["note"] != DBNull.Value
                    ? row["note"]?.ToString()
                    : null;

                int? performedBy = row.Table.Columns.Contains("performed_by_id") && row["performed_by_id"] != DBNull.Value
                    ? Convert.ToInt32(row["performed_by_id"])
                    : null;

                _recentTransactions.Add(new InventoryTransactionReportItem(
                    transactionDate,
                    typeValue,
                    quantityValue,
                    warehouseName,
                    document,
                    note,
                    performedBy));
            }
        }).ConfigureAwait(false);
    }

    private Task ClearInventoryReportsAsync()
        => RunOnUiThreadAsync(() =>
        {
            _zoneSummaries.Clear();
            _recentTransactions.Clear();
            _transactionAlerts.Clear();
            _zoneSummariesView.Refresh();
        });

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

    public sealed class ZoneSummaryItem
    {
        public ZoneSummaryItem(int warehouseId, ZoneClassification classification, string zoneLabel, string warehouse, int quantity, int? minimum, int? maximum)
        {
            WarehouseId = warehouseId;
            Classification = classification;
            ZoneLabel = zoneLabel;
            Warehouse = warehouse;
            Quantity = quantity;
            Minimum = minimum;
            Maximum = maximum;
        }

        public int WarehouseId { get; }

        public ZoneClassification Classification { get; }

        public string ZoneLabel { get; }

        public string Warehouse { get; }

        public int Quantity { get; }

        public int? Minimum { get; }

        public int? Maximum { get; }
    }

    public sealed record InventoryTransactionReportItem(
        DateTime TransactionDate,
        string TransactionType,
        int Quantity,
        string Warehouse,
        string? Document,
        string? Note,
        int? PerformedById);

    public enum ZoneClassification
    {
        Critical,
        Warning,
        Healthy,
        Overflow
    }

    private async Task EnsureSuppliersAsync()
    {
        if (SupplierOptions.Count > 0)
        {
            return;
        }

        var suppliers = await Database.GetAllSuppliersAsync().ConfigureAwait(false) ?? new List<Supplier>();
        SupplierOptions = new ReadOnlyCollection<Supplier>(suppliers);
    }

    private string GetZoneLabel(string resourceKey, string fallback)
    {
        try
        {
            var value = _localization.GetString(resourceKey);
            return string.IsNullOrWhiteSpace(value) || string.Equals(value, resourceKey, StringComparison.Ordinal)
                ? fallback
                : value;
        }
        catch
        {
            return fallback;
        }
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

        if (e.PropertyName is nameof(PartEditor.Stock)
            or nameof(PartEditor.MinStockAlert)
            or nameof(PartEditor.LowWarehouseCount)
            or nameof(PartEditor.IsWarehouseStockCritical))
        {
            UpdateStockHealth();
        }
    }

    private void LoadEditor(Part part)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = PartEditor.FromPart(part, _partService.NormalizeStatus);
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateStockHealth();
        NotifyInventoryCommands();
    }

    private void SetEditor(PartEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateStockHealth();
        NotifyInventoryCommands();
    }

    private bool CanAttachDocument()
        => !IsBusy
           && !IsEditorEnabled
           && _loadedPart is not null
           && _loadedPart.Id > 0;

    private bool CanExecuteInventoryTransaction()
        => !IsBusy
           && !IsEditorEnabled
           && _loadedPart is { Id: > 0 }
           && Mode is FormMode.View;

    private async Task AttachDocumentAsync()
    {
        if (_loadedPart is null || _loadedPart.Id <= 0)
        {
            return;
        }

        var files = await _filePicker.PickFilesAsync(new FilePickerRequest
        {
            Title = "Attach file to part"
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
                EntityType = "parts",
                EntityId = _loadedPart.Id,
                FileName = file.FileName,
                ContentType = file.ContentType,
                UploadedById = _authContext.CurrentUser?.Id,
                SourceIp = _authContext.CurrentIpAddress,
                SourceHost = _authContext.CurrentDeviceInfo,
                Reason = $"Attached via WPF on {DateTime.UtcNow:O}"
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

    private void UpdateAttachmentCommandState()
        => AttachDocumentCommand.NotifyCanExecuteChanged();

    private void UpdateStockHealth()
    {
        if (Editor.IsWarehouseStockCritical || Editor.IsBelowMinimum)
        {
            StockHealthMessage = Editor.LowWarehouseCount > 0
                ? $"{Editor.LowWarehouseCount} warehouse location(s) below minimum threshold."
                : "Stock below configured minimum.";
        }
        else if (!string.IsNullOrWhiteSpace(Editor.WarehouseSummary))
        {
            StockHealthMessage = $"Distribution: {Editor.WarehouseSummary}.";
        }
        else
        {
            StockHealthMessage = string.Empty;
        }
    }

    private void NotifyInventoryCommands()
    {
        ReceiveStockCommand.NotifyCanExecuteChanged();
        IssueStockCommand.NotifyCanExecuteChanged();
        AdjustStockCommand.NotifyCanExecuteChanged();
    }

    private async Task ExecuteInventoryTransactionAsync(InventoryTransactionType type)
    {
        if (_loadedPart is null)
        {
            return;
        }

        List<Warehouse> warehouses;
        try
        {
            warehouses = await Database.GetWarehousesAsync().ConfigureAwait(false) ?? new List<Warehouse>();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load warehouses: {ex.Message}";
            return;
        }

        if (warehouses.Count == 0)
        {
            StatusMessage = "No warehouses available for stock transaction.";
            return;
        }

        InventoryTransactionRequest? submittedRequest = null;
        ElectronicSignatureDialogResult? submittedSignature = null;

        await RunOnUiThreadAsync(() =>
        {
            var displayName = string.IsNullOrWhiteSpace(_loadedPart.Name)
                ? _loadedPart.Code ?? $"Part #{_loadedPart.Id}"
                : $"{_loadedPart.Name} ({_loadedPart.Code})";

            var dialogVm = new StockTransactionDialogViewModel(
                _loadedPart.Id,
                displayName ?? $"Part #{_loadedPart.Id}",
                type,
                _signatureDialog,
                async (request, signature) =>
                {
                    var context = CreateInventoryContext(signature);
                    await _inventoryService.ExecuteAsync(request, context).ConfigureAwait(false);
                });

            dialogVm.LoadWarehouses(warehouses);

            var dialog = new StockTransactionDialog
            {
                Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current?.MainWindow,
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

        var selectedWarehouseName = warehouses
            .FirstOrDefault(w => w.Id == submittedRequest.Value.WarehouseId)?.Name
            ?? $"WH-{submittedRequest.Value.WarehouseId}";

        await RefreshInventoryStateAsync(type, submittedRequest.Value, selectedWarehouseName).ConfigureAwait(false);
    }

    private InventoryTransactionContext CreateInventoryContext(ElectronicSignatureDialogResult signature)
    {
        var userId = _authContext.CurrentUser?.Id ?? 0;
        return new InventoryTransactionContext(
            userId,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signature);
    }

    private async Task RefreshInventoryStateAsync(
        InventoryTransactionType type,
        InventoryTransactionRequest request,
        string warehouseName)
    {
        try
        {
            var part = await _partService.TryGetByIdAsync(request.PartId).ConfigureAwait(false);
            if (part is not null)
            {
                _loadedPart = part;
                await RunOnUiThreadAsync(() =>
                {
                    LoadEditor(part);
                    UpdateAttachmentCommandState();
                }).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to reload part: {ex.Message}";
        }

        await RefreshInventoryReportsAsync(request.PartId).ConfigureAwait(false);

        var delta = type switch
        {
            InventoryTransactionType.Adjust => request.AdjustmentDelta ?? 0,
            InventoryTransactionType.Issue => -request.Quantity,
            _ => request.Quantity
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

        var message = $"{verb} {deltaDisplay} units at {warehouseName}.";
        StatusMessage = message;
        _shellInteraction.UpdateStatus(message);

        NotifyInventoryCommands();
    }

    private ModuleRecord ToRecord(Part part)
    {
        var recordKey = part.Id.ToString(CultureInfo.InvariantCulture);
        var recordTitle = string.IsNullOrWhiteSpace(part.Name) ? recordKey : part.Name;

        InspectorField Field(string label, string? value) => CreateInspectorField(recordKey, recordTitle, label, value);

        var fields = new List<InspectorField>
        {
            Field("SKU", part.Sku ?? part.Code),
            Field("Supplier", part.DefaultSupplierName ?? "-"),
            Field("Stock", part.Stock?.ToString(CultureInfo.InvariantCulture) ?? "-"),
            Field("Min Stock", part.MinStockAlert?.ToString(CultureInfo.InvariantCulture) ?? "-"),
            Field("Location", part.Location ?? "-"),
            Field("Status", part.Status ?? "-")
        };

        return new ModuleRecord(
            recordKey,
            recordTitle,
            part.Code,
            part.Status,
            part.Description,
            fields,
            SuppliersModuleViewModel.ModuleKey,
            part.DefaultSupplierId);
    }
    /// <summary>
    /// Represents the part editor value.
    /// </summary>

    public sealed partial class PartEditor : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _code = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _category = string.Empty;

        [ObservableProperty]
        private string _status = "active";

        [ObservableProperty]
        private int? _stock;

        [ObservableProperty]
        private int? _minStockAlert;

        [ObservableProperty]
        private string _location = string.Empty;

        [ObservableProperty]
        private int? _defaultSupplierId;

        [ObservableProperty]
        private string _defaultSupplierName = string.Empty;

        [ObservableProperty]
        private string _sku = string.Empty;

        [ObservableProperty]
        private decimal? _price;

        [ObservableProperty]
        private int _lowWarehouseCount;

        [ObservableProperty]
        private string _warehouseSummary = string.Empty;

        [ObservableProperty]
        private bool _isWarehouseStockCritical;
        /// <summary>
        /// Gets or sets the is below minimum.
        /// </summary>

        public bool IsBelowMinimum => MinStockAlert.HasValue && Stock.HasValue && Stock.Value < MinStockAlert.Value;
        /// <summary>
        /// Executes the create empty operation.
        /// </summary>

        public static PartEditor CreateEmpty() => new();
        /// <summary>
        /// Executes the create for new operation.
        /// </summary>

        public static PartEditor CreateForNew(string normalizedStatus)
            => new() { Status = normalizedStatus };
        /// <summary>
        /// Executes the from part operation.
        /// </summary>

        public static PartEditor FromPart(Part part, Func<string?, string> normalizer)
        {
            return new PartEditor
            {
                Id = part.Id,
                Code = part.Code ?? string.Empty,
                Name = part.Name ?? string.Empty,
                Description = part.Description ?? string.Empty,
                Category = part.Category ?? string.Empty,
                Status = normalizer(part.Status),
                Stock = part.Stock,
                MinStockAlert = part.MinStockAlert,
                Location = part.Location ?? string.Empty,
                DefaultSupplierId = part.DefaultSupplierId,
                DefaultSupplierName = part.DefaultSupplierName ?? string.Empty,
                Sku = part.Sku ?? string.Empty,
                Price = part.Price,
                LowWarehouseCount = part.LowWarehouseCount,
                WarehouseSummary = part.WarehouseSummary ?? string.Empty,
                IsWarehouseStockCritical = part.IsWarehouseStockCritical
            };
        }
        /// <summary>
        /// Executes the clone operation.
        /// </summary>

        public PartEditor Clone()
            => new()
            {
                Id = Id,
                Code = Code,
                Name = Name,
                Description = Description,
                Category = Category,
                Status = Status,
                Stock = Stock,
                MinStockAlert = MinStockAlert,
                Location = Location,
                DefaultSupplierId = DefaultSupplierId,
                DefaultSupplierName = DefaultSupplierName,
                Sku = Sku,
                Price = Price,
                LowWarehouseCount = LowWarehouseCount,
                WarehouseSummary = WarehouseSummary,
                IsWarehouseStockCritical = IsWarehouseStockCritical
            };
        /// <summary>
        /// Executes the to part operation.
        /// </summary>

        public Part ToPart(Part? existing)
        {
            var part = existing is null ? new Part() : existing;
            part.Id = Id;
            part.Code = Code?.Trim() ?? string.Empty;
            part.Name = Name?.Trim() ?? string.Empty;
            part.Description = Description?.Trim();
            part.Category = Category?.Trim();
            part.Status = Status?.Trim();
            part.Stock = Stock;
            part.MinStockAlert = MinStockAlert;
            part.Location = Location?.Trim();
            part.DefaultSupplierId = DefaultSupplierId;
            part.DefaultSupplierName = DefaultSupplierName?.Trim();
            part.Sku = Sku?.Trim();
            part.Price = Price;
            part.LowWarehouseCount = LowWarehouseCount;
            part.WarehouseSummary = WarehouseSummary ?? string.Empty;
            part.IsWarehouseStockCritical = IsWarehouseStockCritical;
            return part;
        }
    }
}
