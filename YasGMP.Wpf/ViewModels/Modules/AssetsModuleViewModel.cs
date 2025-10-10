using System;
using System.Collections;
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
/// Represents the assets module view model value.
/// </summary>

public sealed partial class AssetsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>
    /// Represents the module key value.
    /// </summary>
    public new const string ModuleKey = "Assets";

    private readonly IMachineCrudService _machineService;
    private readonly IAuthContext _authContext;
    private readonly IFilePicker _filePicker;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private readonly ILocalizationService _localization;
    private Machine? _loadedMachine;
    private AssetEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;
    /// <summary>
    /// Initializes a new instance of the AssetsModuleViewModel class.
    /// </summary>

    public AssetsModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IMachineCrudService machineService,
        IAuthContext authContext,
        IFilePicker filePicker,
        IAttachmentWorkflowService attachmentWorkflow,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, localization.GetString("Module.Title.Assets"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _machineService = machineService ?? throw new ArgumentNullException(nameof(machineService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        Editor = AssetEditor.CreateEmpty();
        StatusOptions = new ReadOnlyCollection<string>(new[]
        {
            _localization.GetString("Module.Assets.Status.Active"),
            _localization.GetString("Module.Assets.Status.Maintenance"),
            _localization.GetString("Module.Assets.Status.Reserved"),
            _localization.GetString("Module.Assets.Status.Decommissioned"),
            _localization.GetString("Module.Assets.Status.Scrapped")
        });

        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
    }

    /// <summary>Editor payload bound to the form fields.</summary>
    [ObservableProperty]
    private AssetEditor _editor;

    /// <summary>Indicates whether form controls are writable (Add/Update modes).</summary>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Canonical status options rendered in the combo-box.</summary>
    public IReadOnlyList<string> StatusOptions { get; }

    /// <summary>Command exposed to the toolbar for uploading attachments.</summary>
    public IAsyncRelayCommand AttachDocumentCommand { get; }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var (target, machines, filterActive, searchTerm) = await ResolveNavigationPayloadAsync(parameter).ConfigureAwait(false);

        var records = new List<ModuleRecord>();
        ModuleRecord? selectedRecord = null;

        foreach (var machine in machines)
        {
            var record = ToRecord(machine);
            records.Add(record);

            if (target is not null && machine.Id == target.Id)
            {
                selectedRecord = record;
            }
        }

        if (target is not null && selectedRecord is null)
        {
            selectedRecord = ToRecord(target);
            records.Insert(0, selectedRecord);
        }

        if (target is not null && selectedRecord is not null)
        {
            var matchIndex = records.IndexOf(selectedRecord);
            if (matchIndex > 0)
            {
                records.RemoveAt(matchIndex);
                records.Insert(0, selectedRecord);
            }
        }

        if (filterActive)
        {
            if (target is not null && ApplyNavigationSelection(target, records, searchTerm))
            {
                return records;
            }

            if (selectedRecord is not null)
            {
                SelectedRecord = selectedRecord;
                var search = string.IsNullOrWhiteSpace(searchTerm)
                    ? (string.IsNullOrWhiteSpace(selectedRecord.Title)
                        ? (string.IsNullOrWhiteSpace(selectedRecord.Code) ? selectedRecord.Key : selectedRecord.Code)
                        : selectedRecord.Title)
                    : searchTerm;

                SearchText = search;
                StatusMessage = _localization.GetString("Module.Status.Filtered", Title, search);
            }
            else if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                SearchText = searchTerm;
                StatusMessage = _localization.GetString("Module.Status.Filtered", Title, searchTerm);
                SelectedRecord = null;
            }

            return records;
        }

        if (target is not null)
        {
            ApplyNavigationSelection(target, records, searchTerm);
        }

        return records;
    }

    private bool ApplyNavigationSelection(Machine target, IReadOnlyList<ModuleRecord>? records = null, string? searchOverride = null)
    {
        var source = records ?? Records;
        if (source is null || source.Count == 0)
        {
            return false;
        }

        var key = target.Id.ToString(CultureInfo.InvariantCulture);
        var match = source.FirstOrDefault(record => string.Equals(record.Key, key, StringComparison.Ordinal));
        if (match is null)
        {
            return false;
        }

        var forceReload = ReferenceEquals(SelectedRecord, match);
        if (forceReload)
        {
            SelectedRecord = null;
        }

        SelectedRecord = match;

        var search = !string.IsNullOrWhiteSpace(searchOverride)
            ? searchOverride!
            : string.IsNullOrWhiteSpace(match.Title)
                ? (string.IsNullOrWhiteSpace(match.Code) ? key : match.Code)
                : match.Title;

        SearchText = search;
        StatusMessage = _localization.GetString("Module.Status.Filtered", Title, search);
        return true;
    }

    private async Task<(Machine? Target, IReadOnlyList<Machine> Machines, bool FilterActive, string? SearchTerm)> ResolveNavigationPayloadAsync(object? parameter)
    {
        string? searchOverride = null;
        if (TryNormalizeNavigationParameter(parameter, out var normalized, out var overrideText))
        {
            parameter = normalized;
            searchOverride = overrideText;
        }

        if (parameter is int id)
        {
            var target = await _machineService.TryGetByIdAsync(id).ConfigureAwait(false);
            var search = searchOverride ?? id.ToString(CultureInfo.InvariantCulture);
            if (target is null)
            {
                return (null, Array.Empty<Machine>(), true, search);
            }

            return (target, new[] { target }, true, search);
        }

        if (parameter is string text)
        {
            var trimmed = text.Trim();
            if (trimmed.Length == 0)
            {
                var machines = await _machineService.GetAllAsync().ConfigureAwait(false);
                return (null, machines, false, null);
            }

            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericId))
            {
                var target = await _machineService.TryGetByIdAsync(numericId).ConfigureAwait(false);
                if (target is null)
                {
                    var search = searchOverride ?? trimmed;
                    return (null, Array.Empty<Machine>(), true, search);
                }

                var search = searchOverride ?? trimmed;
                return (target, new[] { target }, true, search);
            }

            var machines = await _machineService.GetAllAsync().ConfigureAwait(false);
            var matches = machines
                .Where(machine =>
                    (!string.IsNullOrWhiteSpace(machine.Code) && machine.Code.Contains(trimmed, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(machine.Name) && machine.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var target = matches.FirstOrDefault();
            var search = searchOverride ?? trimmed;
            return (target, matches, true, search);
        }

        if (parameter is Machine machine)
        {
            var search = searchOverride
                ?? (!string.IsNullOrWhiteSpace(machine.Code)
                    ? machine.Code
                    : machine.Id > 0
                        ? machine.Id.ToString(CultureInfo.InvariantCulture)
                        : machine.Name);

            if (machine.Id > 0)
            {
                var target = await _machineService.TryGetByIdAsync(machine.Id).ConfigureAwait(false) ?? machine;
                return (target, new[] { target }, true, search);
            }

            if (!string.IsNullOrWhiteSpace(machine.Code))
            {
                return await ResolveNavigationPayloadAsync(machine.Code).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(machine.Name))
            {
                var machines = await _machineService.GetAllAsync().ConfigureAwait(false);
                var matches = machines
                    .Where(m => string.Equals(m.Name, machine.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var target = matches.FirstOrDefault();
                return (target, matches, true, search);
            }

            return (null, Array.Empty<Machine>(), true, search);
        }

        if (parameter is ModuleRecord record)
        {
            var nextParameter = record.RelatedParameter ?? (!string.IsNullOrWhiteSpace(record.Code) ? record.Code : record.Key);
            return await ResolveNavigationPayloadAsync(nextParameter).ConfigureAwait(false);
        }

        var allMachines = await _machineService.GetAllAsync().ConfigureAwait(false);
        return (null, allMachines, false, null);
    }

    private static bool TryNormalizeNavigationParameter(object? parameter, out object? normalized, out string? searchOverride)
    {
        normalized = parameter;
        searchOverride = null;

        if (parameter is null)
        {
            return false;
        }

        if (parameter is ModuleRecord record)
        {
            var candidate = record.RelatedParameter
                ?? (!string.IsNullOrWhiteSpace(record.Code) ? record.Code : record.Key);

            if (candidate is null)
            {
                return false;
            }

            return TryNormalizeNavigationParameter(candidate, out normalized, out searchOverride);
        }

        if (parameter is Machine machine)
        {
            if (machine.Id > 0)
            {
                normalized = machine.Id;
                searchOverride = !string.IsNullOrWhiteSpace(machine.Code)
                    ? machine.Code
                    : machine.Id.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            if (!string.IsNullOrWhiteSpace(machine.Code))
            {
                normalized = machine.Code;
                searchOverride = machine.Code.Trim();
                return true;
            }

            if (!string.IsNullOrWhiteSpace(machine.Name))
            {
                normalized = machine.Name;
                searchOverride = machine.Name.Trim();
                return true;
            }

            return false;
        }

        if (TryGetNavigationValueFromDictionary(parameter, out var dictionaryValue))
        {
            return TryNormalizeNavigationParameter(dictionaryValue, out normalized, out searchOverride);
        }

        switch (parameter)
        {
            case string text:
                normalized = text;
                searchOverride = text.Trim();
                return true;
            case int intValue:
                normalized = intValue;
                searchOverride = intValue.ToString(CultureInfo.InvariantCulture);
                return true;
            case long longValue:
                if (longValue <= int.MaxValue && longValue >= int.MinValue)
                {
                    normalized = (int)longValue;
                }
                else
                {
                    normalized = longValue.ToString(CultureInfo.InvariantCulture);
                }

                searchOverride = Convert.ToString(longValue, CultureInfo.InvariantCulture);
                return true;
            case uint uintValue:
                if (uintValue <= int.MaxValue)
                {
                    normalized = (int)uintValue;
                }
                else
                {
                    normalized = uintValue.ToString(CultureInfo.InvariantCulture);
                }

                searchOverride = Convert.ToString(uintValue, CultureInfo.InvariantCulture);
                return true;
            case ushort ushortValue:
                normalized = (int)ushortValue;
                searchOverride = ushortValue.ToString(CultureInfo.InvariantCulture);
                return true;
            case short shortValue:
                normalized = (int)shortValue;
                searchOverride = shortValue.ToString(CultureInfo.InvariantCulture);
                return true;
            case byte byteValue:
                normalized = (int)byteValue;
                searchOverride = byteValue.ToString(CultureInfo.InvariantCulture);
                return true;
            case sbyte sbyteValue:
                normalized = (int)sbyteValue;
                searchOverride = sbyteValue.ToString(CultureInfo.InvariantCulture);
                return true;
            case ulong ulongValue:
                if (ulongValue <= int.MaxValue)
                {
                    normalized = (int)ulongValue;
                }
                else
                {
                    normalized = ulongValue.ToString(CultureInfo.InvariantCulture);
                }

                searchOverride = Convert.ToString(ulongValue, CultureInfo.InvariantCulture);
                return true;
            case float floatValue:
                normalized = floatValue.ToString(CultureInfo.InvariantCulture);
                searchOverride = normalized as string;
                return true;
            case double doubleValue:
                normalized = doubleValue.ToString(CultureInfo.InvariantCulture);
                searchOverride = normalized as string;
                return true;
            case decimal decimalValue:
                normalized = decimalValue.ToString(CultureInfo.InvariantCulture);
                searchOverride = normalized as string;
                return true;
        }

        return false;
    }

    private static bool TryGetNavigationValueFromDictionary(object? candidate, out object? value)
    {
        if (candidate is null)
        {
            value = null;
            return false;
        }

        if (candidate is IReadOnlyDictionary<string, object?> readOnlyDictionary
            && TryGetNavigationValueFromPairs(readOnlyDictionary, out value))
        {
            return true;
        }

        if (candidate is IDictionary<string, object?> typedDictionary
            && TryGetNavigationValueFromPairs(typedDictionary, out value))
        {
            return true;
        }

        if (candidate is IDictionary dictionary)
        {
            object? best = null;
            var bestPriority = int.MaxValue;

            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is not string key)
                {
                    continue;
                }

                if (!TryGetNavigationKeyPriority(key, out var priority))
                {
                    continue;
                }

                if (priority < bestPriority)
                {
                    bestPriority = priority;
                    best = entry.Value;

                    if (priority == 0)
                    {
                        break;
                    }
                }
            }

            if (bestPriority != int.MaxValue)
            {
                value = best;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static bool TryGetNavigationValueFromPairs(IEnumerable<KeyValuePair<string, object?>> pairs, out object? value)
    {
        object? best = null;
        var bestPriority = int.MaxValue;

        foreach (var pair in pairs)
        {
            if (!TryGetNavigationKeyPriority(pair.Key, out var priority))
            {
                continue;
            }

            if (priority < bestPriority)
            {
                bestPriority = priority;
                best = pair.Value;

                if (priority == 0)
                {
                    break;
                }
            }
        }

        if (bestPriority == int.MaxValue)
        {
            value = null;
            return false;
        }

        value = best;
        return true;
    }

    private static bool TryGetNavigationKeyPriority(string? key, out int priority)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            priority = int.MaxValue;
            return false;
        }

        if (string.Equals(key, "machineId", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "assetId", StringComparison.OrdinalIgnoreCase))
        {
            priority = 0;
            return true;
        }

        if (string.Equals(key, "id", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "recordId", StringComparison.OrdinalIgnoreCase))
        {
            priority = 1;
            return true;
        }

        if (string.Equals(key, "code", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "assetCode", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "machineCode", StringComparison.OrdinalIgnoreCase))
        {
            priority = 2;
            return true;
        }

        priority = int.MaxValue;
        return false;
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var sample = new[]
        {
            new Machine
            {
                Id = 1001,
                Name = "Autoclave",
                Code = "AUTO-001",
                Status = StatusOptions[0],
                Description = "Steam sterilizer",
                Manufacturer = "Steris",
                Location = "Building A",
                InstallDate = DateTime.UtcNow.AddYears(-3)
            },
            new Machine
            {
                Id = 1002,
                Name = "pH Meter",
                Code = "LAB-PH-12",
                Status = StatusOptions[1],
                Description = "Metrohm pH meter",
                Manufacturer = "Metrohm",
                Location = "QC Lab",
                InstallDate = DateTime.UtcNow.AddYears(-2)
            }
        };

        return sample.Select(ToRecord).ToList();
    }

    protected override async Task OnActivatedAsync(object? parameter)
    {
        if (parameter is null)
        {
            return;
        }

        if (parameter is string text && string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var (target, _, filterActive, searchTerm) = await ResolveNavigationPayloadAsync(parameter).ConfigureAwait(false);
        if (target is null)
        {
            if (filterActive && !string.IsNullOrWhiteSpace(searchTerm))
            {
                await RefreshAsync(parameter).ConfigureAwait(false);
                SearchText = searchTerm;
                StatusMessage = _localization.GetString("Module.Status.Filtered", Title, searchTerm);
            }

            return;
        }

        if (IsInEditMode)
        {
            await EnterViewModeCommand.ExecuteAsync(null).ConfigureAwait(false);
        }

        if (ApplyNavigationSelection(target, searchOverride: searchTerm))
        {
            return;
        }

        await RefreshAsync(parameter).ConfigureAwait(false);

        if (IsInEditMode)
        {
            await EnterViewModeCommand.ExecuteAsync(null).ConfigureAwait(false);
        }

        if (!ApplyNavigationSelection(target, searchOverride: searchTerm) && !string.IsNullOrWhiteSpace(searchTerm))
        {
            SearchText = searchTerm;
            StatusMessage = _localization.GetString("Module.Status.Filtered", Title, searchTerm);
        }
    }

    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var machines = await _machineService.GetAllAsync().ConfigureAwait(false);
        var items = machines
            .Select(machine =>
            {
                var key = machine.Id.ToString(CultureInfo.InvariantCulture);
                var label = string.IsNullOrWhiteSpace(machine.Name) ? key : machine.Name;
                var descriptionParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(machine.Code))
                {
                    descriptionParts.Add(machine.Code);
                }

                if (!string.IsNullOrWhiteSpace(machine.Location))
                {
                    descriptionParts.Add(machine.Location!);
                }

                if (!string.IsNullOrWhiteSpace(machine.Status))
                {
                    descriptionParts.Add(machine.Status!);
                }

                var description = descriptionParts.Count > 0
                    ? string.Join(" • ", descriptionParts)
                    : null;

                return new CflItem(key, label, description);
            })
            .ToList();

        return new CflRequest("Select Asset", items);
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
        StatusMessage = _localization.GetString("Module.Status.Filtered", Title, search);
        return Task.CompletedTask;
    }

    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedMachine = null;
            SetEditor(AssetEditor.CreateEmpty());
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

        var machine = await _machineService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (machine is null)
        {
            StatusMessage = $"Unable to locate asset #{id}.";
            return;
        }

        _loadedMachine = machine;
        LoadEditor(machine);
        UpdateAttachmentCommandState();
    }

    protected override Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        switch (mode)
        {
            case FormMode.Add:
                _snapshot = null;
                _loadedMachine = null;
                SetEditor(AssetEditor.CreateForNew(_machineService.NormalizeStatus("active")));
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
            var machine = Editor.ToMachine(_loadedMachine);
            machine.Status = _machineService.NormalizeStatus(machine.Status);
            _machineService.Validate(machine);
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
        var machine = Editor.ToMachine(_loadedMachine);
        machine.Status = _machineService.NormalizeStatus(machine.Status);

        if (Mode == FormMode.Update && _loadedMachine is null)
        {
            StatusMessage = "Select an asset before saving.";
            return false;
        }

        var recordId = Mode == FormMode.Update ? _loadedMachine!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("machines", recordId))
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

        var signature = signatureResult.Signature;
        var signerDisplayName = _authContext.CurrentUser?.FullName;
        if (string.IsNullOrWhiteSpace(signerDisplayName))
        {
            signerDisplayName = _authContext.CurrentUser?.Username ?? string.Empty;
        }

        machine.DigitalSignature = signature.SignatureHash ?? string.Empty;
        machine.LastModified = signature.SignedAt ?? DateTime.UtcNow;
        machine.LastModifiedById = signature.UserId != 0
            ? signature.UserId
            : _authContext.CurrentUser?.Id ?? machine.LastModifiedById;

        var context = MachineCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        Editor.SignatureHash = machine.DigitalSignature;
        Editor.SignatureReason = signatureResult.ReasonDisplay;
        Editor.SignatureNote = signature.Note ?? string.Empty;
        Editor.SignatureTimestampUtc = signature.SignedAt;
        Editor.SignerUserId = signature.UserId == 0 ? _authContext.CurrentUser?.Id : signature.UserId;
        Editor.SignerUserName = string.IsNullOrWhiteSpace(signature.UserName)
            ? signerDisplayName ?? string.Empty
            : signature.UserName;
        Editor.LastModifiedUtc = machine.LastModified;
        Editor.LastModifiedById = machine.LastModifiedById;
        Editor.LastModifiedByName = Editor.SignerUserName;
        Editor.SourceIp = signature.IpAddress ?? _authContext.CurrentIpAddress ?? string.Empty;
        Editor.SessionId = signature.SessionId ?? _authContext.CurrentSessionId ?? string.Empty;
        Editor.DeviceInfo = signature.DeviceInfo ?? _authContext.CurrentDeviceInfo ?? string.Empty;

        Machine adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _machineService.CreateAsync(machine, context).ConfigureAwait(false);
                machine.Id = saveResult.Id;
                adapterResult = machine;
            }
            else if (Mode == FormMode.Update)
            {
                machine.Id = _loadedMachine!.Id;
                saveResult = await _machineService.UpdateAsync(machine, context).ConfigureAwait(false);
                adapterResult = machine;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist asset: {ex.Message}", ex);
        }

        if (saveResult.SignatureMetadata?.Id is { } signatureId)
        {
            adapterResult.DigitalSignatureId = signatureId;
        }

        _loadedMachine = machine;
        LoadEditor(machine);
        UpdateAttachmentCommandState();

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "machines",
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
            if (_loadedMachine is not null)
            {
                LoadEditor(_loadedMachine);
            }
            else
            {
                SetEditor(AssetEditor.CreateEmpty());
            }
        }
        else if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
        }

        UpdateAttachmentCommandState();
    }

    partial void OnEditorChanging(AssetEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(AssetEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged += OnEditorPropertyChanged;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is nameof(IsBusy) or nameof(Mode) or nameof(SelectedRecord) or nameof(IsDirty))
        {
            UpdateAttachmentCommandState();
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
    }

    private void LoadEditor(Machine machine)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = AssetEditor.FromMachine(machine, _machineService.NormalizeStatus);
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateAttachmentCommandState();
    }

    private void SetEditor(AssetEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateAttachmentCommandState();
    }

    private bool CanAttachDocument()
        => !IsBusy
           && !IsEditorEnabled
           && _loadedMachine is not null
           && _loadedMachine.Id > 0;

    private async Task AttachDocumentAsync()
    {
        if (_loadedMachine is null || _loadedMachine.Id <= 0)
        {
            StatusMessage = "Save the asset before adding attachments.";
            return;
        }

        try
        {
            IsBusy = true;
            var files = await _filePicker.PickFilesAsync(
                    new FilePickerRequest(AllowMultiple: true, Title: $"Attach files to {_loadedMachine.Name}"))
                .ConfigureAwait(false);

            if (files is null || files.Count == 0)
            {
                StatusMessage = "Attachment upload cancelled.";
                return;
            }

            var uploadedBy = _authContext.CurrentUser?.Id;
            var processed = 0;
            var deduplicated = 0;

            foreach (var file in files)
            {
                await using var stream = await file.OpenReadAsync().ConfigureAwait(false);
                var request = new AttachmentUploadRequest
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    EntityType = "machines",
                    EntityId = _loadedMachine.Id,
                    UploadedById = uploadedBy,
                    Reason = $"asset:{_loadedMachine.Id}",
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

    private static ModuleRecord ToRecord(Machine machine)
    {
        var fields = new List<InspectorField>
        {
            new("Location", machine.Location ?? "-"),
            new("Model", machine.Model ?? "-"),
            new("Manufacturer", machine.Manufacturer ?? "-"),
            new("Status", machine.Status ?? "-"),
            new("Installed", machine.InstallDate?.ToString("d", CultureInfo.CurrentCulture) ?? "-")
        };

        return new ModuleRecord(
            machine.Id.ToString(CultureInfo.InvariantCulture),
            machine.Name,
            machine.Code,
            machine.Status,
            machine.Description,
            fields,
            WorkOrdersModuleViewModel.ModuleKey,
            machine.Id);
    }
    /// <summary>
    /// Represents the asset editor value.
    /// </summary>

    public sealed partial class AssetEditor : SignatureAwareEditor
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
        private string _model = string.Empty;

        [ObservableProperty]
        private string _manufacturer = string.Empty;

        [ObservableProperty]
        private string _location = string.Empty;

        [ObservableProperty]
        private string _status = "active";

        [ObservableProperty]
        private string _ursDoc = string.Empty;

        [ObservableProperty]
        private DateTime? _installDate = DateTime.UtcNow.Date;

        [ObservableProperty]
        private DateTime? _procurementDate;

        [ObservableProperty]
        private DateTime? _warrantyUntil;

        [ObservableProperty]
        private bool _isCritical;

        [ObservableProperty]
        private string _serialNumber = string.Empty;

        [ObservableProperty]
        private string _lifecyclePhase = string.Empty;

        [ObservableProperty]
        private string _notes = string.Empty;
        /// <summary>
        /// Executes the create empty operation.
        /// </summary>

        public static AssetEditor CreateEmpty() => new()
        {
            LastModifiedUtc = null,
            LastModifiedById = null,
            LastModifiedByName = string.Empty,
            SignatureHash = string.Empty,
            SignatureReason = string.Empty,
            SignatureNote = string.Empty,
            SignatureTimestampUtc = null,
            SignerUserId = null,
            SignerUserName = string.Empty,
            SourceIp = string.Empty,
            SessionId = string.Empty,
            DeviceInfo = string.Empty
        };
        /// <summary>
        /// Executes the create for new operation.
        /// </summary>

        public static AssetEditor CreateForNew(string normalizedStatus)
            => new()
            {
                Status = normalizedStatus,
                LastModifiedUtc = DateTime.UtcNow,
                LastModifiedById = null,
                LastModifiedByName = string.Empty,
                SignatureHash = string.Empty,
                SignatureReason = string.Empty,
                SignatureNote = string.Empty,
                SignatureTimestampUtc = null,
                SignerUserId = null,
                SignerUserName = string.Empty,
                SourceIp = string.Empty,
                SessionId = string.Empty,
                DeviceInfo = string.Empty
            };
        /// <summary>
        /// Executes the from machine operation.
        /// </summary>

        public static AssetEditor FromMachine(Machine machine, Func<string?, string> normalizer)
        {
            return new AssetEditor
            {
                Id = machine.Id,
                Code = machine.Code ?? string.Empty,
                Name = machine.Name ?? string.Empty,
                Description = machine.Description ?? string.Empty,
                Model = machine.Model ?? string.Empty,
                Manufacturer = machine.Manufacturer ?? string.Empty,
                Location = machine.Location ?? string.Empty,
                Status = normalizer(machine.Status),
                UrsDoc = machine.UrsDoc ?? string.Empty,
                InstallDate = machine.InstallDate,
                ProcurementDate = machine.ProcurementDate,
                WarrantyUntil = machine.WarrantyUntil,
                IsCritical = machine.IsCritical,
                SerialNumber = machine.SerialNumber ?? string.Empty,
                LifecyclePhase = machine.LifecyclePhase ?? string.Empty,
                Notes = machine.Note ?? string.Empty,
                SignatureHash = machine.DigitalSignature ?? string.Empty,
                LastModifiedUtc = machine.LastModified,
                LastModifiedById = machine.LastModifiedById,
                LastModifiedByName = machine.LastModifiedBy?.FullName ?? string.Empty,
                SignatureTimestampUtc = machine.LastModified,
                SignerUserId = machine.LastModifiedById,
                SignerUserName = machine.LastModifiedBy?.FullName ?? string.Empty
            };
        }
        /// <summary>
        /// Executes the to machine operation.
        /// </summary>

        public Machine ToMachine(Machine? existing)
        {
            var machine = existing is null ? new Machine() : CloneMachine(existing);
            machine.Id = Id;
            machine.Code = Code;
            machine.Name = Name;
            machine.Description = Description;
            machine.Model = Model;
            machine.Manufacturer = Manufacturer;
            machine.Location = Location;
            machine.Status = Status;
            machine.UrsDoc = UrsDoc;
            machine.InstallDate = InstallDate;
            machine.ProcurementDate = ProcurementDate;
            machine.WarrantyUntil = WarrantyUntil;
            machine.IsCritical = IsCritical;
            machine.SerialNumber = string.IsNullOrWhiteSpace(SerialNumber) ? machine.SerialNumber : SerialNumber;
            machine.LifecyclePhase = LifecyclePhase;
            machine.Note = Notes;
            machine.DigitalSignature = SignatureHash;
            machine.LastModified = LastModifiedUtc ?? DateTime.UtcNow;
            machine.LastModifiedById = LastModifiedById ?? machine.LastModifiedById;
            return machine;
        }
        /// <summary>
        /// Executes the clone operation.
        /// </summary>

        public AssetEditor Clone()
            => new()
            {
                Id = Id,
                Code = Code,
                Name = Name,
                Description = Description,
                Model = Model,
                Manufacturer = Manufacturer,
                Location = Location,
                Status = Status,
                UrsDoc = UrsDoc,
                InstallDate = InstallDate,
                ProcurementDate = ProcurementDate,
                WarrantyUntil = WarrantyUntil,
                IsCritical = IsCritical,
                SerialNumber = SerialNumber,
                LifecyclePhase = LifecyclePhase,
                Notes = Notes,
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

        private static Machine CloneMachine(Machine source)
        {
            return new Machine
            {
                Id = source.Id,
                Code = source.Code,
                Name = source.Name,
                Description = source.Description,
                Model = source.Model,
                Manufacturer = source.Manufacturer,
                Location = source.Location,
                Status = source.Status,
                UrsDoc = source.UrsDoc,
                InstallDate = source.InstallDate,
                ProcurementDate = source.ProcurementDate,
                WarrantyUntil = source.WarrantyUntil,
                IsCritical = source.IsCritical,
                SerialNumber = source.SerialNumber,
                LifecyclePhase = source.LifecyclePhase,
                Note = source.Note
            };
        }
    }
}
