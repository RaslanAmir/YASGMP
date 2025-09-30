using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed partial class WarehouseModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public new const string ModuleKey = "Warehouse";

    private readonly IWarehouseCrudService _warehouseService;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IFilePicker _filePicker;
    private readonly IAuthContext _authContext;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private Warehouse? _loadedWarehouse;
    private WarehouseEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;

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
        IModuleNavigationService navigation)
        : base(ModuleKey, "Warehouse", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        _warehouseService = warehouseService ?? throw new ArgumentNullException(nameof(warehouseService));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));

        Editor = WarehouseEditor.CreateEmpty();
        StatusOptions = new ReadOnlyCollection<string>(new[] { "qualified", "in-qualification", "maintenance", "inactive" });
        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
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

    public IReadOnlyList<string> StatusOptions { get; }

    public IAsyncRelayCommand AttachDocumentCommand { get; }

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
                Status = "qualified",
                LegacyResponsibleName = "John Doe",
                Note = "Primary GMP warehouse"
            },
            new Warehouse
            {
                Id = 2,
                Name = "Cold Storage",
                Location = "Building C",
                Status = "qualified",
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

        StatusMessage = $"Filtered {Title} by \"{SearchText}\".";
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

        try
        {
            if (Mode == FormMode.Add)
            {
                await _warehouseService.CreateAsync(warehouse, context).ConfigureAwait(false);
            }
            else if (Mode == FormMode.Update)
            {
                warehouse.Id = _loadedWarehouse!.Id;
                await _warehouseService.UpdateAsync(warehouse, context).ConfigureAwait(false);
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

        signatureResult.Signature.RecordId = warehouse.Id;

        try
        {
            await _signatureDialog.PersistSignatureAsync(signatureResult).ConfigureAwait(false);
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
            StockSnapshot = new ObservableCollection<WarehouseStockSnapshot>(stock);
            HasStockAlerts = StockSnapshot.Any(s => s.IsBelowMinimum || s.IsAboveMaximum);

            var movements = await _warehouseService.GetRecentMovementsAsync(warehouseId, 15).ConfigureAwait(false);
            RecentMovements = new ObservableCollection<InventoryMovementEntry>(movements);

            StatusMessage = HasStockAlerts
                ? $"{Title}: stock alerts detected for {StockSnapshot.Count(s => s.IsBelowMinimum)} item(s)."
                : $"{Title}: stock overview refreshed ({StockSnapshot.Count} tracked parts).";
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

    private void UpdateAttachmentCommandState()
        => AttachDocumentCommand.NotifyCanExecuteChanged();

    private static ModuleRecord ToRecord(Warehouse warehouse)
    {
        var fields = new List<InspectorField>
        {
            new("Location", warehouse.Location ?? "-"),
            new("Responsible", warehouse.LegacyResponsibleName ?? "-"),
            new("Status", warehouse.Status ?? "-"),
            new("Qualified", warehouse.IsQualified ? "Yes" : "No")
        };

        return new ModuleRecord(
            warehouse.Id.ToString(CultureInfo.InvariantCulture),
            warehouse.Name,
            warehouse.Name,
            warehouse.Status,
            warehouse.Note,
            fields,
            null,
            warehouse.Id);
    }

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

        public static WarehouseEditor CreateEmpty() => new();

        public static WarehouseEditor CreateForNew(string normalizedStatus)
            => new() { Status = normalizedStatus, IsQualified = normalizedStatus == "qualified" };

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
