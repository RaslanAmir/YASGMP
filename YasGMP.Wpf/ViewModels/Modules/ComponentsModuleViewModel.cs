using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
/// Represents the components module view model value.
/// </summary>

public sealed partial class ComponentsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>
    /// Represents the module key value.
    /// </summary>
    public new const string ModuleKey = "Components";

    private static readonly string[] DefaultStatuses =
    {
        "active",
        "maintenance",
        "out-of-service",
        "decommissioned"
    };

    private static readonly string[] DefaultTypes =
    {
        "sensor",
        "assembly",
        "consumable",
        "control",
        "other"
    };

    private readonly IComponentCrudService _componentService;
    private readonly IMachineCrudService _machineService;
    private readonly IAuthContext _authContext;
    private readonly IFilePicker _filePicker;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private readonly IShellInteractionService _shellInteraction;
    private readonly ICodeGeneratorService _codeGeneratorService;
    private readonly IQRCodeService _qrCodeService;
    private readonly IPlatformService _platformService;

    private Component? _loadedComponent;
    private ComponentEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;
    private IReadOnlyList<Machine> _machines = Array.Empty<Machine>();
    /// <summary>
    /// Initializes a new instance of the ComponentsModuleViewModel class.
    /// </summary>

    public ComponentsModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IComponentCrudService componentService,
        IMachineCrudService machineService,
        IAuthContext authContext,
        IFilePicker filePicker,
        IAttachmentWorkflowService attachmentWorkflow,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization,
        ICodeGeneratorService codeGeneratorService,
        IQRCodeService qrCodeService,
        IPlatformService platformService)
        : base(ModuleKey, localization.GetString("Module.Title.Components"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _componentService = componentService ?? throw new ArgumentNullException(nameof(componentService));
        _machineService = machineService ?? throw new ArgumentNullException(nameof(machineService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        _shellInteraction = shellInteraction ?? throw new ArgumentNullException(nameof(shellInteraction));
        _codeGeneratorService = codeGeneratorService ?? throw new ArgumentNullException(nameof(codeGeneratorService));
        _qrCodeService = qrCodeService ?? throw new ArgumentNullException(nameof(qrCodeService));
        _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));

        Editor = ComponentEditor.CreateEmpty();
        StatusOptions = Array.AsReadOnly(DefaultStatuses);
        TypeOptions = Array.AsReadOnly(DefaultTypes);
        MachineOptions = new ObservableCollection<MachineOption>();

        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
        GenerateCodeCommand = new AsyncRelayCommand(GenerateCodeAsync, CanGenerateCode);
        PreviewQrCommand = new AsyncRelayCommand(PreviewQrAsync, CanPreviewQr);
    }

    [ObservableProperty]
    private ComponentEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;
    /// <summary>
    /// Command exposed to the toolbar for uploading attachments.
    /// </summary>

    public IAsyncRelayCommand AttachDocumentCommand { get; }
    /// <summary>
    /// Generates a component code and QR payload.
    /// </summary>

    public IAsyncRelayCommand GenerateCodeCommand { get; }
    /// <summary>
    /// Persists and previews the QR image for the current component.
    /// </summary>

    public IAsyncRelayCommand PreviewQrCommand { get; }
    /// <summary>
    /// Gets or sets the machine options.
    /// </summary>

    public ObservableCollection<MachineOption> MachineOptions { get; }
    /// <summary>
    /// Gets or sets the status options.
    /// </summary>

    public IReadOnlyList<string> StatusOptions { get; }
    /// <summary>
    /// Gets or sets the type options.
    /// </summary>

    public IReadOnlyList<string> TypeOptions { get; }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        _machines = await _machineService.GetAllAsync().ConfigureAwait(false);
        RefreshMachineOptions(_machines);

        var components = await _componentService.GetAllAsync().ConfigureAwait(false);
        return components.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var machines = new List<Machine>
        {
            new()
            {
                Id = 101,
                Name = "Lyophilizer",
                Code = "AST-500"
            },
            new()
            {
                Id = 102,
                Name = "Autoclave",
                Code = "AUTO-01"
            }
        };

        _machines = machines;
        RefreshMachineOptions(machines);

        var sample = new List<Component>
        {
            new()
            {
                Id = 2001,
                MachineId = 101,
                MachineName = "Lyophilizer",
                Code = "CMP-LYO-01",
                Name = "Shelf Temperature Probe",
                Type = "sensor",
                SopDoc = "SOP-LYO-01",
                Status = "active",
                InstallDate = DateTime.UtcNow.AddMonths(-8),
                Supplier = "Contoso Sensors",
                SerialNumber = "LYO-PRB-2024-09",
                Comments = "Calibrated quarterly.",
                LifecycleState = "in-use"
            },
            new()
            {
                Id = 2002,
                MachineId = 102,
                MachineName = "Autoclave",
                Code = "CMP-AUTO-VAL",
                Name = "Steam Valve Assembly",
                Type = "assembly",
                SopDoc = "SOP-AUTO-VAL",
                Status = "maintenance",
                InstallDate = DateTime.UtcNow.AddYears(-2),
                Supplier = "Steris",
                SerialNumber = "AUTO-VAL-11",
                Comments = "Awaiting spare kit.",
                LifecycleState = "maintenance"
            }
        };

        return sample.Select(ToRecord).ToList();
    }

    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var components = await _componentService.GetAllAsync().ConfigureAwait(false);
        var items = components
            .Select(component =>
            {
                var key = component.Id.ToString(CultureInfo.InvariantCulture);
                var label = string.IsNullOrWhiteSpace(component.Name) ? key : component.Name;
                var descriptionParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(component.Code))
                {
                    descriptionParts.Add(component.Code);
                }

                if (!string.IsNullOrWhiteSpace(component.MachineName))
                {
                    descriptionParts.Add(component.MachineName);
                }

                if (!string.IsNullOrWhiteSpace(component.Status))
                {
                    descriptionParts.Add(component.Status);
                }

                var description = descriptionParts.Count > 0
                    ? string.Join(" • ", descriptionParts)
                    : null;

                return new CflItem(key, label, description);
            })
            .ToList();

        return new CflRequest("Select Component", items);
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
            _loadedComponent = null;
            SetEditor(ComponentEditor.CreateEmpty());
            UpdateCommandStates();
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

        var component = await _componentService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (component is null)
        {
            StatusMessage = $"Unable to locate component #{id}.";
            return;
        }

        _loadedComponent = component;
        LoadEditor(component);
        await InitializeEditorIdentifiersAsync(resetDirty: true).ConfigureAwait(false);
        UpdateCommandStates();
    }

    protected override async Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        switch (mode)
        {
            case FormMode.Add:
                _snapshot = null;
                _loadedComponent = null;
                var defaultMachineId = GetDefaultMachineId();
                var defaultMachineName = ResolveMachineName(defaultMachineId);
                SetEditor(ComponentEditor.CreateForNew(
                    _componentService.NormalizeStatus("active"),
                    defaultMachineId,
                    defaultMachineName));
                await InitializeEditorIdentifiersAsync(resetDirty: true).ConfigureAwait(false);
                break;
            case FormMode.Update:
                _snapshot = Editor.Clone();
                await InitializeEditorIdentifiersAsync().ConfigureAwait(false);
                break;
            case FormMode.View:
                _snapshot = null;
                break;
        }

        UpdateCommandStates();
    }

    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var errors = new List<string>();
        try
        {
            var component = Editor.ToComponent(_loadedComponent);
            component.Status = _componentService.NormalizeStatus(component.Status);
            component.MachineName = ResolveMachineName(component.MachineId);
            _componentService.Validate(component);
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
        var component = Editor.ToComponent(_loadedComponent);
        component.Status = _componentService.NormalizeStatus(component.Status);
        component.MachineName = ResolveMachineName(component.MachineId);

        if (Mode == FormMode.Update && _loadedComponent is null)
        {
            StatusMessage = "Select a component before saving.";
            return false;
        }

        var recordId = Mode == FormMode.Update ? _loadedComponent!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("components", recordId))
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

        component.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;
        component.LastModified = DateTime.UtcNow;
        component.LastModifiedById = _authContext.CurrentUser?.Id ?? component.LastModifiedById;
        component.SourceIp = _authContext.CurrentIpAddress ?? component.SourceIp ?? string.Empty;

        var context = ComponentCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        Component adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _componentService.CreateAsync(component, context).ConfigureAwait(false);
                if (component.Id == 0 && saveResult.Id > 0)
                {
                    component.Id = saveResult.Id;
                }

                adapterResult = component;
            }
            else if (Mode == FormMode.Update)
            {
                component.Id = _loadedComponent!.Id;
                saveResult = await _componentService.UpdateAsync(component, context).ConfigureAwait(false);
                adapterResult = component;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist component: {ex.Message}", ex);
        }

        _loadedComponent = component;
        LoadEditor(component);

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "components",
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
        UpdateCommandStates();
        return true;
    }

    protected override void OnCancel()
    {
        if (Mode == FormMode.Add)
        {
            if (_loadedComponent is not null)
            {
                LoadEditor(_loadedComponent);
            }
            else
            {
                SetEditor(ComponentEditor.CreateEmpty());
            }
        }
        else if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
        }

        UpdateCommandStates();
    }

    partial void OnEditorChanging(ComponentEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(ComponentEditor value)
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

        if (string.Equals(e.PropertyName, nameof(ComponentEditor.MachineId), StringComparison.Ordinal))
        {
            _suppressEditorDirtyNotifications = true;
            Editor.MachineName = ResolveMachineName(Editor.MachineId);
            _suppressEditorDirtyNotifications = false;
        }

        if (IsInEditMode)
        {
            MarkDirty();
        }

        UpdateCommandStates();
    }

    private void LoadEditor(Component component)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = ComponentEditor.FromComponent(component, _componentService.NormalizeStatus, ResolveMachineName);
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateCommandStates();
    }

    private void SetEditor(ComponentEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateCommandStates();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is nameof(IsBusy) or nameof(Mode) or nameof(SelectedRecord) or nameof(IsDirty))
        {
            UpdateCommandStates();
        }
    }

    private void RefreshMachineOptions(IEnumerable<Machine> machines)
    {
        MachineOptions.Clear();
        foreach (var option in machines
                     .OrderBy(static m => string.IsNullOrWhiteSpace(m.Name) ? m.Code : m.Name)
                     .Select(m => new MachineOption(m.Id, ResolveMachineDisplayName(m))))
        {
            MachineOptions.Add(option);
        }
    }

    private int GetDefaultMachineId()
    {
        if (_loadedComponent is not null)
        {
            return _loadedComponent.MachineId;
        }

        if (SelectedRecord?.RelatedParameter is int relatedId)
        {
            return relatedId;
        }

        if (SelectedRecord?.RelatedParameter is string relatedKey
            && int.TryParse(relatedKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return _machines.FirstOrDefault()?.Id ?? 0;
    }

    private string ResolveMachineName(int machineId)
    {
        if (machineId <= 0)
        {
            return string.Empty;
        }

        var machine = _machines.FirstOrDefault(m => m.Id == machineId);
        return machine is null ? string.Empty : ResolveMachineDisplayName(machine);
    }

    private static string ResolveMachineDisplayName(Machine machine)
    {
        if (!string.IsNullOrWhiteSpace(machine.Name))
        {
            return machine.Name;
        }

        if (!string.IsNullOrWhiteSpace(machine.Code))
        {
            return machine.Code;
        }

        return $"Machine #{machine.Id}";
    }

    private ModuleRecord ToRecord(Component component)
    {
        var inspector = new List<InspectorField>
        {
            new("Machine", string.IsNullOrWhiteSpace(component.MachineName)
                ? ResolveMachineName(component.MachineId)
                : component.MachineName),
            new("Type", string.IsNullOrWhiteSpace(component.Type) ? "-" : component.Type!),
            new("Status", string.IsNullOrWhiteSpace(component.Status) ? "-" : component.Status!),
            new("SOP", string.IsNullOrWhiteSpace(component.SopDoc) ? "-" : component.SopDoc!),
            new("Lifecycle", string.IsNullOrWhiteSpace(component.LifecycleState) ? "-" : component.LifecycleState!)
        };

        return new ModuleRecord(
            component.Id.ToString(CultureInfo.InvariantCulture),
            component.Name,
            component.Code,
            component.Status,
            component.Comments,
            inspector,
            AssetsModuleViewModel.ModuleKey,
            component.MachineId);
    }

    private bool CanGenerateCode()
        => !IsBusy && IsInEditMode;

    private bool CanPreviewQr()
        => !IsBusy && Editor is not null && !string.IsNullOrWhiteSpace(Editor.QrPayload);

    private bool CanAttachDocument()
        => !IsBusy
           && !IsEditorEnabled
           && _loadedComponent is { Id: > 0 };

    private async Task GenerateCodeAsync()
    {
        if (!IsInEditMode)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var code = EnsureEditorCode(force: true, suppressDirty: false);
            var payload = EnsureEditorQrPayload(code, suppressDirty: false);
            var path = await EnsureEditorQrImageAsync(payload, code, suppressDirty: false).ConfigureAwait(false);
            StatusMessage = string.IsNullOrWhiteSpace(path)
                ? $"Generated component code {code}, but QR path is unavailable."
                : $"Generated component code {code} and QR image at {path}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"QR generation failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            UpdateCommandStates();
        }
    }

    private async Task PreviewQrAsync()
    {
        try
        {
            IsBusy = true;
            var component = Editor.ToComponent(_loadedComponent);
            await SynchronizeIdentifiersAsync(component, forceCode: false, suppressDirty: false).ConfigureAwait(false);
            var path = component.QrCode ?? Editor.QrCode;
            if (string.IsNullOrWhiteSpace(path))
            {
                StatusMessage = "QR generation failed: QR path unavailable.";
            }
            else
            {
                _shellInteraction.PreviewDocument(path);
                StatusMessage = $"QR generated at {path}.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"QR generation failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            UpdateCommandStates();
        }
    }

    private async Task AttachDocumentAsync()
    {
        if (_loadedComponent is null || _loadedComponent.Id <= 0)
        {
            StatusMessage = "Save the component before adding attachments.";
            return;
        }

        try
        {
            IsBusy = true;
            var files = await _filePicker
                .PickFilesAsync(new FilePickerRequest(AllowMultiple: true, Title: $"Attach files to component #{_loadedComponent.Id}"))
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
                    EntityType = "components",
                    EntityId = _loadedComponent.Id,
                    UploadedById = uploadedBy,
                    Reason = $"component:{_loadedComponent.Id}",
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
            UpdateCommandStates();
        }
    }

    private async Task InitializeEditorIdentifiersAsync(bool resetDirty = false)
    {
        if (Editor is null)
        {
            return;
        }

        var component = Editor.ToComponent(_loadedComponent);
        try
        {
            await SynchronizeIdentifiersAsync(
                    component,
                    forceCode: false,
                    suppressDirty: true,
                    resetDirtyAfter: resetDirty)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"QR generation failed: {ex.Message}";
        }
    }

    private async Task SynchronizeIdentifiersAsync(
        Component component,
        bool forceCode,
        bool suppressDirty,
        bool resetDirtyAfter = false,
        CancellationToken cancellationToken = default)
    {
        if (component is null)
        {
            return;
        }

        var code = EnsureEditorCode(forceCode, suppressDirty);
        var payload = EnsureEditorQrPayload(code, suppressDirty);
        var path = await EnsureEditorQrImageAsync(payload, code, suppressDirty, cancellationToken).ConfigureAwait(false);

        component.Code = code;
        component.QrPayload = payload;
        component.QrCode = path;

        if (resetDirtyAfter)
        {
            ResetDirty();
        }
    }

    private string EnsureEditorCode(bool force, bool suppressDirty)
    {
        if (Editor.IsCodeOverrideEnabled && !string.IsNullOrWhiteSpace(Editor.CodeOverride))
        {
            var overrideValue = Editor.CodeOverride.Trim();
            if (suppressDirty)
            {
                ExecuteWithDirtySuppression(() => Editor.Code = overrideValue);
            }
            else
            {
                Editor.Code = overrideValue;
            }

            return Editor.Code;
        }

        if (!force && !string.IsNullOrWhiteSpace(Editor.Code))
        {
            return Editor.Code;
        }

        var machineName = ResolveMachineName(Editor.MachineId);
        var generated = _codeGeneratorService.GenerateMachineCode(Editor.Name, machineName);

        if (string.IsNullOrWhiteSpace(generated))
        {
            throw new InvalidOperationException("Unable to generate a component code.");
        }

        if (Editor.IsCodeOverrideEnabled)
        {
            ExecuteWithDirtySuppression(() =>
            {
                Editor.IsCodeOverrideEnabled = false;
                Editor.CodeOverride = string.Empty;
            });
        }

        if (suppressDirty)
        {
            ExecuteWithDirtySuppression(() => Editor.Code = generated);
        }
        else
        {
            Editor.Code = generated;
        }

        return Editor.Code;
    }

    private string EnsureEditorQrPayload(string code, bool suppressDirty)
    {
        var payload = BuildQrPayload(code, Editor.MachineId);
        if (string.Equals(Editor.QrPayload, payload, StringComparison.Ordinal))
        {
            return payload;
        }

        if (suppressDirty)
        {
            ExecuteWithDirtySuppression(() => Editor.QrPayload = payload);
        }
        else
        {
            Editor.QrPayload = payload;
        }

        return payload;
    }

    private async Task<string> EnsureEditorQrImageAsync(
        string payload,
        string code,
        bool suppressDirty,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new InvalidOperationException("QR payload is required before generating an image.");
        }

        var path = await SaveQrImageAsync(payload, code, Editor.Id, cancellationToken).ConfigureAwait(false);

        if (suppressDirty)
        {
            ExecuteWithDirtySuppression(() => Editor.QrCode = path);
        }
        else
        {
            Editor.QrCode = path;
        }

        return path;
    }

    private static string BuildQrPayload(string code, int machineId)
    {
        var identifier = string.IsNullOrWhiteSpace(code)
            ? "pending"
            : Uri.EscapeDataString(code.Trim());
        var machineSegment = machineId > 0 ? $"?machine={machineId}" : string.Empty;
        return $"yasgmp://component/{identifier}{machineSegment}";
    }

    private async Task<string> SaveQrImageAsync(
        string payload,
        string code,
        int editorId,
        CancellationToken cancellationToken)
    {
        var appData = _platformService.GetAppDataDirectory();
        var qrDirectory = Path.Combine(appData, "Components", "QrCodes");
        Directory.CreateDirectory(qrDirectory);

        var hint = !string.IsNullOrWhiteSpace(code)
            ? code
            : editorId > 0
                ? editorId.ToString(CultureInfo.InvariantCulture)
                : Guid.NewGuid().ToString("N");
        var fileName = $"{SanitizeFileName(hint)}.png";
        var path = Path.Combine(qrDirectory, fileName);

        using var pngStream = _qrCodeService.GeneratePng(payload);
        if (pngStream.CanSeek)
        {
            pngStream.Position = 0;
        }

        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, useAsync: true);
        await pngStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
        await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);

        return path;
    }

    private static string SanitizeFileName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "component";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            builder.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);
        }

        return builder.ToString();
    }

    private void ExecuteWithDirtySuppression(Action action)
    {
        var previous = _suppressEditorDirtyNotifications;
        _suppressEditorDirtyNotifications = true;
        try
        {
            action();
        }
        finally
        {
            _suppressEditorDirtyNotifications = previous;
        }
    }

    private void UpdateCommandStates()
    {
        AttachDocumentCommand.NotifyCanExecuteChanged();
        GenerateCodeCommand.NotifyCanExecuteChanged();
        PreviewQrCommand.NotifyCanExecuteChanged();
    }
    /// <summary>
    /// Represents the component editor value.
    /// </summary>

    public sealed partial class ComponentEditor : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private int _machineId;

        [ObservableProperty]
        private string _machineName = string.Empty;

        [ObservableProperty]
        private string _code = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _type = string.Empty;

        [ObservableProperty]
        private string _sopDoc = string.Empty;

        [ObservableProperty]
        private string _status = "active";

        [ObservableProperty]
        private DateTime? _installDate;

        [ObservableProperty]
        private string _serialNumber = string.Empty;

        [ObservableProperty]
        private string _supplier = string.Empty;

        [ObservableProperty]
        private DateTime? _warrantyUntil;

        [ObservableProperty]
        private string _comments = string.Empty;

        [ObservableProperty]
        private string _lifecycleState = string.Empty;

        [ObservableProperty]
        private string _qrCode = string.Empty;

        [ObservableProperty]
        private string _qrPayload = string.Empty;

        [ObservableProperty]
        private string _codeOverride = string.Empty;

        [ObservableProperty]
        private bool _isCodeOverrideEnabled;

        [ObservableProperty]
        private ObservableCollection<string> _linkedDocuments = new();
        /// <summary>
        /// Executes the create empty operation.
        /// </summary>

        public static ComponentEditor CreateEmpty() => new()
        {
            QrCode = string.Empty,
            QrPayload = string.Empty,
            CodeOverride = string.Empty,
            IsCodeOverrideEnabled = false,
            LinkedDocuments = new ObservableCollection<string>()
        };
        /// <summary>
        /// Executes the create for new operation.
        /// </summary>

        public static ComponentEditor CreateForNew(string normalizedStatus, int machineId, string machineName)
            => new()
            {
                Status = normalizedStatus,
                MachineId = machineId,
                MachineName = machineName,
                QrCode = string.Empty,
                QrPayload = string.Empty,
                CodeOverride = string.Empty,
                IsCodeOverrideEnabled = false,
                LinkedDocuments = new ObservableCollection<string>()
            };
        /// <summary>
        /// Executes the from component operation.
        /// </summary>

        public static ComponentEditor FromComponent(
            Component component,
            Func<string?, string> statusNormalizer,
            Func<int, string> machineNameResolver)
        {
            return new ComponentEditor
            {
                Id = component.Id,
                MachineId = component.MachineId,
                MachineName = string.IsNullOrWhiteSpace(component.MachineName)
                    ? machineNameResolver(component.MachineId)
                    : component.MachineName,
                Code = component.Code ?? string.Empty,
                Name = component.Name ?? string.Empty,
                Type = component.Type ?? string.Empty,
                SopDoc = component.SopDoc ?? string.Empty,
                Status = statusNormalizer(component.Status),
                InstallDate = component.InstallDate,
                SerialNumber = component.SerialNumber ?? string.Empty,
                Supplier = component.Supplier ?? string.Empty,
                WarrantyUntil = component.WarrantyUntil,
                Comments = component.Comments ?? string.Empty,
                LifecycleState = component.LifecycleState ?? string.Empty,
                QrCode = component.QrCode ?? string.Empty,
                QrPayload = component.QrPayload ?? string.Empty,
                CodeOverride = component.CodeOverride ?? string.Empty,
                IsCodeOverrideEnabled = component.IsCodeOverrideEnabled,
                LinkedDocuments = new ObservableCollection<string>(component.LinkedDocuments ?? new List<string>())
            };
        }
        /// <summary>
        /// Executes the to component operation.
        /// </summary>

        public Component ToComponent(Component? existing)
        {
            var component = existing is null ? new Component() : CloneComponent(existing);
            component.Id = Id;
            component.MachineId = MachineId;
            component.MachineName = MachineName;
            component.Code = Code;
            component.Name = Name;
            component.Type = Type;
            component.SopDoc = SopDoc;
            component.Status = Status;
            component.InstallDate = InstallDate;
            component.SerialNumber = SerialNumber;
            component.Supplier = Supplier;
            component.WarrantyUntil = WarrantyUntil;
            component.Comments = Comments;
            component.LifecycleState = LifecycleState;
            component.QrCode = string.IsNullOrWhiteSpace(QrCode) ? string.Empty : QrCode.Trim();
            component.QrPayload = string.IsNullOrWhiteSpace(QrPayload) ? string.Empty : QrPayload.Trim();
            component.CodeOverride = CodeOverride ?? string.Empty;
            component.IsCodeOverrideEnabled = IsCodeOverrideEnabled;
            component.LinkedDocuments = LinkedDocuments?.ToList() ?? new List<string>();
            return component;
        }
        /// <summary>
        /// Executes the clone operation.
        /// </summary>

        public ComponentEditor Clone()
            => new()
            {
                Id = Id,
                MachineId = MachineId,
                MachineName = MachineName,
                Code = Code,
                Name = Name,
                Type = Type,
                SopDoc = SopDoc,
                Status = Status,
                InstallDate = InstallDate,
                SerialNumber = SerialNumber,
                Supplier = Supplier,
                WarrantyUntil = WarrantyUntil,
                Comments = Comments,
                LifecycleState = LifecycleState,
                QrCode = QrCode,
                QrPayload = QrPayload,
                CodeOverride = CodeOverride,
                IsCodeOverrideEnabled = IsCodeOverrideEnabled,
                LinkedDocuments = new ObservableCollection<string>(LinkedDocuments)
            };

        private static Component CloneComponent(Component source)
        {
            return new Component
            {
                Id = source.Id,
                MachineId = source.MachineId,
                MachineName = source.MachineName,
                Code = source.Code,
                Name = source.Name,
                Type = source.Type,
                SopDoc = source.SopDoc,
                Status = source.Status,
                InstallDate = source.InstallDate,
                SerialNumber = source.SerialNumber,
                Supplier = source.Supplier,
                WarrantyUntil = source.WarrantyUntil,
                Comments = source.Comments,
                LifecycleState = source.LifecycleState,
                QrCode = source.QrCode,
                QrPayload = source.QrPayload,
                CodeOverride = source.CodeOverride,
                IsCodeOverrideEnabled = source.IsCodeOverrideEnabled,
                LinkedDocuments = new List<string>(source.LinkedDocuments)
            };
        }
    }
    /// <summary>
    /// Represents the Machine Option record.
    /// </summary>

    public sealed record MachineOption(int Id, string Name);
}
