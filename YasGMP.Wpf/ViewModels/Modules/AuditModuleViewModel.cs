using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using YasGMP.Models.DTO;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;
/// <summary>
/// Represents the audit module view model value.
/// </summary>

public sealed partial class AuditModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>
    /// Represents the module key value.
    /// </summary>
    public const string ModuleKey = "Audit";
    /// <summary>
    /// Initializes a new instance of the AuditModuleViewModel class.
    /// </summary>

    public AuditModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ExportService exportService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization,
        IServiceProvider serviceProvider)
        : base(ModuleKey, localization.GetString("Module.Title.AuditTrail"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _shellInteraction = shellInteraction ?? throw new ArgumentNullException(nameof(shellInteraction));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        ActionOptions = new[]
        {
            _localization.GetString("Module.Audit.Action.All"),
            _localization.GetString("Module.Audit.Action.Create"),
            _localization.GetString("Module.Audit.Action.Update"),
            _localization.GetString("Module.Audit.Action.Delete"),
            _localization.GetString("Module.Audit.Action.Sign"),
            _localization.GetString("Module.Audit.Action.Rollback"),
            _localization.GetString("Module.Audit.Action.Export")
        };

        _exportToPdfCommand = new AsyncRelayCommand(ExportToPdfAsync, CanExecuteExport);
        _exportToExcelCommand = new AsyncRelayCommand(ExportToExcelAsync, CanExecuteExport);
        _rollbackPreviewCommand = new AsyncRelayCommand(ExecuteRollbackPreviewAsync, CanExecuteRollbackPreview);

        FilterFrom = DateTime.Today.AddDays(-30);
        FilterTo = DateTime.Today;
        SelectedAction = ActionOptions[0];
    }
    /// <summary>
    /// Gets or sets the export to pdf command.
    /// </summary>

    public IAsyncRelayCommand ExportToPdfCommand => _exportToPdfCommand;
    /// <summary>
    /// Gets or sets the export to excel command.
    /// </summary>

    public IAsyncRelayCommand ExportToExcelCommand => _exportToExcelCommand;
    /// <summary>
    /// Gets the command that opens the rollback preview document.
    /// </summary>

    public IAsyncRelayCommand RollbackPreviewCommand => _rollbackPreviewCommand;

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var normalizedRange = NormalizeDateRange(FilterFrom, FilterTo);

        FilterFrom = normalizedRange.FilterFrom;
        FilterTo = normalizedRange.FilterTo;

        var actionFilter = string.Equals(SelectedAction, "All", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : SelectedAction ?? string.Empty;

        try
        {
            var audits = await QueryAuditsAsync(
                FilterUser?.Trim() ?? string.Empty,
                FilterEntity?.Trim() ?? string.Empty,
                actionFilter.Trim(),
                normalizedRange.QueryFrom,
                normalizedRange.QueryTo).ConfigureAwait(false);

            var auditEntries = audits?.ToList() ?? new List<AuditEntryDto>();
            _auditEntryLookup = auditEntries.ToDictionary(GetRecordKey, entry => entry, StringComparer.Ordinal);
            _lastAuditEntries = auditEntries;
            _lastFilterDescription = FormatFilterDescription(
                FilterUser,
                FilterEntity,
                SelectedAction,
                normalizedRange.FilterFrom,
                normalizedRange.FilterTo);

            var records = auditEntries.Select(MapToRecord).ToList();
            HasResults = records.Count > 0;
            HasError = false;

            return ToReadOnlyList(records);
        }
        catch
        {
            HasResults = false;
            HasError = true;
            _lastAuditEntries = null;
            _lastFilterDescription = null;
            throw;
        }
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            MapToRecord(new AuditEntryDto
            {
                Id = 1,
                Entity = "system_event_log",
                EntityId = "1",
                Action = "LOGIN_SUCCESS",
                Timestamp = DateTime.Now.AddMinutes(-5),
                Username = "admin",
                UserId = 1,
                IpAddress = "192.168.0.10",
                DeviceInfo = "OS=Windows; Host=QA-WS",
                DigitalSignature = "admin-sign",
                SignatureHash = "A1B2C3",
                Note = "User admin logged in successfully"
            }),
            MapToRecord(new AuditEntryDto
            {
                Id = 2,
                Entity = "work_orders",
                EntityId = "1001",
                Action = "CLOSE",
                Timestamp = DateTime.Now.AddMinutes(-60),
                Username = "tech",
                UserId = 7,
                IpAddress = "10.0.0.5",
                DeviceInfo = "OS=Windows; Host=MAINT-01",
                DigitalSignature = "tech-sign",
                SignatureHash = "FFEE0011",
                Note = "WO-1001 closed",
                Status = "audit"
            })
        };
    /// <summary>
    /// Gets or sets the action options.
    /// </summary>

    public IReadOnlyList<string> ActionOptions { get; }

    [ObservableProperty]
    private string? _filterUser;

    [ObservableProperty]
    private string? _filterEntity;

    [ObservableProperty]
    private string? _selectedAction;

    [ObservableProperty]
    private DateTime? _filterFrom;

    [ObservableProperty]
    private DateTime? _filterTo;

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private bool _hasError;

    partial void OnFilterUserChanged(string? value) => TriggerRefreshForFilterChange();

    partial void OnFilterEntityChanged(string? value) => TriggerRefreshForFilterChange();

    partial void OnSelectedActionChanged(string? value) => TriggerRefreshForFilterChange();

    partial void OnFilterFromChanged(DateTime? value) => TriggerRefreshForFilterChange();

    partial void OnFilterToChanged(DateTime? value) => TriggerRefreshForFilterChange();

    private readonly AuditService _auditService;
    private readonly ILocalizationService _localization;
    private readonly ExportService _exportService;
    private readonly IShellInteractionService _shellInteraction;
    private readonly IServiceProvider _serviceProvider;
    private readonly AsyncRelayCommand _exportToPdfCommand;
    private readonly AsyncRelayCommand _exportToExcelCommand;
    private readonly AsyncRelayCommand _rollbackPreviewCommand;
    private IReadOnlyList<AuditEntryDto>? _lastAuditEntries;
    private string? _lastFilterDescription;
    private Dictionary<string, AuditEntryDto> _auditEntryLookup = new(StringComparer.Ordinal);

    private void TriggerRefreshForFilterChange()
    {
        if (!IsInitialized || IsBusy)
        {
            return;
        }

        HasError = false;
        HasResults = false;
        StatusMessage = _localization.GetString("Module.Status.Loading", Title);
        _ = RefreshCommand.ExecuteAsync(null);
    }

    protected virtual async Task<IReadOnlyList<AuditEntryDto>> QueryAuditsAsync(
        string user,
        string entity,
        string action,
        DateTime from,
        DateTime to)
        => await _auditService.GetFilteredAudits(user, entity, action, from, to).ConfigureAwait(false);

    protected virtual Task<string> ExportAuditToPdfAsync(IReadOnlyList<AuditEntryDto> entries, string filterDescription)
        => _exportService.ExportAuditToPdf(entries, filterDescription);

    protected virtual Task<string> ExportAuditToExcelAsync(IReadOnlyList<AuditEntryDto> entries, string filterDescription)
        => _exportService.ExportAuditToExcel(entries, filterDescription);

    private static (DateTime QueryFrom, DateTime QueryTo, DateTime FilterFrom, DateTime FilterTo) NormalizeDateRange(DateTime? from, DateTime? to)
    {
        var today = DateTime.Today;

        var normalizedFrom = from?.Date ?? today.AddDays(-30);
        var normalizedTo = to?.Date
            ?? (from.HasValue ? normalizedFrom : today);

        var earlier = normalizedFrom <= normalizedTo ? normalizedFrom : normalizedTo;
        var later = normalizedFrom >= normalizedTo ? normalizedFrom : normalizedTo;

        var queryFrom = earlier;
        var queryTo = later.Date.AddDays(1).AddTicks(-1);

        return (queryFrom, queryTo, earlier, later);
    }

    protected override bool MatchesSearch(ModuleRecord record, string searchText)
        => base.MatchesSearch(record, searchText)
           || record.InspectorFields.Any(field =>
               field.Value?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);

    protected override Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        _rollbackPreviewCommand.NotifyCanExecuteChanged();
        return Task.CompletedTask;
    }

    partial void OnHasResultsChanged(bool value) => UpdateExportCommandStates();

    partial void OnIsBusyChanged(bool value) => UpdateExportCommandStates();

    private ModuleRecord MapToRecord(AuditEntryDto entry)
    {
        var timestamp = entry.Timestamp == DateTime.MinValue
            ? string.Empty
            : entry.Timestamp.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

        var userDisplay = !string.IsNullOrWhiteSpace(entry.Username)
            ? entry.Username
            : entry.UserId?.ToString(CultureInfo.InvariantCulture) ?? "-";

        if (entry.UserId.HasValue && !string.IsNullOrWhiteSpace(entry.Username))
        {
            userDisplay = $"{entry.Username} (#{entry.UserId.Value.ToString(CultureInfo.InvariantCulture)})";
        }

        var entityDisplay = string.IsNullOrWhiteSpace(entry.Entity)
            ? "system"
            : entry.Entity!;

        if (!string.IsNullOrWhiteSpace(entry.EntityId))
        {
            entityDisplay += $" #{entry.EntityId}";
        }

        var inspector = new List<InspectorField>
        {
            new("Timestamp", timestamp),
            new("User", userDisplay),
            new("Entity", entityDisplay),
            new("Action", entry.Action ?? string.Empty),
            new("IP Address", entry.IpAddress ?? string.Empty),
            new("Device", entry.DeviceInfo ?? string.Empty),
            new("Session", entry.SessionId ?? string.Empty),
            new("Digital Signature", entry.DigitalSignature ?? string.Empty),
            new("Signature Hash", entry.SignatureHash ?? string.Empty),
            new("Reason", entry.Note ?? string.Empty)
        };

        var key = GetRecordKey(entry);

        return new ModuleRecord(
            key,
            entityDisplay,
            entry.Action,
            entry.Status,
            entry.Note,
            inspector,
            relatedModuleKey: null,
            relatedParameter: null);
    }

    protected override string FormatLoadedStatus(int count)
        => count switch
        {
            <= 0 => "No audit entries match the current filters.",
            1 => "Loaded 1 audit entry.",
            _ => $"Loaded {count} audit entries."
        };

    private bool CanExecuteExport() => HasResults && !IsBusy;

    private async Task ExportToPdfAsync()
        => await ExecuteExportAsync(ExportAuditToPdfAsync, "PDF").ConfigureAwait(false);

    private async Task ExportToExcelAsync()
        => await ExecuteExportAsync(ExportAuditToExcelAsync, "Excel").ConfigureAwait(false);

    private async Task ExecuteExportAsync(
        Func<IReadOnlyList<AuditEntryDto>, string, Task<string>> exportOperation,
        string formatLabel)
    {
        if (exportOperation is null)
        {
            throw new ArgumentNullException(nameof(exportOperation));
        }

        if (_lastAuditEntries is null || _lastAuditEntries.Count == 0)
        {
            return;
        }

        try
        {
            IsBusy = true;
            HasError = false;

            var path = await exportOperation(_lastAuditEntries, _lastFilterDescription ?? string.Empty)
                .ConfigureAwait(false);

            StatusMessage = $"Audit log exported to {formatLabel}: {path}";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Failed to export audit log to {formatLabel}: {ex.Message}";
            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateExportCommandStates()
    {
        _exportToPdfCommand.NotifyCanExecuteChanged();
        _exportToExcelCommand.NotifyCanExecuteChanged();
        _rollbackPreviewCommand.NotifyCanExecuteChanged();
    }

    private static string GetRecordKey(AuditEntryDto entry)
        => entry.Id?.ToString(CultureInfo.InvariantCulture)
           ?? $"{entry.Timestamp:O}|{entry.Entity}|{entry.Action}";

    private bool CanExecuteRollbackPreview()
        => !IsBusy
           && SelectedRecord is not null
           && _auditEntryLookup.ContainsKey(SelectedRecord.Key);

    private Task ExecuteRollbackPreviewAsync()
    {
        if (SelectedRecord is null)
        {
            StatusMessage = _localization.GetString("Audit.Rollback.Status.NoSelection") ?? "Select an audit entry to preview.";
            return Task.CompletedTask;
        }

        if (!_auditEntryLookup.TryGetValue(SelectedRecord.Key, out var entry))
        {
            StatusMessage = _localization.GetString("Audit.Rollback.Status.MissingSnapshot") ?? "Rollback payloads are unavailable for the selected entry.";
            return Task.CompletedTask;
        }

        try
        {
            var document = ActivatorUtilities.CreateInstance<RollbackPreviewDocumentViewModel>(_serviceProvider, entry);
            _shellInteraction.OpenDocument(document);
            var openedFormat = _localization.GetString("Audit.Rollback.Status.Opened") ?? "Rollback preview opened for {0}.";
            var label = entry.EntityName ?? entry.Entity ?? "record";
            StatusMessage = string.Format(CultureInfo.CurrentCulture, openedFormat, label);
        }
        catch (Exception ex)
        {
            var failureFormat = _localization.GetString("Audit.Rollback.Status.OpenError") ?? "Failed to open rollback preview: {0}";
            StatusMessage = string.Format(CultureInfo.CurrentCulture, failureFormat, ex.Message);
        }

        return Task.CompletedTask;
    }

    private static string FormatFilterDescription(
        string? user,
        string? entity,
        string? action,
        DateTime filterFrom,
        DateTime filterTo)
    {
        var userText = string.IsNullOrWhiteSpace(user) ? "All Users" : user.Trim();
        var entityText = string.IsNullOrWhiteSpace(entity) ? "All Entities" : entity.Trim();
        var actionText = string.IsNullOrWhiteSpace(action) || string.Equals(action, "All", StringComparison.OrdinalIgnoreCase)
            ? "All Actions"
            : action.Trim();

        var fromText = filterFrom.ToString("d", CultureInfo.CurrentCulture);
        var toText = filterTo.ToString("d", CultureInfo.CurrentCulture);

        return $"User: {userText}; Entity: {entityText}; Action: {actionText}; Range: {fromText} – {toText}";
    }
}
