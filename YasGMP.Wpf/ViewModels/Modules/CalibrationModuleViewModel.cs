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
/// Represents the calibration module view model value.
/// </summary>

public sealed partial class CalibrationModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>
    /// Represents the module key value.
    /// </summary>
    public new const string ModuleKey = "Calibration";

    private readonly ICalibrationCrudService _calibrationService;
    private readonly IComponentCrudService _componentService;
    private readonly IAuthContext _authContext;
    private readonly IFilePicker _filePicker;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private readonly ICalibrationCertificateDialogService _certificateDialog;

    private Calibration? _loadedCalibration;
    private CalibrationEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;
    private IReadOnlyList<Component> _components = Array.Empty<Component>();
    private IReadOnlyList<Supplier> _suppliers = Array.Empty<Supplier>();
    private CalibrationCertificateDialogResult? _pendingCertificate;
    private int? _pendingAttachmentRemoval;
    /// <summary>
    /// Initializes a new instance of the CalibrationModuleViewModel class.
    /// </summary>

    public CalibrationModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ICalibrationCrudService calibrationService,
        IComponentCrudService componentService,
        IAuthContext authContext,
        IFilePicker filePicker,
        IAttachmentWorkflowService attachmentWorkflow,
        IElectronicSignatureDialogService signatureDialog,
        ICalibrationCertificateDialogService certificateDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, localization.GetString("Module.Title.Calibration"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _calibrationService = calibrationService ?? throw new ArgumentNullException(nameof(calibrationService));
        _componentService = componentService ?? throw new ArgumentNullException(nameof(componentService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        _certificateDialog = certificateDialog ?? throw new ArgumentNullException(nameof(certificateDialog));

        Editor = CalibrationEditor.CreateEmpty();
        ComponentOptions = new ObservableCollection<ComponentOption>();
        SupplierOptions = new ObservableCollection<SupplierOption>();

        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
        ManageCertificateCommand = new AsyncRelayCommand(ManageCertificateAsync, CanManageCertificate);
        ClearCertificateCommand = new RelayCommand(ClearCertificate, CanClearCertificate);
    }

    [ObservableProperty]
    private CalibrationEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;

    partial void OnIsEditorEnabledChanged(bool value)
        => UpdateCertificateCommandState();
    /// <summary>
    /// Gets or sets the component options.
    /// </summary>

    public ObservableCollection<ComponentOption> ComponentOptions { get; }
    /// <summary>
    /// Gets or sets the supplier options.
    /// </summary>

    public ObservableCollection<SupplierOption> SupplierOptions { get; }
    /// <summary>
    /// Gets or sets the attach document command.
    /// </summary>

    public IAsyncRelayCommand AttachDocumentCommand { get; }

    /// <summary>Gets the command that opens the calibration certificate dialog.</summary>
    public IAsyncRelayCommand ManageCertificateCommand { get; }

    /// <summary>Gets the command that clears the staged certificate metadata.</summary>
    public IRelayCommand ClearCertificateCommand { get; }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        _components = await _componentService.GetAllAsync().ConfigureAwait(false);
        _suppliers = await Database.GetAllSuppliersAsync().ConfigureAwait(false);

        RefreshComponentOptions(_components);
        RefreshSupplierOptions(_suppliers);

        var calibrations = await _calibrationService.GetAllAsync().ConfigureAwait(false);
        return calibrations.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        _components = new List<Component>
        {
            new() { Id = 201, Name = "Temperature Probe" },
            new() { Id = 202, Name = "Pressure Transducer" }
        };

        _suppliers = new List<Supplier>
        {
            new() { Id = 51, Name = "Metrologix Labs" }
        };

        RefreshComponentOptions(_components);
        RefreshSupplierOptions(_suppliers);

        var sample = new List<Calibration>
        {
            new()
            {
                Id = 1001,
                ComponentId = 201,
                SupplierId = 51,
                CalibrationDate = DateTime.UtcNow.AddDays(-14),
                NextDue = DateTime.UtcNow.AddMonths(6),
                Result = "PASS",
                CertDoc = "CAL-1001.pdf",
                Comment = "Initial qualification"
            },
            new()
            {
                Id = 1002,
                ComponentId = 202,
                SupplierId = 51,
                CalibrationDate = DateTime.UtcNow.AddDays(-45),
                NextDue = DateTime.UtcNow.AddMonths(3),
                Result = "PASS",
                CertDoc = "CAL-1002.pdf",
                Comment = "Semi-annual"
            }
        };

        return sample.Select(ToRecord).ToList();
    }

    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var calibrations = await _calibrationService.GetAllAsync().ConfigureAwait(false);
        var items = calibrations
            .Select(calibration =>
            {
                var key = calibration.Id.ToString(CultureInfo.InvariantCulture);
                var label = $"Calibration #{calibration.Id}";
                var descriptionParts = new List<string>
                {
                    FindComponentName(calibration.ComponentId) ?? $"Component #{calibration.ComponentId}",
                    calibration.CalibrationDate.ToString("d", CultureInfo.CurrentCulture),
                    calibration.NextDue.ToString("d", CultureInfo.CurrentCulture)
                };

                if (!string.IsNullOrWhiteSpace(calibration.Result))
                {
                    descriptionParts.Add(calibration.Result!);
                }

                return new CflItem(key, label, string.Join(" • ", descriptionParts));
            })
            .ToList();

        return new CflRequest("Select Calibration", items);
    }

    protected override Task OnCflSelectionAsync(CflResult result)
    {
        var search = result.Selected.Label;
        var match = Records.FirstOrDefault(r => r.Key == result.Selected.Key);
        if (match is not null)
        {
            SelectedRecord = match;
            search = match.Title;
        }

        SearchText = search;
        StatusMessage = $"Filtered {Title} by \"{search}\".";
        return Task.CompletedTask;
    }

    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedCalibration = null;
            SetEditor(CalibrationEditor.CreateEmpty());
            UpdateAttachmentCommandState();
            _pendingCertificate = null;
            _pendingAttachmentRemoval = null;
            UpdateCertificateCommandState();
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

        var calibration = await _calibrationService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (calibration is null)
        {
            StatusMessage = $"Unable to locate calibration #{id}.";
            return;
        }

        _loadedCalibration = calibration;
        _pendingCertificate = null;
        _pendingAttachmentRemoval = null;
        LoadEditor(calibration);
        UpdateAttachmentCommandState();
        UpdateCertificateCommandState();
    }

    protected override Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        switch (mode)
        {
            case FormMode.Add:
                _snapshot = null;
                _loadedCalibration = null;
                SetEditor(CalibrationEditor.CreateForNew());
                ApplyDefaultLookupSelections();
                _pendingCertificate = null;
                _pendingAttachmentRemoval = null;
                break;
            case FormMode.Update:
                _snapshot = Editor.Clone();
                break;
            case FormMode.View:
                _snapshot = null;
                break;
        }

        UpdateAttachmentCommandState();
        UpdateCertificateCommandState();

        return Task.CompletedTask;
    }

    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var errors = new List<string>();
        try
        {
            var calibration = Editor.ToCalibration(_loadedCalibration);
            _calibrationService.Validate(calibration);
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(ex.Message);
        }
        catch (Exception ex)
        {
            errors.Add($"Unexpected validation failure: {ex.Message}");
        }

        return await Task.FromResult<IReadOnlyList<string>>(errors).ConfigureAwait(false);
    }

    protected override async Task<bool> OnSaveAsync()
    {
        if (Mode == FormMode.Update && _loadedCalibration is null)
        {
            StatusMessage = "Select a calibration before saving.";
            return false;
        }

        var calibration = Editor.ToCalibration(_loadedCalibration);
        var recordId = Mode == FormMode.Update ? _loadedCalibration!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("calibrations", recordId))
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

        calibration.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;

        var context = CalibrationCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        Calibration adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _calibrationService.CreateAsync(calibration, context).ConfigureAwait(false);
                calibration.Id = saveResult.Id;
                adapterResult = calibration;
            }
            else if (Mode == FormMode.Update)
            {
                calibration.Id = _loadedCalibration!.Id;
                saveResult = await _calibrationService.UpdateAsync(calibration, context).ConfigureAwait(false);
                adapterResult = calibration;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist calibration: {ex.Message}", ex);
        }

        if (saveResult.SignatureMetadata?.Id is { } signatureId)
        {
            adapterResult.DigitalSignatureId = signatureId;
        }

        _loadedCalibration = calibration;

        var (certificateSnapshot, certificateMessage) = await HandlePendingCertificateAsync(adapterResult).ConfigureAwait(false);

        LoadEditor(calibration);
        UpdateAttachmentCommandState();

        if (certificateSnapshot is not null)
        {
            Editor.Certificate = certificateSnapshot;
            Editor.CertDoc = certificateSnapshot.DisplayName;
        }

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "calibrations",
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

        StatusMessage = string.IsNullOrWhiteSpace(certificateMessage)
            ? $"Electronic signature captured ({signatureResult.ReasonDisplay})."
            : $"Electronic signature captured ({signatureResult.ReasonDisplay}). {certificateMessage}";
        return true;
    }

    protected override void OnCancel()
    {
        if (Mode == FormMode.Add)
        {
            SetEditor(_loadedCalibration is null
                ? CalibrationEditor.CreateEmpty()
                : CalibrationEditor.FromCalibration(_loadedCalibration, FindComponentName, FindSupplierName));
            UpdateAttachmentCommandState();
            _pendingCertificate = null;
            _pendingAttachmentRemoval = null;
            UpdateCertificateCommandState();
            return;
        }

        if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
            UpdateAttachmentCommandState();
            _pendingCertificate = null;
            _pendingAttachmentRemoval = null;
            UpdateCertificateCommandState();
            return;
        }

        if (_loadedCalibration is not null)
        {
            LoadEditor(_loadedCalibration);
        }

        UpdateAttachmentCommandState();
        _pendingCertificate = null;
        _pendingAttachmentRemoval = null;
        UpdateCertificateCommandState();
    }

    partial void OnEditorChanging(CalibrationEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(CalibrationEditor value)
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

        if (e.PropertyName == nameof(CalibrationEditor.ComponentId))
        {
            Editor.ComponentName = FindComponentName(Editor.ComponentId) ?? string.Empty;
        }
        else if (e.PropertyName == nameof(CalibrationEditor.SupplierId))
        {
            Editor.SupplierName = FindSupplierName(Editor.SupplierId) ?? string.Empty;
        }
        else if (e.PropertyName == nameof(CalibrationEditor.Certificate))
        {
            UpdateCertificateCommandState();
        }

        if (IsInEditMode)
        {
            MarkDirty();
        }
    }

    private void ApplyDefaultLookupSelections()
    {
        if (ComponentOptions.Count > 0)
        {
            Editor.ComponentId = ComponentOptions[0].Id;
        }

        if (SupplierOptions.Count > 0)
        {
            Editor.SupplierId = SupplierOptions[0].Id;
        }
    }

    private void LoadEditor(Calibration calibration)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = CalibrationEditor.FromCalibration(calibration, FindComponentName, FindSupplierName);
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateAttachmentCommandState();
        UpdateCertificateCommandState();
    }

    private void SetEditor(CalibrationEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateAttachmentCommandState();
        UpdateCertificateCommandState();
    }

    private void RefreshComponentOptions(IEnumerable<Component> components)
    {
        ComponentOptions.Clear();
        foreach (var component in components)
        {
            var label = string.IsNullOrWhiteSpace(component.Name)
                ? component.Id.ToString(CultureInfo.InvariantCulture)
                : component.Name!;
            ComponentOptions.Add(new ComponentOption(component.Id, label));
        }

        Editor.ComponentName = FindComponentName(Editor.ComponentId) ?? string.Empty;
    }

    private void RefreshSupplierOptions(IEnumerable<Supplier> suppliers)
    {
        SupplierOptions.Clear();
        foreach (var supplier in suppliers)
        {
            SupplierOptions.Add(new SupplierOption(supplier.Id, supplier.Name));
        }

        Editor.SupplierName = FindSupplierName(Editor.SupplierId) ?? string.Empty;
    }

    private ModuleRecord ToRecord(Calibration calibration)
    {
        var fields = new List<InspectorField>
        {
            new("Component", FindComponentName(calibration.ComponentId) ?? $"Component #{calibration.ComponentId}"),
            new("Supplier", FindSupplierName(calibration.SupplierId) ?? "-"),
            new("Calibrated", calibration.CalibrationDate.ToString("d", CultureInfo.CurrentCulture)),
            new("Next Due", calibration.NextDue.ToString("d", CultureInfo.CurrentCulture)),
            new("Result", string.IsNullOrWhiteSpace(calibration.Result) ? "-" : calibration.Result)
        };

        return new ModuleRecord(
            calibration.Id.ToString(CultureInfo.InvariantCulture),
            $"Calibration #{calibration.Id}",
            calibration.CertDoc,
            null,
            calibration.Comment,
            fields,
            ComponentsModuleViewModel.ModuleKey,
            calibration.ComponentId);
    }

    private string? FindComponentName(int componentId)
        => _components.FirstOrDefault(c => c.Id == componentId)?.Name;

    private string? FindSupplierName(int? supplierId)
    {
        if (!supplierId.HasValue)
        {
            return null;
        }

        return _suppliers.FirstOrDefault(s => s.Id == supplierId.Value)?.Name;
    }

    private bool CanAttachDocument()
        => !IsBusy
           && !IsEditorEnabled
           && _loadedCalibration is { Id: > 0 };

    private async Task AttachDocumentAsync()
    {
        if (_loadedCalibration is null || _loadedCalibration.Id <= 0)
        {
            StatusMessage = "Save the calibration before adding attachments.";
            return;
        }

        try
        {
            IsBusy = true;
            var files = await _filePicker
                .PickFilesAsync(new FilePickerRequest(AllowMultiple: true, Title: $"Attach files to calibration #{_loadedCalibration.Id}"))
                .ConfigureAwait(false);

            if (files is null || files.Count == 0)
            {
                StatusMessage = "Attachment upload cancelled.";
                return;
            }

            var processed = 0;
            var deduplicated = 0;
            var uploadedBy = _authContext.CurrentUser?.Id;

            foreach (var file in files)
            {
                await using var stream = await file.OpenReadAsync().ConfigureAwait(false);
                var request = new AttachmentUploadRequest
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    EntityType = "calibrations",
                    EntityId = _loadedCalibration.Id,
                    UploadedById = uploadedBy,
                    Reason = $"calibration:{_loadedCalibration.Id}",
                    SourceIp = _authContext.CurrentIpAddress,
                    SourceHost = _authContext.CurrentDeviceInfo,
                    Notes = $"WPF:{ModuleKey}:{DateTime.UtcNow:O}"
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
        catch (Exception ex)
        {
            StatusMessage = $"Attachment upload failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            UpdateAttachmentCommandState();
        }
    }

    private void UpdateAttachmentCommandState()
        => AttachDocumentCommand.NotifyCanExecuteChanged();

    private bool CanManageCertificate()
        => IsEditorEnabled;

    private bool CanClearCertificate()
        => IsEditorEnabled && Editor.Certificate is not null;

    private async Task ManageCertificateAsync()
    {
        if (!CanManageCertificate())
        {
            return;
        }

        var request = new CalibrationCertificateDialogRequest(
            Editor.Certificate,
            BuildCertificateDialogCaption(),
            AllowFileSelection: true);

        CalibrationCertificateDialogResult? result;
        try
        {
            result = await _certificateDialog
                .ShowAsync(request)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Certificate dialog failed: {ex.Message}";
            return;
        }

        if (result is null)
        {
            StatusMessage = "Certificate update cancelled.";
            return;
        }

        ApplyCertificateSelection(result);
        StatusMessage = "Certificate metadata staged.";
    }

    private void ClearCertificate()
    {
        if (!CanClearCertificate())
        {
            return;
        }

        _pendingCertificate = null;
        _pendingAttachmentRemoval = Editor.Certificate?.AttachmentId;

        Editor.Certificate = null;
        Editor.CertDoc = string.Empty;

        if (IsInEditMode)
        {
            MarkDirty();
        }

        UpdateCertificateCommandState();
        UpdateAttachmentCommandState();
        StatusMessage = "Certificate metadata cleared.";
    }

    private void ApplyCertificateSelection(CalibrationCertificateDialogResult result)
    {
        _pendingCertificate = result;
        _pendingAttachmentRemoval = result.FileCleared ? result.RemovedAttachmentId : null;

        Editor.Certificate = result.Certificate;
        Editor.CertDoc = result.Certificate.DisplayName;

        if (IsInEditMode)
        {
            MarkDirty();
        }

        UpdateCertificateCommandState();
        UpdateAttachmentCommandState();
    }

    private string? BuildCertificateDialogCaption()
    {
        if (_loadedCalibration is { Id: > 0 } calibration)
        {
            return $"Calibration #{calibration.Id}";
        }

        if (!string.IsNullOrWhiteSpace(Editor.ComponentName))
        {
            return Editor.ComponentName;
        }

        return null;
    }

    private void UpdateCertificateCommandState()
    {
        ManageCertificateCommand.NotifyCanExecuteChanged();
        ClearCertificateCommand.NotifyCanExecuteChanged();
    }

    private async Task<(CalibrationCertificateSnapshot? Snapshot, string? Message)> HandlePendingCertificateAsync(Calibration calibration)
    {
        if (calibration is null || calibration.Id <= 0)
        {
            _pendingCertificate = null;
            _pendingAttachmentRemoval = null;
            return (null, null);
        }

        var messages = new List<string>();
        CalibrationCertificateSnapshot? snapshot = null;

        if (_pendingAttachmentRemoval is int removeId && removeId > 0)
        {
            try
            {
                await _attachmentWorkflow
                    .RemoveLinkAsync("calibrations", calibration.Id, removeId)
                    .ConfigureAwait(false);
                messages.Add($"Removed certificate attachment #{removeId}.");
            }
            catch (Exception ex)
            {
                _pendingAttachmentRemoval = null;
                return (null, $"Certificate attachment removal failed: {ex.Message}");
            }

            _pendingAttachmentRemoval = null;
        }

        if (_pendingCertificate is { File: not null } pendingFile)
        {
            try
            {
                await using var stream = await pendingFile.File.OpenReadAsync().ConfigureAwait(false);
                var request = new AttachmentUploadRequest
                {
                    FileName = pendingFile.File.FileName,
                    ContentType = pendingFile.File.ContentType,
                    EntityType = "calibrations",
                    EntityId = calibration.Id,
                    UploadedById = _authContext.CurrentUser?.Id,
                    DisplayName = pendingFile.Certificate.DisplayName,
                    RetainUntil = pendingFile.Certificate.ExpiresOn,
                    Reason = $"calibration-cert:{calibration.Id}",
                    SourceIp = _authContext.CurrentIpAddress,
                    SourceHost = _authContext.CurrentDeviceInfo,
                    Notes = BuildCertificateNotes(pendingFile.Certificate)
                };

                var uploadResult = await _attachmentWorkflow
                    .UploadAsync(stream, request)
                    .ConfigureAwait(false);

                var attachment = uploadResult.Attachment;
                var updatedCertificate = pendingFile.Certificate with
                {
                    AttachmentId = attachment.Id,
                    FileName = attachment.FileName,
                    FileSize = attachment.FileSize,
                    ContentType = attachment.FileType,
                    Sha256 = attachment.Sha256
                };

                snapshot = updatedCertificate;
                messages.Add($"Uploaded certificate '{attachment.FileName}'.");
            }
            catch (Exception ex)
            {
                return (snapshot, $"Certificate upload failed: {ex.Message}");
            }
        }
        else if (_pendingCertificate is { File: null } pendingSnapshot)
        {
            snapshot = pendingSnapshot.Certificate;
        }

        _pendingCertificate = null;
        var message = messages.Count > 0 ? string.Join(" ", messages) : null;
        return (snapshot, message);
    }

    private static string? BuildCertificateNotes(CalibrationCertificateSnapshot certificate)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(certificate.CertificateNumber))
        {
            parts.Add($"number={certificate.CertificateNumber}");
        }

        if (!string.IsNullOrWhiteSpace(certificate.Issuer))
        {
            parts.Add($"issuer={certificate.Issuer}");
        }

        if (certificate.IssuedOn.HasValue)
        {
            parts.Add($"issued={certificate.IssuedOn:yyyy-MM-dd}");
        }

        if (certificate.ExpiresOn.HasValue)
        {
            parts.Add($"expires={certificate.ExpiresOn:yyyy-MM-dd}");
        }

        if (!string.IsNullOrWhiteSpace(certificate.Notes))
        {
            parts.Add($"notes={certificate.Notes}");
        }

        return parts.Count == 0 ? null : string.Join("; ", parts);
    }
    /// <summary>
    /// Represents the calibration editor value.
    /// </summary>

    public sealed partial class CalibrationEditor : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private int _componentId;

        [ObservableProperty]
        private string _componentName = string.Empty;

        [ObservableProperty]
        private int? _supplierId;

        [ObservableProperty]
        private string _supplierName = string.Empty;

        [ObservableProperty]
        private DateTime _calibrationDate = DateTime.UtcNow.Date;

        [ObservableProperty]
        private DateTime _nextDue = DateTime.UtcNow.Date.AddMonths(6);

        [ObservableProperty]
        private string _certDoc = string.Empty;

        [ObservableProperty]
        private string _result = string.Empty;

        [ObservableProperty]
        private string _comment = string.Empty;

        [ObservableProperty]
        private CalibrationCertificateSnapshot? _certificate;
        /// <summary>
        /// Executes the create empty operation.
        /// </summary>

        public static CalibrationEditor CreateEmpty() => new();
        /// <summary>
        /// Executes the create for new operation.
        /// </summary>

        public static CalibrationEditor CreateForNew() => new();
        /// <summary>
        /// Executes the from calibration operation.
        /// </summary>

        public static CalibrationEditor FromCalibration(
            Calibration calibration,
            Func<int, string?> componentLookup,
            Func<int?, string?> supplierLookup)
        {
            return new CalibrationEditor
            {
                Id = calibration.Id,
                ComponentId = calibration.ComponentId,
                ComponentName = componentLookup(calibration.ComponentId) ?? string.Empty,
                SupplierId = calibration.SupplierId,
                SupplierName = supplierLookup(calibration.SupplierId) ?? string.Empty,
                CalibrationDate = calibration.CalibrationDate,
                NextDue = calibration.NextDue,
                CertDoc = calibration.CertDoc ?? string.Empty,
                Certificate = string.IsNullOrWhiteSpace(calibration.CertDoc)
                    ? null
                    : new CalibrationCertificateSnapshot(calibration.CertDoc!, FileName: calibration.CertDoc),
                Result = calibration.Result ?? string.Empty,
                Comment = calibration.Comment ?? string.Empty
            };
        }
        /// <summary>
        /// Executes the to calibration operation.
        /// </summary>

        public Calibration ToCalibration(Calibration? existing)
        {
            var calibration = existing is null ? new Calibration() : CloneCalibration(existing);
            calibration.Id = Id;
            calibration.ComponentId = ComponentId;
            calibration.SupplierId = SupplierId;
            calibration.CalibrationDate = CalibrationDate;
            calibration.NextDue = NextDue;
            calibration.CertDoc = Certificate?.DisplayName ?? CertDoc;
            calibration.Result = Result;
            calibration.Comment = Comment;
            return calibration;
        }
        /// <summary>
        /// Executes the clone operation.
        /// </summary>

        public CalibrationEditor Clone()
            => new()
            {
                Id = Id,
                ComponentId = ComponentId,
                ComponentName = ComponentName,
                SupplierId = SupplierId,
                SupplierName = SupplierName,
                CalibrationDate = CalibrationDate,
                NextDue = NextDue,
                CertDoc = CertDoc,
                Certificate = Certificate,
                Result = Result,
                Comment = Comment
            };

        private static Calibration CloneCalibration(Calibration source)
        {
            return new Calibration
            {
                Id = source.Id,
                ComponentId = source.ComponentId,
                SupplierId = source.SupplierId,
                CalibrationDate = source.CalibrationDate,
                NextDue = source.NextDue,
                CertDoc = source.CertDoc,
                Result = source.Result,
                Comment = source.Comment
            };
        }
    }
    /// <summary>
    /// Executes the struct operation.
    /// </summary>

    public readonly record struct ComponentOption(int Id, string Name)
    {
        /// <summary>
        /// Executes the to string operation.
        /// </summary>
        public override string ToString() => Name;
    }
    /// <summary>
    /// Executes the struct operation.
    /// </summary>

    public readonly record struct SupplierOption(int Id, string Name)
    {
        /// <summary>
        /// Executes the to string operation.
        /// </summary>
        public override string ToString() => Name;
    }
}
