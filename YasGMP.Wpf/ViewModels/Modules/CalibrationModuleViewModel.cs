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

public sealed partial class CalibrationModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public new const string ModuleKey = "Calibration";

    private readonly ICalibrationCrudService _calibrationService;
    private readonly IComponentCrudService _componentService;
    private readonly IAuthContext _authContext;
    private readonly IFilePicker _filePicker;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IElectronicSignatureDialogService _signatureDialog;

    private Calibration? _loadedCalibration;
    private CalibrationEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;
    private IReadOnlyList<Component> _components = Array.Empty<Component>();
    private IReadOnlyList<Supplier> _suppliers = Array.Empty<Supplier>();

    public CalibrationModuleViewModel(
        DatabaseService databaseService,
        ICalibrationCrudService calibrationService,
        IComponentCrudService componentService,
        IAuthContext authContext,
        IFilePicker filePicker,
        IAttachmentWorkflowService attachmentWorkflow,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Calibration", databaseService, cflDialogService, shellInteraction, navigation)
    {
        _calibrationService = calibrationService ?? throw new ArgumentNullException(nameof(calibrationService));
        _componentService = componentService ?? throw new ArgumentNullException(nameof(componentService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));

        Editor = CalibrationEditor.CreateEmpty();
        ComponentOptions = new ObservableCollection<ComponentOption>();
        SupplierOptions = new ObservableCollection<SupplierOption>();

        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
    }

    [ObservableProperty]
    private CalibrationEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;

    public ObservableCollection<ComponentOption> ComponentOptions { get; }

    public ObservableCollection<SupplierOption> SupplierOptions { get; }

    public IAsyncRelayCommand AttachDocumentCommand { get; }

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
        LoadEditor(calibration);
        UpdateAttachmentCommandState();
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
                break;
            case FormMode.Update:
                _snapshot = Editor.Clone();
                break;
            case FormMode.View:
                _snapshot = null;
                break;
        }

        UpdateAttachmentCommandState();

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

        try
        {
            if (Mode == FormMode.Add)
            {
                var id = await _calibrationService.CreateAsync(calibration, context).ConfigureAwait(false);
                calibration.Id = id;
            }
            else if (Mode == FormMode.Update)
            {
                calibration.Id = _loadedCalibration!.Id;
                await _calibrationService.UpdateAsync(calibration, context).ConfigureAwait(false);
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

        _loadedCalibration = calibration;
        LoadEditor(calibration);
        UpdateAttachmentCommandState();

        signatureResult.Signature.RecordId = calibration.Id;

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
            SetEditor(_loadedCalibration is null
                ? CalibrationEditor.CreateEmpty()
                : CalibrationEditor.FromCalibration(_loadedCalibration, FindComponentName, FindSupplierName));
            UpdateAttachmentCommandState();
            return;
        }

        if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
            UpdateAttachmentCommandState();
            return;
        }

        if (_loadedCalibration is not null)
        {
            LoadEditor(_loadedCalibration);
        }

        UpdateAttachmentCommandState();
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
    }

    private void SetEditor(CalibrationEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateAttachmentCommandState();
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

        public static CalibrationEditor CreateEmpty() => new();

        public static CalibrationEditor CreateForNew() => new();

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
                Result = calibration.Result ?? string.Empty,
                Comment = calibration.Comment ?? string.Empty
            };
        }

        public Calibration ToCalibration(Calibration? existing)
        {
            var calibration = existing is null ? new Calibration() : CloneCalibration(existing);
            calibration.Id = Id;
            calibration.ComponentId = ComponentId;
            calibration.SupplierId = SupplierId;
            calibration.CalibrationDate = CalibrationDate;
            calibration.NextDue = NextDue;
            calibration.CertDoc = CertDoc;
            calibration.Result = Result;
            calibration.Comment = Comment;
            return calibration;
        }

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

    public readonly record struct ComponentOption(int Id, string Name)
    {
        public override string ToString() => Name;
    }

    public readonly record struct SupplierOption(int Id, string Name)
    {
        public override string ToString() => Name;
    }
}
