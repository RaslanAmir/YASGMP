using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using YasGMP.Wpf.ViewModels;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.ViewModels.Modules;
/// <summary>
/// Represents the assets module view model value.
/// </summary>

public sealed partial class AssetsModuleViewModel : DataDrivenModuleDocumentViewModel, IDisposable
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
    private readonly ICodeGeneratorService _codeGeneratorService;
    private readonly IQRCodeService _qrCodeService;
    private readonly IPlatformService _platformService;
    private readonly IShellInteractionService _shellInteraction;
    private readonly AssetViewModel _assetViewModel;
    private INotifyCollectionChanged? _filteredAssetsSubscription;
    private bool _suppressFilteredAssetsCollectionNotifications;
    private bool _isSynchronizingRecords;
    private bool _isSynchronizingSelection;
    private Machine? _loadedMachine;
    private string? _pendingNavigationStatusMessage;
    private string? _pendingNavigationSelectionKey;

    private readonly record struct AssetNavigationContext(int? Id, string? Code, string SearchText)
    {
        public bool HasFilter => Id.HasValue || !string.IsNullOrWhiteSpace(Code);
    }
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
        AssetViewModel assetViewModel,
        IModuleNavigationService navigation,
        ILocalizationService localization,
        ICodeGeneratorService codeGeneratorService,
        IQRCodeService qrCodeService,
        IPlatformService platformService)
        : base(ModuleKey, localization.GetString("Module.Title.Assets"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _machineService = machineService ?? throw new ArgumentNullException(nameof(machineService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _codeGeneratorService = codeGeneratorService ?? throw new ArgumentNullException(nameof(codeGeneratorService));
        _qrCodeService = qrCodeService ?? throw new ArgumentNullException(nameof(qrCodeService));
        _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
        _shellInteraction = shellInteraction ?? throw new ArgumentNullException(nameof(shellInteraction));
        _assetViewModel = assetViewModel ?? throw new ArgumentNullException(nameof(assetViewModel));
        _assetViewModel.PropertyChanged += OnAssetViewModelPropertyChanged;
        _assetViewModel.EditorChanged += OnAssetEditorChanged;
        ObserveFilteredAssets(_assetViewModel.FilteredAssets, skipImmediateSync: true);
        ResetAsset();
        UpdateCommandStates();
        StatusOptions = new ReadOnlyCollection<string>(new[]
        {
            _localization.GetString("Module.Assets.Status.Active"),
            _localization.GetString("Module.Assets.Status.Maintenance"),
            _localization.GetString("Module.Assets.Status.Reserved"),
            _localization.GetString("Module.Assets.Status.Decommissioned"),
            _localization.GetString("Module.Assets.Status.Scrapped")
        });

        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
        GenerateCodeCommand = new AsyncRelayCommand(GenerateCodeAsync, CanGenerateCode);
        PreviewQrCommand = new AsyncRelayCommand(PreviewQrAsync, CanPreviewQr);
    }

    /// <summary>Shared asset editor payload bound to the form fields.</summary>
    public AssetViewModel Asset => _assetViewModel;

    /// <summary>Mirrors the shared asset list surfaced by the shell.</summary>
    public ObservableCollection<Asset> FilteredAssets => _assetViewModel.FilteredAssets;

    /// <summary>Currently selected asset from the shared collection.</summary>
    public Asset? SelectedAsset
    {
        get => _assetViewModel.SelectedAsset;
        set => UpdateSelectedAsset(value);
    }

    public void Dispose()
    {
        _assetViewModel.PropertyChanged -= OnAssetViewModelPropertyChanged;
        _assetViewModel.EditorChanged -= OnAssetEditorChanged;
        if (_filteredAssetsSubscription is not null)
        {
            _filteredAssetsSubscription.CollectionChanged -= OnFilteredAssetsCollectionChanged;
            _filteredAssetsSubscription = null;
        }
    }

    /// <summary>Search term forwarded from the shell search box.</summary>
    public string? AssetSearchTerm
    {
        get => _assetViewModel.SearchTerm;
        set
        {
            if (string.Equals(_assetViewModel.SearchTerm, value, StringComparison.Ordinal))
            {
                return;
            }

            _assetViewModel.SearchTerm = value;
            if (!string.Equals(SearchText, value, StringComparison.Ordinal))
            {
                SearchText = value;
            }

            OnPropertyChanged(nameof(AssetSearchTerm));
        }
    }

    /// <summary>Status filter forwarded from the shell filter controls.</summary>
    public string? AssetStatusFilter
    {
        get => _assetViewModel.StatusFilter;
        set
        {
            if (string.Equals(_assetViewModel.StatusFilter, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _assetViewModel.StatusFilter = value;
            OnPropertyChanged(nameof(AssetStatusFilter));
        }
    }

    /// <summary>Risk filter forwarded from the shell filter controls.</summary>
    public string? AssetRiskFilter
    {
        get => _assetViewModel.RiskFilter;
        set
        {
            if (string.Equals(_assetViewModel.RiskFilter, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _assetViewModel.RiskFilter = value;
            OnPropertyChanged(nameof(AssetRiskFilter));
        }
    }

    /// <summary>Type filter forwarded from the shell filter controls.</summary>
    public string? AssetTypeFilter
    {
        get => _assetViewModel.TypeFilter;
        set
        {
            if (string.Equals(_assetViewModel.TypeFilter, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _assetViewModel.TypeFilter = value;
            OnPropertyChanged(nameof(AssetTypeFilter));
        }
    }

    /// <summary>Indicates whether form controls are writable (Add/Update modes).</summary>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Canonical status options rendered in the combo-box.</summary>
    public IReadOnlyList<string> StatusOptions { get; }

    /// <summary>Command exposed to the toolbar for uploading attachments.</summary>
    public IAsyncRelayCommand AttachDocumentCommand { get; }

    /// <summary>Generates a machine code/QR payload using the shared code generator service.</summary>
    public IAsyncRelayCommand GenerateCodeCommand { get; }

    /// <summary>Persists the QR payload to disk and surfaces the generated PNG path for preview.</summary>
    public IAsyncRelayCommand PreviewQrCommand { get; }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        await _assetViewModel.LoadAssetsAsync().ConfigureAwait(false);
        ObserveFilteredAssets(_assetViewModel.FilteredAssets, skipImmediateSync: !IsInitialized);
        EnsureSelectedAssetFromCollection();
        return ToReadOnlyList(_assetViewModel.FilteredAssets.Select(ToRecord));
    }

    private bool ApplyNavigationSelection(
        Machine target,
        IReadOnlyList<ModuleRecord>? records = null,
        string? searchOverride = null,
        bool queueReselect = false)
    {
        var source = records ?? Records;
        if (source is null || source.Count == 0)
        {
            return false;
        }

        var asset = FindAssetForMachine(target);
        if (asset is null)
        {
            return false;
        }

        var key = asset.Id.ToString(CultureInfo.InvariantCulture);
        var match = source.FirstOrDefault(record => string.Equals(record.Key, key, StringComparison.Ordinal));
        if (match is null)
        {
            return false;
        }

        UpdateSelectedAsset(asset);
        _loadedMachine = BuildLoadedMachineFromAsset(asset, target);

        var search = ResolveNavigationSearchText(match, searchOverride);
        ApplyNavigationHighlight(match, search, queueReselect: queueReselect);
        return true;
    }

    private void ApplyNavigationHighlight(
        ModuleRecord? record,
        string search,
        bool clearSelection = false,
        bool queueReselect = false)
    {
        if (record is not null)
        {
            if (ReferenceEquals(SelectedRecord, record))
            {
                SelectedRecord = null;
            }

            SelectedRecord = record;
            UpdateSelectedAssetFromRecord(record);
        }
        else if (clearSelection)
        {
            SelectedRecord = null;
            UpdateSelectedAsset(null);
        }

        if (string.IsNullOrWhiteSpace(search))
        {
            search = string.Empty;
        }

        AssetSearchTerm = search;

        var status = _localization.GetString("Module.Status.Filtered", Title, search);
        StatusMessage = status;

        _pendingNavigationSelectionKey = queueReselect ? record?.Key : null;
        _pendingNavigationStatusMessage = queueReselect ? status : null;
    }

    private static string ResolveNavigationSearchText(ModuleRecord record, string? searchOverride)
    {
        if (!string.IsNullOrWhiteSpace(searchOverride))
        {
            return searchOverride!;
        }

        if (!string.IsNullOrWhiteSpace(record.Title))
        {
            return record.Title;
        }

        if (!string.IsNullOrWhiteSpace(record.Code))
        {
            return record.Code!;
        }

        return record.Key ?? string.Empty;
    }

    private void EnsurePendingNavigationSelection()
    {
        if (string.IsNullOrWhiteSpace(_pendingNavigationSelectionKey))
        {
            _pendingNavigationSelectionKey = null;
            return;
        }

        var match = Records.FirstOrDefault(
            record => string.Equals(record.Key, _pendingNavigationSelectionKey, StringComparison.Ordinal));
        _pendingNavigationSelectionKey = null;
        if (match is null)
        {
            return;
        }

        if (ReferenceEquals(SelectedRecord, match))
        {
            SelectedRecord = null;
        }

        SelectedRecord = match;
        UpdateSelectedAssetFromRecord(match);
    }

    protected override string FormatLoadedStatus(int count)
    {
        EnsurePendingNavigationSelection();

        if (!string.IsNullOrWhiteSpace(_pendingNavigationStatusMessage))
        {
            var message = _pendingNavigationStatusMessage!;
            _pendingNavigationStatusMessage = null;
            return message;
        }

        return base.FormatLoadedStatus(count);
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

    private static AssetNavigationContext? TryCreateNavigationContext(object? parameter, string? searchOverride = null)
    {
        static string? ResolveSearch(string? candidate, string? overrideValue)
        {
            if (!string.IsNullOrWhiteSpace(overrideValue))
            {
                var trimmed = overrideValue.Trim();
                if (trimmed.Length > 0)
                {
                    return trimmed;
                }
            }

            if (!string.IsNullOrWhiteSpace(candidate))
            {
                var trimmed = candidate.Trim();
                if (trimmed.Length > 0)
                {
                    return trimmed;
                }
            }

            return null;
        }

        switch (parameter)
        {
            case null:
                {
                    var search = ResolveSearch(null, searchOverride);
                    return search is null ? null : new AssetNavigationContext(null, null, search);
                }
            case int id:
                {
                    var search = ResolveSearch(id.ToString(CultureInfo.InvariantCulture), searchOverride);
                    return search is null ? null : new AssetNavigationContext(id, null, search);
                }
            case string text:
                {
                    var trimmed = text.Trim();
                    if (trimmed.Length == 0)
                    {
                        var search = ResolveSearch(null, searchOverride);
                        return search is null ? null : new AssetNavigationContext(null, null, search);
                    }

                    if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericId))
                    {
                        var search = ResolveSearch(trimmed, searchOverride);
                        return search is null ? null : new AssetNavigationContext(numericId, null, search);
                    }

                    var codeSearch = ResolveSearch(trimmed, searchOverride);
                    return codeSearch is null ? null : new AssetNavigationContext(null, trimmed, codeSearch);
                }
            case Machine machine:
                {
                    if (machine.Id > 0)
                    {
                        var fallback = !string.IsNullOrWhiteSpace(machine.Code)
                            ? machine.Code
                            : machine.Id.ToString(CultureInfo.InvariantCulture);
                        var search = ResolveSearch(fallback, searchOverride);
                        return search is null ? null : new AssetNavigationContext(machine.Id, null, search);
                    }

                    if (!string.IsNullOrWhiteSpace(machine.Code))
                    {
                        var code = machine.Code!;
                        var search = ResolveSearch(code, searchOverride);
                        return search is null ? null : new AssetNavigationContext(null, code.Trim(), search);
                    }

                    if (!string.IsNullOrWhiteSpace(machine.Name))
                    {
                        var name = machine.Name!;
                        var search = ResolveSearch(name, searchOverride);
                        return search is null ? null : new AssetNavigationContext(null, name.Trim(), search);
                    }

                    break;
                }
            case ModuleRecord record:
                {
                    var nextParameter = record.RelatedParameter
                        ?? (!string.IsNullOrWhiteSpace(record.Code) ? record.Code : record.Key);
                    if (nextParameter is null)
                    {
                        break;
                    }

                    return TryCreateNavigationContext(nextParameter, searchOverride ?? record.Code ?? record.Key);
                }
        }

        if (TryNormalizeNavigationParameter(parameter, out var normalized, out var normalizedSearch))
        {
            return TryCreateNavigationContext(normalized, normalizedSearch ?? searchOverride);
        }

        if (!string.IsNullOrWhiteSpace(searchOverride))
        {
            var search = searchOverride.Trim();
            if (search.Length > 0)
            {
                return new AssetNavigationContext(null, null, search);
            }
        }

        return null;
    }

    private static List<Machine> FilterMachinesByNavigation(IReadOnlyList<Machine> machines, AssetNavigationContext navigation)
    {
        if (machines.Count == 0)
        {
            return new List<Machine>();
        }

        if (navigation.Id is int id)
        {
            return machines.Where(machine => machine.Id == id).ToList();
        }

        if (!string.IsNullOrWhiteSpace(navigation.Code))
        {
            var code = navigation.Code!;
            var equalityMatches = machines
                .Where(machine =>
                    (!string.IsNullOrWhiteSpace(machine.Code)
                        && string.Equals(machine.Code, code, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(machine.Name)
                        && string.Equals(machine.Name, code, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (equalityMatches.Count > 0)
            {
                return equalityMatches;
            }

            return machines
                .Where(machine =>
                    (!string.IsNullOrWhiteSpace(machine.Code)
                        && machine.Code.Contains(code, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(machine.Name)
                        && machine.Name.Contains(code, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return new List<Machine>();
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

        var navigation = TryCreateNavigationContext(parameter);
        var (target, _, filterActive, searchTerm) = await ResolveNavigationPayloadAsync(parameter).ConfigureAwait(false);
        var effectiveSearch = !string.IsNullOrWhiteSpace(navigation?.SearchText)
            ? navigation!.SearchText
            : searchTerm;
        var shouldFilter = filterActive || (navigation?.HasFilter ?? false);
        if (target is null)
        {
            if (shouldFilter && !string.IsNullOrWhiteSpace(effectiveSearch))
            {
                await RefreshAsync(parameter).ConfigureAwait(false);
                AssetSearchTerm = effectiveSearch;
                StatusMessage = _localization.GetString("Module.Status.Filtered", Title, effectiveSearch);
            }

            return;
        }

        if (IsInEditMode)
        {
            await EnterViewModeCommand.ExecuteAsync(null).ConfigureAwait(false);
        }

        if (ApplyNavigationSelection(target, searchOverride: effectiveSearch))
        {
            return;
        }

        await RefreshAsync(parameter).ConfigureAwait(false);

        if (IsInEditMode)
        {
            await EnterViewModeCommand.ExecuteAsync(null).ConfigureAwait(false);
        }

        if (!ApplyNavigationSelection(target, searchOverride: effectiveSearch)
            && !string.IsNullOrWhiteSpace(effectiveSearch))
        {
            AssetSearchTerm = effectiveSearch;
            StatusMessage = _localization.GetString("Module.Status.Filtered", Title, effectiveSearch);
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
            UpdateSelectedAssetFromRecord(match);
        }

        AssetSearchTerm = search;
        StatusMessage = _localization.GetString("Module.Status.Filtered", Title, search);
        return Task.CompletedTask;
    }

    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedMachine = null;
            ResetAsset();
            _assetViewModel.ClearSelection();
            UpdateCommandStates();
            return;
        }

        var asset = await ResolveAssetFromRecordAsync(record).ConfigureAwait(false);
        if (asset is null)
        {
            _loadedMachine = null;
            ResetAsset();
            _assetViewModel.ClearSelection();

            if (int.TryParse(record.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var missingId))
            {
                StatusMessage = _localization.GetString("Module.Assets.Status.AssetNotFound", missingId);
            }
            else
            {
                var descriptor = record.Title ?? record.Key ?? string.Empty;
                StatusMessage = _localization.GetString("Module.Assets.Status.AssetNotFound", descriptor);
            }

            UpdateCommandStates();
            return;
        }

        if (IsInEditMode)
        {
            return;
        }

        var fallback = _loadedMachine is not null && _loadedMachine.Id == asset.Id ? _loadedMachine : null;
        var machine = BuildLoadedMachineFromAsset(asset, fallback);
        _loadedMachine = machine;
        LoadAsset(machine);
        await InitializeEditorIdentifiersAsync(resetDirty: true).ConfigureAwait(false);
        UpdateCommandStates();
    }

    protected override async Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        switch (mode)
        {
            case FormMode.Add:
                _loadedMachine = null;
                UpdateAssetWithoutDirty(() =>
                {
                    var normalized = _machineService.NormalizeStatus("active");
                    _assetViewModel.PrepareForNew(normalized);
                });
                await InitializeEditorIdentifiersAsync(resetDirty: true).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(_assetViewModel.Code) && !string.IsNullOrWhiteSpace(_assetViewModel.QrCode))
                {
                    StatusMessage = _localization.GetString(
                        "Module.Assets.Status.CodeAndQrGenerated",
                        _assetViewModel.Code,
                        _assetViewModel.QrCode);
                }
                break;
            case FormMode.Update:
                if (_loadedMachine is not null)
                {
                    LoadAsset(_loadedMachine);
                }
                await InitializeEditorIdentifiersAsync().ConfigureAwait(false);
                break;
        }

        UpdateCommandStates();
    }

    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        try
        {
            var code = EnsureEditorCode(force: false, suppressDirty: true);
            EnsureEditorQrPayload(code, suppressDirty: true);
            var machine = _assetViewModel.ToMachine(_loadedMachine);
            machine.Status = _machineService.NormalizeStatus(machine.Status);
            return _assetViewModel.ValidateMachine(machine);
        }
        catch (InvalidOperationException ex)
        {
            return new[] { ex.Message };
        }
        catch (Exception ex)
        {
            return new[] { $"Unexpected validation failure: {ex.Message}" };
        }
    }

    protected override async Task<bool> OnSaveAsync()
    {
        var machine = _assetViewModel.ToMachine(_loadedMachine);
        var asset = EnsureSelectedAsset();
        try
        {
            await SynchronizeIdentifiersAsync(machine, forceCode: false, suppressDirty: true).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.GetString("Module.Assets.Status.QrGenerationFailed", ex.Message);
            return false;
        }
        machine.Status = _machineService.NormalizeStatus(machine.Status);

        if (Mode == FormMode.Update && asset.Id <= 0 && _loadedMachine is null)
        {
            StatusMessage = _localization.GetString("Module.Assets.Status.SelectBeforeSave");
            return false;
        }

        var assetId = asset.Id > 0 ? asset.Id : _loadedMachine?.Id ?? 0;
        var recordId = Mode == FormMode.Update ? assetId : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("machines", recordId))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.GetString("Module.Assets.Status.SignatureFailed", ex.Message);
            return false;
        }

        if (signatureResult is null)
        {
            StatusMessage = _localization.GetString("Module.Assets.Status.SignatureCancelled");
            return false;
        }

        if (signatureResult.Signature is null)
        {
            StatusMessage = _localization.GetString("Module.Assets.Status.SignatureNotCaptured");
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

        _assetViewModel.SignatureHash = machine.DigitalSignature;
        _assetViewModel.SignatureReason = signatureResult.ReasonDisplay;
        _assetViewModel.SignatureNote = signature.Note ?? string.Empty;
        _assetViewModel.SignatureTimestampUtc = signature.SignedAt;
        _assetViewModel.SignerUserId = signature.UserId == 0 ? _authContext.CurrentUser?.Id : signature.UserId;
        _assetViewModel.SignerUserName = string.IsNullOrWhiteSpace(signature.UserName)
            ? signerDisplayName ?? string.Empty
            : signature.UserName;
        _assetViewModel.LastModifiedUtc = machine.LastModified;
        _assetViewModel.LastModifiedById = machine.LastModifiedById;
        _assetViewModel.LastModifiedByName = _assetViewModel.SignerUserName;
        _assetViewModel.SourceIp = signature.IpAddress ?? _authContext.CurrentIpAddress ?? string.Empty;
        _assetViewModel.SessionId = signature.SessionId ?? _authContext.CurrentSessionId ?? string.Empty;
        _assetViewModel.DeviceInfo = signature.DeviceInfo ?? _authContext.CurrentDeviceInfo ?? string.Empty;

        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _assetViewModel.AddAsync(machine, context).ConfigureAwait(false);
                machine.Id = saveResult.Id;
            }
            else if (Mode == FormMode.Update)
            {
                machine.Id = assetId;
                saveResult = await _assetViewModel.UpdateAsync(machine, context).ConfigureAwait(false);
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
            machine.DigitalSignatureId = signatureId;
        }

        _loadedMachine = machine;
        LoadAsset(machine);
        UpdateCommandStates();

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "machines",
            recordId: machine.Id,
            metadata: saveResult.SignatureMetadata,
            fallbackSignatureHash: machine.DigitalSignature,
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
            StatusMessage = _localization.GetString("Module.Assets.Status.SignaturePersistenceFailed", ex.Message);
            Mode = FormMode.Update;
            return false;
        }

        _assetViewModel.SyncSelectedAssetFromEditor();
        await _assetViewModel.LoadAssetsAsync().ConfigureAwait(false);
        await _assetViewModel.EnsureAssetAsync(machine.Id, machine.Code).ConfigureAwait(false);
        _assetViewModel.StatusMessage = _localization.GetString("Module.Assets.Status.SignatureCaptured", signatureResult.ReasonDisplay);
        StatusMessage = _assetViewModel.StatusMessage;
        return true;
    }

    protected override void OnCancel()
    {
        if (Mode == FormMode.Add)
        {
            if (_loadedMachine is not null)
            {
                LoadAsset(_loadedMachine);
            }
            else
            {
                ResetAsset();
            }
        }
        else if (Mode == FormMode.Update && _loadedMachine is not null)
        {
            LoadAsset(_loadedMachine);
        }

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

    private void OnAssetViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AssetViewModel.FilteredAssets):
                OnPropertyChanged(nameof(FilteredAssets));
                ObserveFilteredAssets(_assetViewModel.FilteredAssets);
                break;
            case nameof(AssetViewModel.SelectedAsset):
                OnPropertyChanged(nameof(SelectedAsset));
                if (!_isSynchronizingSelection)
                {
                    SyncSelectedRecordWithAsset();
                }
                break;
            case nameof(AssetViewModel.SearchTerm):
                OnPropertyChanged(nameof(AssetSearchTerm));
                if (!string.Equals(SearchText, _assetViewModel.SearchTerm, StringComparison.Ordinal))
                {
                    SearchText = _assetViewModel.SearchTerm;
                }
                break;
            case nameof(AssetViewModel.StatusFilter):
                OnPropertyChanged(nameof(AssetStatusFilter));
                break;
            case nameof(AssetViewModel.RiskFilter):
                OnPropertyChanged(nameof(AssetRiskFilter));
                break;
            case nameof(AssetViewModel.TypeFilter):
                OnPropertyChanged(nameof(AssetTypeFilter));
                break;
        }

        UpdateCommandStates();
    }

    private void OnAssetEditorChanged(object? sender, EventArgs e)
    {
        if (!IsInEditMode)
        {
            return;
        }

        MarkDirty();
        UpdateCommandStates();
    }

    private void ResetAsset()
    {
        UpdateAssetWithoutDirty(() =>
        {
            _assetViewModel.Reset();
            _assetViewModel.ClearSelection();
        });
        ResetDirty();
    }

    private void LoadAsset(Machine machine)
    {
        UpdateAssetWithoutDirty(() => _assetViewModel.PrepareForExisting(machine, _machineService.NormalizeStatus));
        ResetDirty();
    }

    private void UpdateAssetWithoutDirty(Action updateAction)
        => ExecuteWithSuppressedAssetNotifications<object?>(() =>
        {
            updateAction();
            return null;
        });

    private T UpdateAssetWithoutDirty<T>(Func<T> updateAction)
        => ExecuteWithSuppressedAssetNotifications(updateAction);

    private T ExecuteWithSuppressedAssetNotifications<T>(Func<T> updateAction)
    {
        if (updateAction is null)
        {
            throw new ArgumentNullException(nameof(updateAction));
        }

        _assetViewModel.PropertyChanged -= OnAssetViewModelPropertyChanged;
        _assetViewModel.EditorChanged -= OnAssetEditorChanged;
        if (_filteredAssetsSubscription is not null)
        {
            _filteredAssetsSubscription.CollectionChanged -= OnFilteredAssetsCollectionChanged;
        }

        _suppressFilteredAssetsCollectionNotifications = true;
        try
        {
            return updateAction();
        }
        finally
        {
            ObserveFilteredAssets(_assetViewModel.FilteredAssets, skipImmediateSync: true);
            _assetViewModel.PropertyChanged += OnAssetViewModelPropertyChanged;
            _assetViewModel.EditorChanged += OnAssetEditorChanged;
            _suppressFilteredAssetsCollectionNotifications = false;
        }
    }

    private Asset EnsureSelectedAsset()
    {
        if (_assetViewModel.SelectedAsset is { } asset)
        {
            return asset;
        }

        var placeholder = new Asset
        {
            Id = _assetViewModel.Id,
            AssetCode = string.IsNullOrWhiteSpace(_assetViewModel.Code) ? string.Empty : _assetViewModel.Code,
            AssetName = string.IsNullOrWhiteSpace(_assetViewModel.Name) ? string.Empty : _assetViewModel.Name,
            Description = string.IsNullOrWhiteSpace(_assetViewModel.Description) ? null : _assetViewModel.Description,
            Model = string.IsNullOrWhiteSpace(_assetViewModel.Model) ? null : _assetViewModel.Model,
            Manufacturer = string.IsNullOrWhiteSpace(_assetViewModel.Manufacturer) ? null : _assetViewModel.Manufacturer,
            Location = string.IsNullOrWhiteSpace(_assetViewModel.Location) ? null : _assetViewModel.Location,
            Status = string.IsNullOrWhiteSpace(_assetViewModel.Status) ? null : _assetViewModel.Status,
            UrsDoc = string.IsNullOrWhiteSpace(_assetViewModel.UrsDoc) ? null : _assetViewModel.UrsDoc,
            InstallDate = _assetViewModel.InstallDate,
            ProcurementDate = _assetViewModel.ProcurementDate,
            WarrantyUntil = _assetViewModel.WarrantyUntil,
            IsCritical = _assetViewModel.IsCritical,
            SerialNumber = string.IsNullOrWhiteSpace(_assetViewModel.SerialNumber) ? null : _assetViewModel.SerialNumber,
            LifecyclePhase = string.IsNullOrWhiteSpace(_assetViewModel.LifecyclePhase) ? null : _assetViewModel.LifecyclePhase,
            Notes = string.IsNullOrWhiteSpace(_assetViewModel.Notes) ? null : _assetViewModel.Notes,
            QrCode = string.IsNullOrWhiteSpace(_assetViewModel.QrCode) ? null : _assetViewModel.QrCode,
            QrPayload = string.IsNullOrWhiteSpace(_assetViewModel.QrPayload) ? null : _assetViewModel.QrPayload,
            DigitalSignature = string.IsNullOrWhiteSpace(_assetViewModel.SignatureHash) ? null : _assetViewModel.SignatureHash,
            LastModified = _assetViewModel.LastModifiedUtc
        };

        UpdateAssetWithoutDirty(() => _assetViewModel.SelectedAsset = placeholder);
        return placeholder;
    }

    private void ObserveFilteredAssets(ObservableCollection<Asset> assets, bool skipImmediateSync = false)
    {
        if (_filteredAssetsSubscription is not null)
        {
            _filteredAssetsSubscription.CollectionChanged -= OnFilteredAssetsCollectionChanged;
        }

        _filteredAssetsSubscription = assets;
        if (_filteredAssetsSubscription is not null)
        {
            _filteredAssetsSubscription.CollectionChanged += OnFilteredAssetsCollectionChanged;
        }

        if (!skipImmediateSync)
        {
            HandleFilteredAssetsProjectionChanged();
        }
    }

    private void OnFilteredAssetsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_suppressFilteredAssetsCollectionNotifications)
        {
            return;
        }

        HandleFilteredAssetsProjectionChanged();
    }

    private void HandleFilteredAssetsProjectionChanged()
    {
        SyncRecordsWithFilteredAssets();
        EnsurePendingNavigationSelection();

        var searchTerm = _assetViewModel.SearchTerm;
        if (!string.Equals(SearchText, searchTerm, StringComparison.Ordinal))
        {
            SearchText = searchTerm;
        }

        var assetStatus = _assetViewModel.StatusMessage;
        if (!string.IsNullOrWhiteSpace(assetStatus))
        {
            StatusMessage = assetStatus!;
        }
        else
        {
            var trimmedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm!.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedSearch))
            {
                StatusMessage = _localization.GetString("Module.Status.Filtered", Title, trimmedSearch);
            }
            else
            {
                StatusMessage = FormatLoadedStatus(Records.Count);
            }
        }

        UpdateCommandStates();
    }

    private void SyncRecordsWithFilteredAssets()
    {
        if (_isSynchronizingRecords)
        {
            return;
        }

        try
        {
            _isSynchronizingRecords = true;
            var selectedAssetId = _assetViewModel.SelectedAsset?.Id;
            var previousKey = SelectedRecord?.Key;

            Records.Clear();
            foreach (var record in _assetViewModel.FilteredAssets.Select(ToRecord))
            {
                Records.Add(record);
            }

            RecordsView.Refresh();

            ModuleRecord? nextSelection = null;
            if (selectedAssetId.HasValue)
            {
                var idKey = selectedAssetId.Value.ToString(CultureInfo.InvariantCulture);
                nextSelection = Records.FirstOrDefault(record => string.Equals(record.Key, idKey, StringComparison.Ordinal));
            }

            if (nextSelection is null && previousKey is not null)
            {
                nextSelection = Records.FirstOrDefault(record => string.Equals(record.Key, previousKey, StringComparison.Ordinal));
            }

            if (nextSelection is null && Records.Count > 0)
            {
                nextSelection = Records[0];
            }

            if (!ReferenceEquals(SelectedRecord, nextSelection))
            {
                SelectedRecord = nextSelection;
            }
        }
        finally
        {
            _isSynchronizingRecords = false;
        }
    }

    private void EnsureSelectedAssetFromCollection()
    {
        if (_assetViewModel.FilteredAssets.Count == 0)
        {
            UpdateSelectedAsset(null);
            return;
        }

        var current = _assetViewModel.SelectedAsset;
        if (current is not null && _assetViewModel.FilteredAssets.Any(asset => AreSameAsset(asset, current)))
        {
            return;
        }

        UpdateSelectedAsset(_assetViewModel.FilteredAssets[0]);
    }

    private void UpdateSelectedAsset(Asset? asset)
    {
        if (_isSynchronizingSelection)
        {
            _assetViewModel.SelectedAsset = asset;
            if (asset is null)
            {
                _loadedMachine = null;
            }
            return;
        }

        try
        {
            _isSynchronizingSelection = true;
            if (AreSameAsset(_assetViewModel.SelectedAsset, asset))
            {
                return;
            }

            _assetViewModel.SelectedAsset = asset;
            if (asset is null)
            {
                _loadedMachine = null;
            }
        }
        finally
        {
            _isSynchronizingSelection = false;
        }
    }

    private void UpdateSelectedAssetFromRecord(ModuleRecord? record)
    {
        if (record is null)
        {
            UpdateSelectedAsset(null);
            return;
        }

        var asset = FindAssetForRecord(record);
        UpdateSelectedAsset(asset);
    }

    private Asset? FindAssetForMachine(Machine machine)
    {
        if (machine.Id > 0)
        {
            var idMatch = _assetViewModel.FilteredAssets
                .FirstOrDefault(asset => asset.Id == machine.Id);
            if (idMatch is not null)
            {
                return idMatch;
            }
        }

        if (!string.IsNullOrWhiteSpace(machine.Code))
        {
            var code = machine.Code;
            var codeMatch = _assetViewModel.FilteredAssets
                .FirstOrDefault(asset => string.Equals(asset.AssetCode, code, StringComparison.OrdinalIgnoreCase));
            if (codeMatch is not null)
            {
                return codeMatch;
            }
        }

        return null;
    }

    private void SyncSelectedRecordWithAsset()
    {
        if (_isSynchronizingSelection)
        {
            return;
        }

        try
        {
            _isSynchronizingSelection = true;
            ModuleRecord? target = null;
            if (_assetViewModel.SelectedAsset is { } asset)
            {
                var idKey = asset.Id.ToString(CultureInfo.InvariantCulture);
                target = Records.FirstOrDefault(record => string.Equals(record.Key, idKey, StringComparison.Ordinal));
            }

            if (!ReferenceEquals(SelectedRecord, target))
            {
                SelectedRecord = target;
            }
        }
        finally
        {
            _isSynchronizingSelection = false;
        }
    }

    private Machine BuildLoadedMachineFromAsset(Asset asset, Machine? fallback)
    {
        if (asset is null)
        {
            throw new ArgumentNullException(nameof(asset));
        }

        var machine = new Machine
        {
            Id = asset.Id,
            Code = !string.IsNullOrWhiteSpace(asset.AssetCode)
                ? asset.AssetCode
                : fallback?.Code ?? string.Empty,
            Name = !string.IsNullOrWhiteSpace(asset.AssetName)
                ? asset.AssetName
                : fallback?.Name ?? string.Empty,
            Description = asset.Description ?? fallback?.Description,
            Model = asset.Model ?? fallback?.Model,
            Manufacturer = asset.Manufacturer ?? fallback?.Manufacturer,
            Location = asset.Location ?? fallback?.Location,
            Status = asset.Status ?? fallback?.Status,
            UrsDoc = asset.UrsDoc ?? fallback?.UrsDoc,
            InstallDate = asset.InstallDate ?? fallback?.InstallDate,
            ProcurementDate = asset.ProcurementDate ?? fallback?.ProcurementDate,
            WarrantyUntil = asset.WarrantyUntil ?? fallback?.WarrantyUntil,
            IsCritical = fallback?.IsCritical ?? false,
            SerialNumber = fallback?.SerialNumber,
            LifecyclePhase = fallback?.LifecyclePhase,
            Note = asset.Notes ?? fallback?.Note,
            QrCode = fallback?.QrCode,
            QrPayload = fallback?.QrPayload,
            DigitalSignature = asset.DigitalSignature ?? fallback?.DigitalSignature,
            LastModified = asset.LastModified ?? fallback?.LastModified ?? DateTime.UtcNow,
            LastModifiedById = fallback?.LastModifiedById,
            LastModifiedBy = fallback?.LastModifiedBy,
            DigitalSignatureId = fallback?.DigitalSignatureId
        };

        machine.Status = _machineService.NormalizeStatus(machine.Status);

        return machine;
    }

    private Asset? FindAssetForRecord(ModuleRecord record)
    {
        if (int.TryParse(record.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return _assetViewModel.FilteredAssets.FirstOrDefault(asset => asset.Id == id)
                ?? _assetViewModel.Assets.FirstOrDefault(asset => asset.Id == id);
        }

        if (!string.IsNullOrWhiteSpace(record.Code))
        {
            return _assetViewModel.FilteredAssets.FirstOrDefault(asset => string.Equals(asset.AssetCode, record.Code, StringComparison.OrdinalIgnoreCase))
                ?? _assetViewModel.Assets.FirstOrDefault(asset => string.Equals(asset.AssetCode, record.Code, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    private async Task<Asset?> ResolveAssetFromRecordAsync(ModuleRecord record)
    {
        int? id = null;
        if (int.TryParse(record.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            id = parsed;
        }

        return await _assetViewModel.EnsureAssetAsync(id, record.Code).ConfigureAwait(false);
    }

    private static bool AreSameAsset(Asset? left, Asset? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        if (left.Id != 0 && right.Id != 0 && left.Id == right.Id)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(left.AssetCode)
            && !string.IsNullOrWhiteSpace(right.AssetCode)
            && string.Equals(left.AssetCode, right.AssetCode, StringComparison.OrdinalIgnoreCase);
    }

    private bool CanGenerateCode()
        => !IsBusy && IsInEditMode;

    private bool CanPreviewQr()
    {
        if (IsBusy)
        {
            return false;
        }

        var asset = _assetViewModel.SelectedAsset;
        var payload = asset?.QrPayload ?? _assetViewModel.QrPayload;

        return !string.IsNullOrWhiteSpace(payload);
    }

    private async Task GenerateCodeAsync()
    {
        if (!IsInEditMode)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var asset = EnsureSelectedAsset();
            var code = EnsureEditorCode(force: true, suppressDirty: false);
            var payload = EnsureEditorQrPayload(code, suppressDirty: false);
            var path = await EnsureEditorQrImageAsync(payload, code, suppressDirty: false).ConfigureAwait(false);
            var displayCode = string.IsNullOrWhiteSpace(asset.AssetCode) ? code : asset.AssetCode;
            var displayPath = string.IsNullOrWhiteSpace(asset.QrCode) ? path : asset.QrCode;
            var resolvedPath = string.IsNullOrWhiteSpace(displayPath)
                ? _localization.GetString("Module.Assets.Status.QrPathUnavailable")
                : displayPath;
            StatusMessage = _localization.GetString(
                "Module.Assets.Status.CodeAndQrGenerated",
                displayCode,
                resolvedPath);
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.GetString("Module.Assets.Status.QrGenerationFailed", ex.Message);
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
            var asset = EnsureSelectedAsset();
            var machine = _assetViewModel.ToMachine(_loadedMachine);
            await SynchronizeIdentifiersAsync(machine, forceCode: false, suppressDirty: false).ConfigureAwait(false);
            var path = string.IsNullOrWhiteSpace(asset.QrCode)
                ? machine.QrCode ?? _assetViewModel.QrCode
                : asset.QrCode;
            if (string.IsNullOrWhiteSpace(path))
            {
                StatusMessage = _localization.GetString("Module.Assets.Status.QrGenerationFailed", _localization.GetString("Module.Assets.Status.QrPathUnavailable"));
            }
            else
            {
                _shellInteraction.PreviewDocument(path);
                StatusMessage = _localization.GetString("Module.Assets.Status.QrGenerated", path);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.GetString("Module.Assets.Status.QrGenerationFailed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            UpdateCommandStates();
        }
    }

    private bool CanAttachDocument()
        => !IsBusy
           && !IsEditorEnabled
           && _assetViewModel.SelectedAsset is { Id: > 0 };

    private async Task AttachDocumentAsync()
    {
        var asset = _assetViewModel.SelectedAsset;
        if (asset is null || asset.Id <= 0)
        {
            StatusMessage = _localization.GetString("Module.Assets.Status.SaveBeforeAttachment");
            return;
        }

        try
        {
            IsBusy = true;
            var displayName = !string.IsNullOrWhiteSpace(asset.AssetName)
                ? asset.AssetName
                : asset.Name;
            var files = await _filePicker.PickFilesAsync(
                    new FilePickerRequest(AllowMultiple: true, Title: $"Attach files to {displayName}"))
                .ConfigureAwait(false);

            if (files is null || files.Count == 0)
            {
                StatusMessage = _localization.GetString("Module.Assets.Status.AttachmentCancelled");
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
                    EntityId = asset.Id,
                    UploadedById = uploadedBy,
                    Reason = $"asset:{asset.Id}",
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
            StatusMessage = _localization.GetString("Module.Assets.Status.AttachmentFailed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            UpdateCommandStates();
        }
    }

    private async Task InitializeEditorIdentifiersAsync(bool resetDirty = false)
    {
        var machine = _assetViewModel.ToMachine(_loadedMachine);
        try
        {
            await SynchronizeIdentifiersAsync(
                    machine,
                    forceCode: false,
                    suppressDirty: true,
                    resetDirtyAfter: resetDirty)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.GetString("Module.Assets.Status.QrGenerationFailed", ex.Message);
        }
    }

    private async Task SynchronizeIdentifiersAsync(
        Machine machine,
        bool forceCode,
        bool suppressDirty,
        bool resetDirtyAfter = false,
        CancellationToken cancellationToken = default)
    {
        if (machine is null)
        {
            return;
        }

        var asset = EnsureSelectedAsset();
        var code = EnsureEditorCode(forceCode, suppressDirty);
        var payload = EnsureEditorQrPayload(code, suppressDirty);
        var path = await EnsureEditorQrImageAsync(payload, code, suppressDirty, cancellationToken).ConfigureAwait(false);

        machine.Code = code;
        machine.QrPayload = payload;
        machine.QrCode = path;

        asset.AssetCode = code;
        asset.QrPayload = payload;
        asset.QrCode = path;

        if (resetDirtyAfter)
        {
            ResetDirty();
        }
    }

    private string EnsureEditorCode(bool force, bool suppressDirty)
    {
        var asset = EnsureSelectedAsset();
        var existing = asset.AssetCode;
        if (string.IsNullOrWhiteSpace(existing))
        {
            existing = _assetViewModel.Code;
        }

        if (!force && !string.IsNullOrWhiteSpace(existing))
        {
            var normalized = existing.Trim();
            if (!string.Equals(asset.AssetCode, normalized, StringComparison.Ordinal))
            {
                asset.AssetCode = normalized;
            }

            if (!string.Equals(_assetViewModel.Code, normalized, StringComparison.Ordinal))
            {
                if (suppressDirty)
                {
                    UpdateAssetWithoutDirty(() => _assetViewModel.Code = normalized);
                }
                else
                {
                    _assetViewModel.Code = normalized;
                }
            }

            return normalized;
        }

        var generated = _codeGeneratorService.GenerateMachineCode(
            string.IsNullOrWhiteSpace(asset.AssetName)
                ? string.IsNullOrWhiteSpace(_assetViewModel.Name) ? null : _assetViewModel.Name
                : asset.AssetName,
            string.IsNullOrWhiteSpace(asset.Manufacturer)
                ? string.IsNullOrWhiteSpace(_assetViewModel.Manufacturer) ? null : _assetViewModel.Manufacturer
                : asset.Manufacturer);

        if (suppressDirty)
        {
            UpdateAssetWithoutDirty(() =>
            {
                _assetViewModel.Code = generated;
                asset.AssetCode = generated;
            });
        }
        else
        {
            _assetViewModel.Code = generated;
            asset.AssetCode = generated;
        }

        return generated;
    }

    private string EnsureEditorQrPayload(string code, bool suppressDirty)
    {
        var asset = EnsureSelectedAsset();
        var payload = BuildQrPayload(code);
        if (string.Equals(asset.QrPayload, payload, StringComparison.Ordinal)
            && string.Equals(_assetViewModel.QrPayload, payload, StringComparison.Ordinal))
        {
            return payload;
        }

        if (suppressDirty)
        {
            UpdateAssetWithoutDirty(() =>
            {
                _assetViewModel.QrPayload = payload;
                asset.QrPayload = payload;
            });
        }
        else
        {
            _assetViewModel.QrPayload = payload;
            asset.QrPayload = payload;
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

        var asset = EnsureSelectedAsset();
        var path = await SaveQrImageAsync(payload, code, _assetViewModel.Id, cancellationToken).ConfigureAwait(false);

        if (suppressDirty)
        {
            UpdateAssetWithoutDirty(() =>
            {
                _assetViewModel.QrCode = path;
                asset.QrCode = path;
            });
        }
        else
        {
            _assetViewModel.QrCode = path;
            asset.QrCode = path;
        }

        return path;
    }

    private static string BuildQrPayload(string code)
    {
        var identifier = string.IsNullOrWhiteSpace(code)
            ? "pending"
            : Uri.EscapeDataString(code.Trim());
        return $"yasgmp://machine/{identifier}";
    }

    private async Task<string> SaveQrImageAsync(
        string payload,
        string code,
        int editorId,
        CancellationToken cancellationToken)
    {
        var appData = _platformService.GetAppDataDirectory();
        var qrDirectory = Path.Combine(appData, "Assets", "QrCodes");
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
            return "asset";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            builder.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);
        }

        return builder.ToString();
    }

    private void UpdateCommandStates()
    {
        AttachDocumentCommand.NotifyCanExecuteChanged();
        GenerateCodeCommand.NotifyCanExecuteChanged();
        PreviewQrCommand.NotifyCanExecuteChanged();
    }

    private static ModuleRecord ToRecord(Asset asset)
    {
        var fields = new List<InspectorField>
        {
            new("Location", asset.Location ?? "-"),
            new("Model", asset.Model ?? "-"),
            new("Manufacturer", asset.Manufacturer ?? "-"),
            new("Status", asset.Status ?? "-"),
            new("Installed", asset.InstallDate?.ToString("d", CultureInfo.CurrentCulture) ?? "-")
        };

        return new ModuleRecord(
            asset.Id.ToString(CultureInfo.InvariantCulture),
            asset.AssetName,
            asset.AssetCode,
            asset.Status,
            asset.Description,
            fields,
            WorkOrdersModuleViewModel.ModuleKey,
            asset.Id);
    }

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
}
