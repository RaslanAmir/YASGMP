using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.AppCore.Services;
using YasGMP.Models;
using YasGMP.Models.DTO;
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
    private readonly IShellInteractionService _shellInteraction;
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
        _shellInteraction = shellInteraction ?? throw new ArgumentNullException(nameof(shellInteraction));

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
        AuditTimeline = new ObservableCollection<AuditEntryDto>();
        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
        PreviewContractCommand = new AsyncRelayCommand(PreviewContractAsync, CanPreviewContract);
        DownloadContractCommand = new AsyncRelayCommand(DownloadContractAsync, CanDownloadContract);
    }

    [ObservableProperty]
    private SupplierEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;

    [ObservableProperty]
    private IReadOnlyList<string> _statusOptions;

    [ObservableProperty]
    private IReadOnlyList<string> _riskOptions;

    [ObservableProperty]
    private ObservableCollection<AuditEntryDto> _auditTimeline;
    /// <summary>
    /// Gets or sets the attach document command.
    /// </summary>

    public IAsyncRelayCommand AttachDocumentCommand { get; }

    /// <summary>
    /// Gets the command that previews the configured supplier contract.
    /// </summary>
    public IAsyncRelayCommand PreviewContractCommand { get; }

    /// <summary>
    /// Gets the command that downloads the configured supplier contract.
    /// </summary>
    public IAsyncRelayCommand DownloadContractCommand { get; }

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
            AuditTimeline.Clear();
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

        _loadedSupplier = supplier;
        LoadEditor(supplier);
        await LoadAuditTimelineAsync(supplier.Id).ConfigureAwait(false);
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
                SetEditor(SupplierEditor.CreateForNew(_authContext));
                AuditTimeline.Clear();
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
        ApplySignatureMetadataToSupplier(supplier, signatureResult, context, saveResult.SignatureMetadata);
        await LoadAuditTimelineAsync(supplier.Id).ConfigureAwait(false);
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
                _ = LoadAuditTimelineAsync(_loadedSupplier.Id);
            }
            else
            {
                SetEditor(SupplierEditor.CreateEmpty());
                AuditTimeline.Clear();
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
        if (AttachDocumentCommand is AsyncRelayCommand attach)
        {
            attach.NotifyCanExecuteChanged();
        }

        if (PreviewContractCommand is AsyncRelayCommand preview)
        {
            preview.NotifyCanExecuteChanged();
        }

        if (DownloadContractCommand is AsyncRelayCommand download)
        {
            download.NotifyCanExecuteChanged();
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

    private bool CanPreviewContract()
        => !IsBusy
           && !IsEditorEnabled
           && _loadedSupplier is { Id: > 0 }
           && !string.IsNullOrWhiteSpace(Editor?.ContractFile);

    private async Task PreviewContractAsync()
    {
        if (!CanPreviewContract())
        {
            return;
        }

        var contractName = Editor.ContractFile?.Trim();
        if (string.IsNullOrWhiteSpace(contractName))
        {
            StatusMessage = "Contract file name is not specified.";
            return;
        }

        try
        {
            IsBusy = true;
            UpdateAttachmentCommandState();

            var attachment = await FindContractAttachmentAsync(contractName).ConfigureAwait(false);
            if (attachment is null)
            {
                StatusMessage = $"Contract '{contractName}' not found for the selected supplier.";
                return;
            }

            var tempDirectory = Path.Combine(Path.GetTempPath(), $"YasGMP.Supplier.{_loadedSupplier!.Id}.{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDirectory);
            var tempPath = Path.Combine(tempDirectory, attachment.Attachment.FileName);

            await using (var destination = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.Read, 128 * 1024, useAsync: true))
            {
                var request = new AttachmentReadRequest
                {
                    RequestedById = _authContext.CurrentUser?.Id,
                    Reason = $"wpf:{ModuleKey}:preview",
                    SourceHost = Environment.MachineName,
                    SourceIp = _authContext.CurrentIpAddress
                };

                await _attachmentWorkflow
                    .DownloadAsync(attachment.Attachment.Id, destination, request)
                    .ConfigureAwait(false);
            }

            _shellInteraction.PreviewDocument(tempPath);
            StatusMessage = $"Previewing contract '{attachment.Attachment.FileName}'.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to preview contract: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            UpdateAttachmentCommandState();
        }
    }

    private bool CanDownloadContract()
        => !IsBusy
           && !IsEditorEnabled
           && _loadedSupplier is { Id: > 0 }
           && !string.IsNullOrWhiteSpace(Editor?.ContractFile);

    private async Task DownloadContractAsync()
    {
        if (!CanDownloadContract())
        {
            return;
        }

        var contractName = Editor.ContractFile?.Trim();
        if (string.IsNullOrWhiteSpace(contractName))
        {
            StatusMessage = "Contract file name is not specified.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            FileName = contractName,
            Title = $"Save {contractName}",
            Filter = "All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            StatusMessage = "Download cancelled.";
            return;
        }

        try
        {
            IsBusy = true;
            UpdateAttachmentCommandState();

            var attachment = await FindContractAttachmentAsync(contractName).ConfigureAwait(false);
            if (attachment is null)
            {
                StatusMessage = $"Contract '{contractName}' not found for the selected supplier.";
                return;
            }

            await using (var destination = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true))
            {
                var request = new AttachmentReadRequest
                {
                    RequestedById = _authContext.CurrentUser?.Id,
                    Reason = $"wpf:{ModuleKey}:download",
                    SourceHost = Environment.MachineName,
                    SourceIp = _authContext.CurrentIpAddress
                };

                var result = await _attachmentWorkflow
                    .DownloadAsync(attachment.Attachment.Id, destination, request)
                    .ConfigureAwait(false);

                StatusMessage = $"Downloaded {result.BytesWritten:N0} byte(s) to '{dialog.FileName}'.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to download contract: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            UpdateAttachmentCommandState();
        }
    }

    private async Task<AttachmentLinkWithAttachment?> FindContractAttachmentAsync(string contractName)
    {
        if (_loadedSupplier is null)
        {
            return null;
        }

        try
        {
            var links = await _attachmentWorkflow
                .GetLinksForEntityAsync("suppliers", _loadedSupplier.Id)
                .ConfigureAwait(false);

            return links.FirstOrDefault(link
                => string.Equals(link.Attachment.FileName, contractName, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(link.Attachment.Name, contractName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to query supplier attachments: {ex.Message}";
            return null;
        }
    }

    private async Task LoadAuditTimelineAsync(int supplierId)
    {
        if (Audit is null)
        {
            AuditTimeline.Clear();
            return;
        }

        try
        {
            var from = DateTime.UtcNow.AddYears(-5);
            var to = DateTime.UtcNow.AddDays(1);
            var audits = await Audit
                .GetFilteredAudits(string.Empty, "suppliers", string.Empty, from, to)
                .ConfigureAwait(false);

            var filtered = audits
                .Where(entry => string.Equals(entry.Entity, "suppliers", StringComparison.OrdinalIgnoreCase))
                .Where(entry => int.TryParse(entry.EntityId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedId)
                                 && parsedId == supplierId)
                .OrderBy(entry => entry.Timestamp)
                .ToList();

            AuditTimeline.Clear();
            foreach (var entry in filtered)
            {
                AuditTimeline.Add(entry);
            }
        }
        catch (Exception ex)
        {
            AuditTimeline.Clear();
            StatusMessage = $"Failed to load supplier audit timeline: {ex.Message}";
        }
    }

    private void ApplySignatureMetadataToSupplier(
        Supplier supplier,
        ElectronicSignatureDialogResult signatureResult,
        SupplierCrudContext context,
        SignatureMetadataDto? metadata)
    {
        if (supplier is null)
        {
            throw new ArgumentNullException(nameof(supplier));
        }

        if (signatureResult is null)
        {
            throw new ArgumentNullException(nameof(signatureResult));
        }

        if (signatureResult.Signature is null)
        {
            throw new ArgumentException("Signature result is missing the captured signature payload.", nameof(signatureResult));
        }

        var signature = signatureResult.Signature;
        var signerId = signature.UserId != 0 ? signature.UserId : context.UserId;
        var fallbackName = _authContext.CurrentUser?.FullName
            ?? _authContext.CurrentUser?.Username
            ?? string.Empty;
        var signerName = !string.IsNullOrWhiteSpace(signature.UserName)
            ? signature.UserName!
            : supplier.LastModifiedBy?.FullName
                ?? supplier.LastModifiedBy?.Username
                ?? fallbackName;

        var signatureHash = !string.IsNullOrWhiteSpace(signature.SignatureHash)
            ? signature.SignatureHash!
            : metadata?.Hash
              ?? context.SignatureHash
              ?? supplier.DigitalSignature
              ?? string.Empty;

        supplier.DigitalSignature = signatureHash;
        var metadataId = metadata?.Id ?? (signature.Id > 0 ? signature.Id : supplier.DigitalSignatureId);
        supplier.DigitalSignatureId = metadataId;

        var signedAt = signature.SignedAt ?? DateTime.UtcNow;
        supplier.LastModified = signedAt;

        if (signerId > 0)
        {
            supplier.LastModifiedById = signerId;
        }

        supplier.SourceIp = !string.IsNullOrWhiteSpace(signature.IpAddress)
            ? signature.IpAddress!
            : metadata?.IpAddress
              ?? context.Ip
              ?? supplier.SourceIp;

        supplier.SessionId = !string.IsNullOrWhiteSpace(signature.SessionId)
            ? signature.SessionId!
            : metadata?.Session
              ?? context.SessionId
              ?? supplier.SessionId;

        if (supplier.LastModifiedBy is null && (!string.IsNullOrWhiteSpace(signerName) || signerId > 0))
        {
            supplier.LastModifiedBy = new User
            {
                Id = signerId,
                FullName = signerName,
                Username = signerName
            };
        }
        else if (supplier.LastModifiedBy is not null)
        {
            if (signerId > 0)
            {
                supplier.LastModifiedBy.Id = signerId;
            }

            if (!string.IsNullOrWhiteSpace(signerName))
            {
                if (string.IsNullOrWhiteSpace(supplier.LastModifiedBy.FullName))
                {
                    supplier.LastModifiedBy.FullName = signerName;
                }

                if (string.IsNullOrWhiteSpace(supplier.LastModifiedBy.Username))
                {
                    supplier.LastModifiedBy.Username = signerName;
                }
            }
        }

        Editor.DigitalSignatureId = supplier.DigitalSignatureId;
        Editor.DigitalSignature = supplier.DigitalSignature ?? string.Empty;
        Editor.SignatureHash = supplier.DigitalSignature;
        Editor.SignatureReason = signatureResult.ReasonDisplay ?? string.Empty;
        var note = !string.IsNullOrWhiteSpace(signature.Note)
            ? signature.Note!
            : metadata?.Note
              ?? context.SignatureNote
              ?? string.Empty;
        Editor.SignatureNote = note;
        Editor.SignatureTimestampUtc = signedAt;
        Editor.SignerUserId = supplier.LastModifiedById;
        Editor.SignerUserName = signerName;
        Editor.LastModifiedUtc = supplier.LastModified;
        Editor.LastModifiedById = supplier.LastModifiedById;
        Editor.LastModifiedByName = signerName;
        Editor.SourceIp = supplier.SourceIp ?? string.Empty;
        Editor.SessionId = supplier.SessionId ?? string.Empty;
        var deviceInfo = !string.IsNullOrWhiteSpace(signature.DeviceInfo)
            ? signature.DeviceInfo!
            : metadata?.Device
              ?? context.DeviceInfo
              ?? string.Empty;
        Editor.DeviceInfo = deviceInfo;
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

    [ObservableProperty]
    private int? _digitalSignatureId;

    [ObservableProperty]
    private string _signatureHash = string.Empty;

    [ObservableProperty]
    private string _signatureReason = string.Empty;

    [ObservableProperty]
    private string _signatureNote = string.Empty;

    [ObservableProperty]
    private DateTime? _signatureTimestampUtc;

    [ObservableProperty]
    private int? _signerUserId;

    [ObservableProperty]
    private string _signerUserName = string.Empty;

    [ObservableProperty]
    private DateTime? _lastModifiedUtc;

    [ObservableProperty]
    private int? _lastModifiedById;

    [ObservableProperty]
    private string _lastModifiedByName = string.Empty;

    [ObservableProperty]
    private string _sourceIp = string.Empty;

    [ObservableProperty]
    private string _sessionId = string.Empty;

    [ObservableProperty]
    private string _deviceInfo = string.Empty;
    /// <summary>
    /// Executes the create empty operation.
    /// </summary>

    public static SupplierEditor CreateEmpty() => new();
    /// <summary>
    /// Executes the create for new operation.
    /// </summary>

    public static SupplierEditor CreateForNew(IAuthContext authContext)
    {
        if (authContext is null)
        {
            throw new ArgumentNullException(nameof(authContext));
        }

        var userId = authContext.CurrentUser?.Id;
        var userName = authContext.CurrentUser?.FullName
            ?? authContext.CurrentUser?.Username
            ?? string.Empty;

        return new SupplierEditor
        {
            Status = "Active",
            RiskLevel = "Low",
            CooperationStart = DateTime.UtcNow.Date,
            SignatureHash = string.Empty,
            SignatureReason = string.Empty,
            SignatureNote = string.Empty,
            SignatureTimestampUtc = DateTime.UtcNow,
            SignerUserId = userId,
            SignerUserName = userName,
            LastModifiedUtc = DateTime.UtcNow,
            LastModifiedById = userId,
            LastModifiedByName = userName,
            SourceIp = authContext.CurrentIpAddress ?? string.Empty,
            SessionId = authContext.CurrentSessionId ?? string.Empty,
            DeviceInfo = authContext.CurrentDeviceInfo ?? string.Empty
        };
    }
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
            DigitalSignature = supplier.DigitalSignature ?? string.Empty,
            DigitalSignatureId = supplier.DigitalSignatureId,
            SignatureHash = supplier.DigitalSignature ?? string.Empty,
            SignatureReason = string.Empty,
            SignatureNote = string.Empty,
            SignatureTimestampUtc = supplier.LastModified,
            SignerUserId = supplier.LastModifiedById,
            SignerUserName = supplier.LastModifiedBy?.FullName
                ?? supplier.LastModifiedBy?.Username
                ?? string.Empty,
            LastModifiedUtc = supplier.LastModified,
            LastModifiedById = supplier.LastModifiedById,
            LastModifiedByName = supplier.LastModifiedBy?.FullName
                ?? supplier.LastModifiedBy?.Username
                ?? string.Empty,
            SourceIp = supplier.SourceIp ?? string.Empty,
            SessionId = supplier.SessionId ?? string.Empty,
            DeviceInfo = string.Empty
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
            DigitalSignature = DigitalSignature,
            DigitalSignatureId = DigitalSignatureId,
            SignatureHash = SignatureHash,
            SignatureReason = SignatureReason,
            SignatureNote = SignatureNote,
            SignatureTimestampUtc = SignatureTimestampUtc,
            SignerUserId = SignerUserId,
            SignerUserName = SignerUserName,
            LastModifiedUtc = LastModifiedUtc,
            LastModifiedById = LastModifiedById,
            LastModifiedByName = LastModifiedByName,
            SourceIp = SourceIp,
            SessionId = SessionId,
            DeviceInfo = DeviceInfo
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
        supplier.DigitalSignatureId = DigitalSignatureId;
        supplier.DigitalSignature = !string.IsNullOrWhiteSpace(SignatureHash)
            ? SignatureHash!
            : DigitalSignature ?? existing?.DigitalSignature ?? string.Empty;
        supplier.LastModified = LastModifiedUtc ?? supplier.LastModified;
        supplier.LastModifiedById = LastModifiedById ?? supplier.LastModifiedById;
        supplier.SourceIp = string.IsNullOrWhiteSpace(SourceIp) ? supplier.SourceIp : SourceIp;
        supplier.SessionId = string.IsNullOrWhiteSpace(SessionId) ? supplier.SessionId : SessionId;
        return supplier;
    }
}
