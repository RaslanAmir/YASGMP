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

public sealed partial class PartsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public new const string ModuleKey = "Parts";

    private readonly IPartCrudService _partService;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IFilePicker _filePicker;
    private readonly IAuthContext _authContext;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private Part? _loadedPart;
    private PartEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;

    public PartsModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IPartCrudService partService,
        IAttachmentWorkflowService attachmentWorkflow,
        IFilePicker filePicker,
        IAuthContext authContext,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Parts & Stock", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        _partService = partService ?? throw new ArgumentNullException(nameof(partService));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));

        Editor = PartEditor.CreateEmpty();
        StatusOptions = new ReadOnlyCollection<string>(new[] { "active", "inactive", "blocked" });
        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
    }

    [ObservableProperty]
    private PartEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;

    [ObservableProperty]
    private IReadOnlyList<Supplier> _supplierOptions = Array.Empty<Supplier>();

    [ObservableProperty]
    private string _stockHealthMessage = string.Empty;

    public IReadOnlyList<string> StatusOptions { get; }

    public IAsyncRelayCommand AttachDocumentCommand { get; }

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
                Status = "active",
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
                Status = "inactive",
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
    }

    protected override Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        switch (mode)
        {
            case FormMode.Add:
                _snapshot = null;
                _loadedPart = null;
                SetEditor(PartEditor.CreateForNew(_partService.NormalizeStatus("active")));
                StockHealthMessage = "";
                break;
            case FormMode.Update:
                _snapshot = Editor.Clone();
                break;
            case FormMode.View:
                if (_loadedPart is not null)
                {
                    LoadEditor(_loadedPart);
                    UpdateStockHealth();
                }
                break;
        }

        UpdateAttachmentCommandState();
        return Task.CompletedTask;
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
        try
        {
            if (Mode == FormMode.Add)
            {
                var id = await _partService.CreateAsync(part, context).ConfigureAwait(false);
                part.Id = id;
                adapterResult = part;
            }
            else if (Mode == FormMode.Update)
            {
                part.Id = _loadedPart!.Id;
                await _partService.UpdateAsync(part, context).ConfigureAwait(false);
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

        _loadedPart = part;
        LoadEditor(part);
        UpdateStockHealth();
        UpdateAttachmentCommandState();

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "parts",
            recordId: adapterResult.Id,
            signatureId: adapterResult.DigitalSignatureId,
            signatureHash: adapterResult.DigitalSignature,
            method: context.SignatureMethod,
            status: context.SignatureStatus,
            note: context.SignatureNote,
            signedAt: signatureResult.Signature.SignedAt,
            deviceInfo: context.DeviceInfo,
            ipAddress: context.Ip,
            sessionId: context.SessionId);

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

    private async Task EnsureSuppliersAsync()
    {
        if (SupplierOptions.Count > 0)
        {
            return;
        }

        var suppliers = await Database.GetAllSuppliersAsync().ConfigureAwait(false) ?? new List<Supplier>();
        SupplierOptions = new ReadOnlyCollection<Supplier>(suppliers);
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
    }

    private void SetEditor(PartEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateStockHealth();
    }

    private bool CanAttachDocument()
        => !IsBusy
           && !IsEditorEnabled
           && _loadedPart is not null
           && _loadedPart.Id > 0;

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

    private static ModuleRecord ToRecord(Part part)
    {
        var fields = new List<InspectorField>
        {
            new("SKU", part.Sku ?? part.Code),
            new("Supplier", part.DefaultSupplierName ?? "-"),
            new("Stock", part.Stock?.ToString(CultureInfo.InvariantCulture) ?? "-"),
            new("Min Stock", part.MinStockAlert?.ToString(CultureInfo.InvariantCulture) ?? "-"),
            new("Location", part.Location ?? "-"),
            new("Status", part.Status ?? "-")
        };

        return new ModuleRecord(
            part.Id.ToString(CultureInfo.InvariantCulture),
            part.Name,
            part.Code,
            part.Status,
            part.Description,
            fields,
            SuppliersModuleViewModel.ModuleKey,
            part.DefaultSupplierId);
    }

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

        public bool IsBelowMinimum => MinStockAlert.HasValue && Stock.HasValue && Stock.Value < MinStockAlert.Value;

        public static PartEditor CreateEmpty() => new();

        public static PartEditor CreateForNew(string normalizedStatus)
            => new() { Status = normalizedStatus };

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
