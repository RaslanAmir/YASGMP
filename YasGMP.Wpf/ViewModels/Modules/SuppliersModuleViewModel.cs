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
/// <summary>
/// Represents the suppliers module view model value.
/// </summary>

public sealed partial class SuppliersModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>
    /// Represents the module key value.
    /// </summary>
    public const string ModuleKey = "Suppliers";

    private readonly ISupplierCrudService _supplierService;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IFilePicker _filePicker;
    private readonly IAuthContext _authContext;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private readonly ILocalizationService _localization;
    private Supplier? _loadedSupplier;
    private SupplierEditor? _snapshot;
    private bool _suppressDirtyNotifications;
    private int? _lastSavedSupplierId;
    /// <summary>
    /// Initializes a new instance of the SuppliersModuleViewModel class.
    /// </summary>

    public SuppliersModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ISupplierCrudService supplierService,
        IAttachmentWorkflowService attachmentWorkflow,
        IFilePicker filePicker,
        IAuthContext authContext,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, localization.GetString("Module.Title.Suppliers"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        Editor = SupplierEditor.CreateEmpty();
        StatusOptions = new ReadOnlyCollection<string>(new[]
        {
            _localization.GetString("Module.Suppliers.Status.Active"),
            _localization.GetString("Module.Suppliers.Status.Validated"),
            _localization.GetString("Module.Suppliers.Status.Qualified"),
            _localization.GetString("Module.Suppliers.Status.Suspended"),
            _localization.GetString("Module.Suppliers.Status.UnderReview"),
            _localization.GetString("Module.Suppliers.Status.PendingApproval"),
            _localization.GetString("Module.Suppliers.Status.Capa"),
            _localization.GetString("Module.Suppliers.Status.Expired"),
            _localization.GetString("Module.Suppliers.Status.Delisted"),
            _localization.GetString("Module.Suppliers.Status.Blacklisted"),
            _localization.GetString("Module.Suppliers.Status.Probation")
        });
        RiskOptions = new ReadOnlyCollection<string>(new[]
        {
            _localization.GetString("Module.Suppliers.Risk.Low"),
            _localization.GetString("Module.Suppliers.Risk.Moderate"),
            _localization.GetString("Module.Suppliers.Risk.Elevated"),
            _localization.GetString("Module.Suppliers.Risk.High"),
            _localization.GetString("Module.Suppliers.Risk.Critical")
        });
        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
    }

    [ObservableProperty]
    private SupplierEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;

    [ObservableProperty]
    private IReadOnlyList<string> _statusOptions;

    [ObservableProperty]
    private IReadOnlyList<string> _riskOptions;
    /// <summary>
    /// Gets or sets the attach document command.
    /// </summary>

    public IAsyncRelayCommand AttachDocumentCommand { get; }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var suppliers = await _supplierService.GetAllAsync().ConfigureAwait(false);
        var ordered = suppliers
            .OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(s => s.Id)
            .Select(ToRecord)
            .ToList();

        if (_lastSavedSupplierId.HasValue)
        {
            var savedKey = _lastSavedSupplierId.Value.ToString(CultureInfo.InvariantCulture);
            var index = ordered.FindIndex(r => r.Key == savedKey);
            if (index > 0)
            {
                var match = ordered[index];
                ordered.RemoveAt(index);
                ordered.Insert(0, match);
            }

            _lastSavedSupplierId = null;
        }

        return ordered;
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var sample = new[]
        {
            new Supplier
            {
                Id = 1,
                Name = "Contoso Calibration",
                SupplierType = "Calibration",
                Status = StatusOptions[0],
                Email = "support@contoso.example",
                Phone = "+385 91 111 222",
                RiskLevel = RiskOptions[1],
                Country = "Croatia",
                CooperationStart = DateTime.UtcNow.AddYears(-2),
                CooperationEnd = DateTime.UtcNow.AddYears(1)
            },
            new Supplier
            {
                Id = 2,
                Name = "Globex Servicers",
                SupplierType = "Maintenance",
                Status = StatusOptions[3],
                Email = "hq@globex.example",
                Phone = "+385 91 333 444",
                RiskLevel = RiskOptions[3],
                Country = "Austria"
            }
        };

        return sample.Select(ToRecord).ToList();
    }

    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedSupplier = null;
            SetEditor(SupplierEditor.CreateEmpty());
            UpdateAttachmentCommandState();
            return;
        }

        if (!int.TryParse(record.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return;
        }

        if (IsInEditMode)
        {
            return;
        }

        var supplier = await _supplierService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (supplier is null)
        {
            StatusMessage = $"Supplier #{id} could not be located.";
            return;
        }

        if (saveResult.SignatureMetadata?.Id is { } signatureId)
        {
            adapterResult.DigitalSignatureId = signatureId;
        }

        _loadedSupplier = supplier;
        LoadEditor(supplier);
        UpdateAttachmentCommandState();
    }

    protected override Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        switch (mode)
        {
            case FormMode.Add:
                _snapshot = null;
                _loadedSupplier = null;
                SetEditor(SupplierEditor.CreateForNew());
                break;
            case FormMode.Update:
                if (_loadedSupplier is not null)
                {
                    _snapshot = Editor.Clone();
                }
                break;
            case FormMode.Find:
            case FormMode.View:
                _snapshot = null;
                break;
        }

        UpdateAttachmentCommandState();
        return Task.CompletedTask;
    }

    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var supplier = Editor.ToSupplier(_loadedSupplier);
        var errors = new List<string>();

        try
        {
            _supplierService.Validate(supplier);
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(ex.Message);
        }
        catch (Exception ex)
        {
            errors.Add($"Unexpected validation error: {ex.Message}");
        }

        return await Task.FromResult<IReadOnlyList<string>>(errors).ConfigureAwait(false);
    }

    protected override async Task<bool> OnSaveAsync()
    {
        if (Mode == FormMode.Update && _loadedSupplier is null)
        {
            StatusMessage = "Select a supplier before saving.";
            return false;
        }

        var supplier = Editor.ToSupplier(_loadedSupplier);
        supplier.Status = _supplierService.NormalizeStatus(supplier.Status);

        var recordId = Mode == FormMode.Update ? _loadedSupplier!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("suppliers", recordId))
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

        supplier.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;

        var context = SupplierCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        Supplier adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _supplierService.CreateAsync(supplier, context).ConfigureAwait(false);
                supplier.Id = saveResult.Id;
                adapterResult = supplier;
            }
            else if (Mode == FormMode.Update)
            {
                supplier.Id = _loadedSupplier!.Id;
                saveResult = await _supplierService.UpdateAsync(supplier, context).ConfigureAwait(false);
                adapterResult = supplier;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist supplier: {ex.Message}", ex);
        }

        _loadedSupplier = supplier;
        _lastSavedSupplierId = supplier.Id;

        LoadEditor(supplier);
        UpdateAttachmentCommandState();

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "suppliers",
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
            if (_loadedSupplier is not null)
            {
                LoadEditor(_loadedSupplier);
            }
            else
            {
                SetEditor(SupplierEditor.CreateEmpty());
            }
        }
        else if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
        }

        UpdateAttachmentCommandState();
    }

    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var suppliers = await _supplierService.GetAllAsync().ConfigureAwait(false);
        var items = suppliers.Select(supplier =>
        {
            var key = supplier.Id.ToString(CultureInfo.InvariantCulture);
            var descriptionParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(supplier.SupplierType))
            {
                descriptionParts.Add(supplier.SupplierType);
            }

            if (!string.IsNullOrWhiteSpace(supplier.Country))
            {
                descriptionParts.Add(supplier.Country);
            }

            if (!string.IsNullOrWhiteSpace(supplier.Email))
            {
                descriptionParts.Add(supplier.Email);
            }

            var description = descriptionParts.Count > 0 ? string.Join(" • ", descriptionParts) : null;
            return new CflItem(key, supplier.Name, description);
        }).ToList();

        return new CflRequest("Select Supplier", items);
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

        StatusMessage = $"Filtered suppliers by \"{SearchText}\".";
        return Task.CompletedTask;
    }

    protected override bool MatchesSearch(ModuleRecord record, string searchText)
    {
        if (base.MatchesSearch(record, searchText))
        {
            return true;
        }

        return record.InspectorFields.Any(field => field.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is nameof(IsBusy) or nameof(Mode) or nameof(SelectedRecord) or nameof(IsDirty))
        {
            UpdateAttachmentCommandState();
        }
    }

    partial void OnEditorChanging(SupplierEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(SupplierEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged += OnEditorPropertyChanged;
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressDirtyNotifications)
        {
            return;
        }

        if (IsEditorEnabled)
        {
            MarkDirty();
        }
    }

    private void LoadEditor(Supplier supplier)
    {
        _suppressDirtyNotifications = true;
        Editor = SupplierEditor.FromSupplier(supplier);
        _suppressDirtyNotifications = false;
        ResetDirty();
    }

    private void SetEditor(SupplierEditor editor)
    {
        _suppressDirtyNotifications = true;
        Editor = editor;
        _suppressDirtyNotifications = false;
        ResetDirty();
    }

    private bool CanAttachDocument()
        => !IsBusy && !IsEditorEnabled && _loadedSupplier is not null && _loadedSupplier.Id > 0;

    private void UpdateAttachmentCommandState()
    {
        if (AttachDocumentCommand is AsyncRelayCommand command)
        {
            command.NotifyCanExecuteChanged();
        }
    }

    private async Task AttachDocumentAsync()
    {
        if (_loadedSupplier is null || _loadedSupplier.Id <= 0)
        {
            return;
        }

        var files = await _filePicker.PickFilesAsync(new FilePickerRequest
        {
            Title = "Attach supplier document"
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
                EntityType = "suppliers",
                EntityId = _loadedSupplier.Id,
                FileName = file.FileName,
                ContentType = file.ContentType,
                UploadedById = _authContext.CurrentUser?.Id,
                SourceIp = _authContext.CurrentIpAddress,
                SourceHost = _authContext.CurrentDeviceInfo,
                Reason = "Supplier document upload"
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

    private static ModuleRecord ToRecord(Supplier supplier)
    {
        var inspector = new List<InspectorField>
        {
            new("Type", string.IsNullOrWhiteSpace(supplier.SupplierType) ? "-" : supplier.SupplierType!),
            new("Email", string.IsNullOrWhiteSpace(supplier.Email) ? "-" : supplier.Email!),
            new("Phone", string.IsNullOrWhiteSpace(supplier.Phone) ? "-" : supplier.Phone!),
            new("Risk", string.IsNullOrWhiteSpace(supplier.RiskLevel) ? "-" : supplier.RiskLevel!),
            new("Country", string.IsNullOrWhiteSpace(supplier.Country) ? "-" : supplier.Country!)
        };

        var relatedKey = supplier.PartsSupplied.Count > 0 ? PartsModuleViewModel.ModuleKey : null;
        return new ModuleRecord(
            supplier.Id.ToString(CultureInfo.InvariantCulture),
            supplier.Name,
            supplier.VatNumber,
            supplier.Status,
            supplier.Notes,
            inspector,
            relatedKey,
            supplier.PartsSupplied.Count > 0 ? supplier.PartsSupplied.First().Id : null);
    }
}
/// <summary>
/// Represents the supplier editor value.
/// </summary>

public sealed partial class SupplierEditor : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _supplierType = string.Empty;

    [ObservableProperty]
    private string _status = "Active";

    [ObservableProperty]
    private string _vatNumber = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _website = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private string _city = string.Empty;

    [ObservableProperty]
    private string _country = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _contractFile = string.Empty;

    [ObservableProperty]
    private string _riskLevel = "Low";

    [ObservableProperty]
    private bool _isQualified;

    [ObservableProperty]
    private DateTime? _cooperationStart = DateTime.UtcNow.Date;

    [ObservableProperty]
    private DateTime? _cooperationEnd;

    [ObservableProperty]
    private string _registeredAuthorities = string.Empty;

    [ObservableProperty]
    private string _digitalSignature = string.Empty;
    /// <summary>
    /// Executes the create empty operation.
    /// </summary>

    public static SupplierEditor CreateEmpty() => new();
    /// <summary>
    /// Executes the create for new operation.
    /// </summary>

    public static SupplierEditor CreateForNew()
        => new()
        {
            Status = "Active",
            RiskLevel = "Low",
            CooperationStart = DateTime.UtcNow.Date
        };
    /// <summary>
    /// Executes the from supplier operation.
    /// </summary>

    public static SupplierEditor FromSupplier(Supplier supplier)
    {
        return new SupplierEditor
        {
            Id = supplier.Id,
            Name = supplier.Name ?? string.Empty,
            SupplierType = supplier.SupplierType ?? string.Empty,
            Status = string.IsNullOrWhiteSpace(supplier.Status)
                ? "Active"
                : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(supplier.Status),
            VatNumber = supplier.VatNumber ?? string.Empty,
            Email = supplier.Email ?? string.Empty,
            Phone = supplier.Phone ?? string.Empty,
            Website = supplier.Website ?? string.Empty,
            Address = supplier.Address ?? string.Empty,
            City = supplier.City ?? string.Empty,
            Country = supplier.Country ?? string.Empty,
            Notes = supplier.Notes ?? string.Empty,
            ContractFile = supplier.ContractFile ?? string.Empty,
            RiskLevel = string.IsNullOrWhiteSpace(supplier.RiskLevel) ? "Low" : supplier.RiskLevel!,
            IsQualified = supplier.IsQualified,
            CooperationStart = supplier.CooperationStart,
            CooperationEnd = supplier.CooperationEnd,
            RegisteredAuthorities = supplier.RegisteredAuthorities ?? string.Empty,
            DigitalSignature = supplier.DigitalSignature ?? string.Empty
        };
    }
    /// <summary>
    /// Executes the clone operation.
    /// </summary>

    public SupplierEditor Clone()
    {
        return new SupplierEditor
        {
            Id = Id,
            Name = Name,
            SupplierType = SupplierType,
            Status = Status,
            VatNumber = VatNumber,
            Email = Email,
            Phone = Phone,
            Website = Website,
            Address = Address,
            City = City,
            Country = Country,
            Notes = Notes,
            ContractFile = ContractFile,
            RiskLevel = RiskLevel,
            IsQualified = IsQualified,
            CooperationStart = CooperationStart,
            CooperationEnd = CooperationEnd,
            RegisteredAuthorities = RegisteredAuthorities,
            DigitalSignature = DigitalSignature
        };
    }
    /// <summary>
    /// Executes the to supplier operation.
    /// </summary>

    public Supplier ToSupplier(Supplier? existing)
    {
        var supplier = existing ?? new Supplier();
        supplier.Id = Id;
        supplier.Name = Name?.Trim() ?? string.Empty;
        supplier.SupplierType = SupplierType?.Trim() ?? string.Empty;
        supplier.Status = Status?.Trim() ?? string.Empty;
        supplier.VatNumber = VatNumber?.Trim() ?? string.Empty;
        supplier.Email = Email?.Trim() ?? string.Empty;
        supplier.Phone = Phone?.Trim() ?? string.Empty;
        supplier.Website = Website?.Trim() ?? string.Empty;
        supplier.Address = Address?.Trim() ?? string.Empty;
        supplier.City = City?.Trim() ?? string.Empty;
        supplier.Country = Country?.Trim() ?? string.Empty;
        supplier.Notes = Notes?.Trim() ?? string.Empty;
        supplier.ContractFile = ContractFile?.Trim() ?? string.Empty;
        supplier.RiskLevel = RiskLevel?.Trim() ?? string.Empty;
        supplier.IsQualified = IsQualified;
        supplier.CooperationStart = CooperationStart;
        supplier.CooperationEnd = CooperationEnd;
        supplier.RegisteredAuthorities = RegisteredAuthorities?.Trim() ?? string.Empty;
        supplier.DigitalSignature = DigitalSignature ?? existing?.DigitalSignature ?? string.Empty;
        return supplier;
    }
}
