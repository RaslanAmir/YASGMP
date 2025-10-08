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
using YasGMP.Models.Enums;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.ViewModels.Modules;
/// <summary>
/// Represents the validations module view model value.
/// </summary>

public sealed partial class ValidationsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>
    /// Represents the module key value.
    /// </summary>
    public new const string ModuleKey = "Validations";

    private readonly IValidationCrudService _validationService;
    private readonly IAuthContext _authContext;
    private readonly IFilePicker _filePicker;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IElectronicSignatureDialogService _signatureDialog;

    private Validation? _loadedValidation;
    private ValidationEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;
    private IReadOnlyList<Machine> _machines = Array.Empty<Machine>();
    private IReadOnlyList<MachineComponent> _components = Array.Empty<MachineComponent>();
    /// <summary>
    /// Initializes a new instance of the ValidationsModuleViewModel class.
    /// </summary>

    public ValidationsModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IValidationCrudService validationService,
        IAuthContext authContext,
        IFilePicker filePicker,
        IAttachmentWorkflowService attachmentWorkflow,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, localization.GetString("Module.Title.Validations"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));

        Editor = ValidationEditor.CreateEmpty();
        MachineOptions = new ObservableCollection<MachineOption>();
        ComponentOptions = new ObservableCollection<ComponentOption>();
        StatusOptions = new ObservableCollection<string>(Enum.GetNames(typeof(ValidationStatus)));
        TypeOptions = new ObservableCollection<string>(Enum.GetNames(typeof(ValidationType)));

        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
    }

    [ObservableProperty]
    private ValidationEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;
    /// <summary>
    /// Gets or sets the machine options.
    /// </summary>

    public ObservableCollection<MachineOption> MachineOptions { get; }
    /// <summary>
    /// Gets or sets the component options.
    /// </summary>

    public ObservableCollection<ComponentOption> ComponentOptions { get; }
    /// <summary>
    /// Gets or sets the status options.
    /// </summary>

    public ObservableCollection<string> StatusOptions { get; }
    /// <summary>
    /// Gets or sets the type options.
    /// </summary>

    public ObservableCollection<string> TypeOptions { get; }
    /// <summary>
    /// Gets or sets the attach document command.
    /// </summary>

    public IAsyncRelayCommand AttachDocumentCommand { get; }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        _machines = await Database.GetAllMachinesAsync().ConfigureAwait(false);
        _components = await Database.GetAllComponentsAsync().ConfigureAwait(false);

        RefreshMachineOptions(_machines);
        RefreshComponentOptions(_components);

        var validations = await _validationService.GetAllAsync().ConfigureAwait(false);
        return validations.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        _machines = new List<Machine>
        {
            new() { Id = 100, Name = "Autoclave" },
            new() { Id = 101, Name = "Filling Line" }
        };

        _components = new List<MachineComponent>
        {
            new() { Id = 200, Name = "Temperature Sensor" },
            new() { Id = 201, Name = "Pressure Gauge" }
        };

        RefreshMachineOptions(_machines);
        RefreshComponentOptions(_components);

        var sample = new List<Validation>
        {
            new()
            {
                Id = 1,
                Code = "VAL-0001",
                Type = ValidationType.IQ.ToString(),
                MachineId = 100,
                DateStart = DateTime.UtcNow.AddDays(-10),
                DateEnd = DateTime.UtcNow.AddDays(-7),
                Status = ValidationStatus.Completed.ToString(),
                NextDue = DateTime.UtcNow.AddMonths(12),
                Comment = "Initial IQ for autoclave"
            },
            new()
            {
                Id = 2,
                Code = "VAL-0002",
                Type = ValidationType.OQ.ToString(),
                ComponentId = 200,
                DateStart = DateTime.UtcNow.AddDays(-5),
                Status = ValidationStatus.InProgress.ToString(),
                NextDue = DateTime.UtcNow.AddMonths(6),
                Comment = "OQ for sensor calibration"
            }
        };

        return sample.Select(ToRecord).ToList();
    }

    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var validations = await _validationService.GetAllAsync().ConfigureAwait(false);
        var items = validations
            .Select(validation =>
            {
                var key = validation.Id.ToString(CultureInfo.InvariantCulture);
                var description = new List<string>
                {
                    validation.Type,
                    FindMachineName(validation.MachineId) ?? FindComponentName(validation.ComponentId) ?? "Unassigned",
                    validation.Status
                };

                if (validation.NextDue.HasValue)
                {
                    description.Add($"Next due {validation.NextDue.Value:d}");
                }

                return new CflItem(
                    key,
                    $"{validation.Code} ({validation.Type})",
                    string.Join(" • ", description.Where(static part => !string.IsNullOrWhiteSpace(part))));
            })
            .ToList();

        return new CflRequest("Select Validation", items);
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
            _loadedValidation = null;
            SetEditor(ValidationEditor.CreateEmpty());
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

        var validation = await _validationService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (validation is null)
        {
            StatusMessage = $"Unable to load validation #{id}.";
            return;
        }

        _loadedValidation = validation;
        LoadEditor(validation);
        UpdateAttachmentCommandState();
    }

    protected override Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        switch (mode)
        {
            case FormMode.Add:
                _snapshot = null;
                _loadedValidation = null;
                SetEditor(ValidationEditor.CreateForNew());
                ApplyDefaultLookups();
                break;
            case FormMode.Update:
                _snapshot = Editor.Clone();
                break;
            case FormMode.View:
                _snapshot = null;
                if (_loadedValidation is not null)
                {
                    LoadEditor(_loadedValidation);
                }
                break;
        }

        UpdateAttachmentCommandState();
        return Task.CompletedTask;
    }

    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Editor.Code))
        {
            errors.Add("Protocol number is required.");
        }

        if (string.IsNullOrWhiteSpace(Editor.Type))
        {
            errors.Add("Validation type is required.");
        }

        if (Editor.MachineId is null && Editor.ComponentId is null)
        {
            errors.Add("Select a machine or component.");
        }

        if (Editor.DateStart is null)
        {
            errors.Add("Start date is required.");
        }

        if (Editor.DateEnd is not null && Editor.DateStart is not null && Editor.DateEnd < Editor.DateStart)
        {
            errors.Add("End date must not precede the start date.");
        }

        try
        {
            var entity = Editor.ToValidation(_loadedValidation);
            _validationService.Validate(entity);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
        }

        return await Task.FromResult(errors);
    }

    protected override async Task<bool> OnSaveAsync()
    {
        var entity = Editor.ToValidation(_loadedValidation);

        if (Mode == FormMode.Update && _loadedValidation is null)
        {
            StatusMessage = "Select a validation before saving.";
            return false;
        }

        var recordId = Mode == FormMode.Update ? _loadedValidation!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("validations", recordId))
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

        entity.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;
        entity.LastModified = DateTime.UtcNow;
        entity.LastModifiedById = _authContext.CurrentUser?.Id;
        entity.SourceIp = _authContext.CurrentIpAddress ?? entity.SourceIp ?? string.Empty;
        entity.SessionId = _authContext.CurrentSessionId ?? entity.SessionId ?? string.Empty;
        entity.SignatureTimestamp = DateTime.UtcNow;

        var context = ValidationCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        Validation adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                if (string.IsNullOrWhiteSpace(entity.Code))
                {
                    entity.Code = $"VAL-{DateTime.UtcNow:yyyyMMddHHmmss}";
                }

                saveResult = await _validationService.CreateAsync(entity, context).ConfigureAwait(false);
                entity.Id = saveResult.Id;
                Records.Add(ToRecord(entity));
                adapterResult = entity;
            }
            else if (Mode == FormMode.Update)
            {
                entity.Id = _loadedValidation!.Id;
                saveResult = await _validationService.UpdateAsync(entity, context).ConfigureAwait(false);
                ReplaceRecord(entity);
                adapterResult = entity;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist validation: {ex.Message}", ex);
        }

        _loadedValidation = entity;
        LoadEditor(entity);
        UpdateAttachmentCommandState();

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "validations",
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
            _loadedValidation = null;
            SetEditor(ValidationEditor.CreateEmpty());
            ApplyDefaultLookups();
        }
        else if (Mode == FormMode.Update)
        {
            if (_snapshot is not null)
            {
                SetEditor(_snapshot.Clone());
            }
            else if (_loadedValidation is not null)
            {
                LoadEditor(_loadedValidation);
            }
        }

        UpdateAttachmentCommandState();
    }

    private void LoadEditor(Validation validation)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = ValidationEditor.FromValidation(validation, FindMachineName, FindComponentName);
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    private void SetEditor(ValidationEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    partial void OnEditorChanging(ValidationEditor value)
    {
        if (_editor is not null)
        {
            _editor.PropertyChanged -= OnEditorPropertyChanged;
        }
    }

    partial void OnEditorChanged(ValidationEditor value)
    {
        value.PropertyChanged += OnEditorPropertyChanged;
        UpdateAttachmentCommandState();
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressEditorDirtyNotifications)
        {
            return;
        }

        if (e.PropertyName == nameof(ValidationEditor.MachineId))
        {
            Editor.MachineName = FindMachineName(Editor.MachineId) ?? string.Empty;
            if (Editor.MachineId.HasValue)
            {
                Editor.ComponentId = null;
            }
        }
        else if (e.PropertyName == nameof(ValidationEditor.ComponentId))
        {
            Editor.ComponentName = FindComponentName(Editor.ComponentId) ?? string.Empty;
            if (Editor.ComponentId.HasValue)
            {
                Editor.MachineId = null;
            }
        }

        if (IsInEditMode)
        {
            MarkDirty();
        }
    }

    private void ApplyDefaultLookups()
    {
        if (MachineOptions.Count > 0)
        {
            Editor.MachineId = MachineOptions[0].Id;
        }
        else if (ComponentOptions.Count > 0)
        {
            Editor.ComponentId = ComponentOptions[0].Id;
        }

        if (string.IsNullOrWhiteSpace(Editor.Type) && TypeOptions.Count > 0)
        {
            Editor.Type = TypeOptions[0];
        }

        if (string.IsNullOrWhiteSpace(Editor.Status) && StatusOptions.Count > 0)
        {
            Editor.Status = StatusOptions[0];
        }
    }

    private void RefreshMachineOptions(IEnumerable<Machine> machines)
    {
        MachineOptions.Clear();
        foreach (var machine in machines)
        {
            var name = string.IsNullOrWhiteSpace(machine.Name)
                ? machine.Id.ToString(CultureInfo.InvariantCulture)
                : machine.Name!;
            MachineOptions.Add(new MachineOption(machine.Id, name));
        }

        Editor.MachineName = FindMachineName(Editor.MachineId) ?? string.Empty;
    }

    private void RefreshComponentOptions(IEnumerable<MachineComponent> components)
    {
        ComponentOptions.Clear();
        foreach (var component in components)
        {
            var name = string.IsNullOrWhiteSpace(component.Name)
                ? component.Id.ToString(CultureInfo.InvariantCulture)
                : component.Name!;
            ComponentOptions.Add(new ComponentOption(component.Id, name));
        }

        Editor.ComponentName = FindComponentName(Editor.ComponentId) ?? string.Empty;
    }

    private ModuleRecord ToRecord(Validation validation)
    {
        var targetName = FindMachineName(validation.MachineId) ?? FindComponentName(validation.ComponentId) ?? "Unassigned";
        var recordKey = validation.Id.ToString(CultureInfo.InvariantCulture);
        var recordTitle = $"{validation.Code} ({validation.Type})";

        InspectorField Field(string label, string? value) => CreateInspectorField(recordKey, recordTitle, label, value);

        var fields = new List<InspectorField>
        {
            Field("Type", validation.Type),
            Field("Status", string.IsNullOrWhiteSpace(validation.Status) ? "-" : validation.Status),
            Field("Target", targetName),
            Field("Start", validation.DateStart?.ToString("d", CultureInfo.CurrentCulture) ?? "-"),
            Field("Next Due", validation.NextDue?.ToString("d", CultureInfo.CurrentCulture) ?? "-")
        };

        var relatedKey = validation.ComponentId.HasValue
            ? ComponentsModuleViewModel.ModuleKey
            : validation.MachineId.HasValue
                ? AssetsModuleViewModel.ModuleKey
                : null;
        var relatedParameter = validation.ComponentId ?? validation.MachineId as object;

        return new ModuleRecord(
            recordKey,
            recordTitle,
            validation.Code,
            validation.Status,
            validation.Comment,
            fields,
            relatedKey,
            relatedParameter);
    }

    private string? FindMachineName(int? machineId)
    {
        if (!machineId.HasValue)
        {
            return null;
        }

        return _machines.FirstOrDefault(m => m.Id == machineId.Value)?.Name;
    }

    private string? FindComponentName(int? componentId)
    {
        if (!componentId.HasValue)
        {
            return null;
        }

        return _components.FirstOrDefault(c => c.Id == componentId.Value)?.Name;
    }

    private void ReplaceRecord(Validation validation)
    {
        var key = validation.Id.ToString(CultureInfo.InvariantCulture);
        var index = Records.ToList().FindIndex(r => r.Key == key);
        if (index >= 0)
        {
            Records[index] = ToRecord(validation);
            SelectedRecord = Records[index];
        }
        else
        {
            Records.Add(ToRecord(validation));
            SelectedRecord = Records.Last();
        }
    }

    private bool CanAttachDocument()
        => !IsBusy
           && !IsEditorEnabled
           && _loadedValidation is { Id: > 0 };

    private async Task AttachDocumentAsync()
    {
        if (_loadedValidation is null || _loadedValidation.Id <= 0)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var files = await _filePicker
                .PickFilesAsync(new FilePickerRequest(AllowMultiple: true, Title: $"Attach files to {_loadedValidation.Code}"))
                .ConfigureAwait(false);

            if (files is null || files.Count == 0)
            {
                StatusMessage = "Attachment upload cancelled.";
                return;
            }

            var processed = 0;
            var deduplicated = 0;
            foreach (var file in files)
            {
                await using var stream = await file.OpenReadAsync().ConfigureAwait(false);
                var request = new AttachmentUploadRequest
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    EntityType = "validations",
                    EntityId = _loadedValidation.Id,
                    UploadedById = _authContext.CurrentUser?.Id,
                    Reason = $"validation:{_loadedValidation.Id}",
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
    /// <summary>
    /// Represents the validation editor value.
    /// </summary>

    public sealed partial class ValidationEditor : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _code = string.Empty;

        [ObservableProperty]
        private string _type = string.Empty;

        [ObservableProperty]
        private int? _machineId;

        [ObservableProperty]
        private string _machineName = string.Empty;

        [ObservableProperty]
        private int? _componentId;

        [ObservableProperty]
        private string _componentName = string.Empty;

        [ObservableProperty]
        private DateTime? _dateStart = DateTime.UtcNow.Date;

        [ObservableProperty]
        private DateTime? _dateEnd;

        [ObservableProperty]
        private DateTime? _nextDue = DateTime.UtcNow.Date.AddMonths(12);

        [ObservableProperty]
        private string _status = string.Empty;

        [ObservableProperty]
        private string _documentation = string.Empty;

        [ObservableProperty]
        private string _comment = string.Empty;
        /// <summary>
        /// Executes the create empty operation.
        /// </summary>

        public static ValidationEditor CreateEmpty() => new();
        /// <summary>
        /// Executes the create for new operation.
        /// </summary>

        public static ValidationEditor CreateForNew() => new();
        /// <summary>
        /// Executes the from validation operation.
        /// </summary>

        public static ValidationEditor FromValidation(
            Validation validation,
            Func<int?, string?> machineLookup,
            Func<int?, string?> componentLookup)
        {
            return new ValidationEditor
            {
                Id = validation.Id,
                Code = validation.Code ?? string.Empty,
                Type = validation.Type ?? string.Empty,
                MachineId = validation.MachineId,
                MachineName = machineLookup(validation.MachineId) ?? string.Empty,
                ComponentId = validation.ComponentId,
                ComponentName = componentLookup(validation.ComponentId) ?? string.Empty,
                DateStart = validation.DateStart,
                DateEnd = validation.DateEnd,
                NextDue = validation.NextDue,
                Status = validation.Status ?? string.Empty,
                Documentation = validation.Documentation ?? string.Empty,
                Comment = validation.Comment ?? string.Empty
            };
        }
        /// <summary>
        /// Executes the to validation operation.
        /// </summary>

        public Validation ToValidation(Validation? existing)
        {
            var validation = existing is null ? new Validation() : CloneValidation(existing);
            validation.Id = Id;
            validation.Code = Code ?? string.Empty;
            validation.Type = Type ?? string.Empty;
            validation.MachineId = MachineId;
            validation.ComponentId = ComponentId;
            validation.DateStart = DateStart;
            validation.DateEnd = DateEnd;
            validation.NextDue = NextDue;
            validation.Status = Status ?? string.Empty;
            validation.Documentation = Documentation ?? string.Empty;
            validation.Comment = Comment ?? string.Empty;
            return validation;
        }
        /// <summary>
        /// Executes the clone operation.
        /// </summary>

        public ValidationEditor Clone()
            => new()
            {
                Id = Id,
                Code = Code,
                Type = Type,
                MachineId = MachineId,
                MachineName = MachineName,
                ComponentId = ComponentId,
                ComponentName = ComponentName,
                DateStart = DateStart,
                DateEnd = DateEnd,
                NextDue = NextDue,
                Status = Status,
                Documentation = Documentation,
                Comment = Comment
            };

        private static Validation CloneValidation(Validation source)
        {
            return new Validation
            {
                Id = source.Id,
                Code = source.Code,
                Type = source.Type,
                MachineId = source.MachineId,
                ComponentId = source.ComponentId,
                DateStart = source.DateStart,
                DateEnd = source.DateEnd,
                Status = source.Status,
                Documentation = source.Documentation,
                Comment = source.Comment,
                NextDue = source.NextDue
            };
        }
    }
    /// <summary>
    /// Executes the struct operation.
    /// </summary>

    public readonly record struct MachineOption(int Id, string Name)
    {
        /// <summary>
        /// Executes the to string operation.
        /// </summary>
        public override string ToString() => Name;
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
}
