using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using YasGMP.Models.DTO;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed partial class ApiAuditModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "ApiAudit";
    private const int DefaultResultLimit = 500;

    public ApiAuditModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "API Audit Trail", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

        _actionOptions = new ObservableCollection<string>();
        ActionOptions = new ReadOnlyObservableCollection<string>(_actionOptions);
        _actionOptions.Add("All");

        FilterFrom = DateTime.Today.AddDays(-30);
        FilterTo = DateTime.Today;
        SelectedAction = _actionOptions.First();
    }

    public ReadOnlyObservableCollection<string> ActionOptions { get; }

    [ObservableProperty]
    private string? _filterApiKey;

    [ObservableProperty]
    private string? _filterUser;

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

    private readonly AuditService _auditService;
    private readonly ObservableCollection<string> _actionOptions;

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var normalized = NormalizeDateRange(FilterFrom, FilterTo);
        FilterFrom = normalized.FilterFrom;
        FilterTo = normalized.FilterTo;

        var apiKeyFilter = FilterApiKey?.Trim() ?? string.Empty;
        var userFilter = FilterUser?.Trim() ?? string.Empty;
        var actionFilter = string.IsNullOrWhiteSpace(SelectedAction) || string.Equals(SelectedAction, "All", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : SelectedAction!.Trim();

        try
        {
            var entries = await QueryApiAuditsAsync(
                    apiKeyFilter,
                    userFilter,
                    actionFilter,
                    normalized.QueryFrom,
                    normalized.QueryTo,
                    DefaultResultLimit)
                .ConfigureAwait(false);

            var list = entries?.ToList() ?? new List<ApiAuditEntryDto>();
            UpdateActionOptions(list);

            var records = list.Select(MapToRecord).ToList();
            HasResults = records.Count > 0;
            HasError = false;

            return ToReadOnlyList(records);
        }
        catch
        {
            HasResults = false;
            HasError = true;
            throw;
        }
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            MapToRecord(new ApiAuditEntryDto
            {
                Id = 1,
                ApiKeyId = 101,
                ApiKeyValue = "INT-PRIMARY-KEY-0001",
                ApiKeyDescription = "Integration Primary Key",
                ApiKeyOwnerUsername = "integration.bot",
                ApiKeyOwnerFullName = "Integration Bot",
                ApiKeyIsActive = true,
                Action = "POST /api/v1/assets",
                Timestamp = DateTime.UtcNow.AddMinutes(-15),
                Username = "integration.bot",
                UserId = 42,
                IpAddress = "198.51.100.12",
                RequestDetails = "{\"asset\":\"A-100\",\"action\":\"create\"}",
                Details = "HTTP 201 Created"
            }),
            MapToRecord(new ApiAuditEntryDto
            {
                Id = 2,
                ApiKeyId = 102,
                ApiKeyValue = "QA-REPORTING-KEY-88",
                ApiKeyDescription = "QA Reporting",
                ApiKeyOwnerUsername = "qa.service",
                ApiKeyOwnerFullName = "QA Reporting Service",
                ApiKeyIsActive = false,
                Action = "GET /api/v1/audit/export",
                Timestamp = DateTime.UtcNow.AddHours(-2),
                Username = "qa.viewer",
                UserId = 77,
                IpAddress = "203.0.113.44",
                RequestDetails = "{\"action\":\"export\",\"format\":\"csv\"}",
                Details = "HTTP 403 Forbidden"
            })
        };

    protected override string FormatLoadedStatus(int count)
        => count switch
        {
            <= 0 => "No API audit entries match the current filters.",
            1 => "Loaded 1 API audit entry.",
            _ => $"Loaded {count} API audit entries."
        };

    protected override bool MatchesSearch(ModuleRecord record, string searchText)
        => base.MatchesSearch(record, searchText)
           || record.InspectorFields.Any(field =>
               field.Value?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);

    protected virtual Task<IReadOnlyList<ApiAuditEntryDto>> QueryApiAuditsAsync(
        string apiKey,
        string user,
        string action,
        DateTime from,
        DateTime to,
        int limit)
        => _auditService.GetApiAuditEntriesAsync(apiKey, user, action, from, to, limit);

    private ModuleRecord MapToRecord(ApiAuditEntryDto entry)
    {
        var timestamp = entry.EffectiveTimestamp?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? string.Empty;
        var userDisplay = entry.UserDisplay;
        var ownerDisplay = entry.OwnerDisplay;
        var keyStatus = entry.ApiKeyIsActive.HasValue
            ? (entry.ApiKeyIsActive.Value ? "Active" : "Disabled")
            : string.Empty;

        var inspector = new List<InspectorField>
        {
            new("Timestamp", timestamp),
            new("API Key", entry.ApiKeyDisplayLabel),
            new("User", userDisplay),
            new("IP Address", entry.IpAddress ?? string.Empty),
            new("Request Payload", entry.RequestDetails ?? string.Empty),
            new("Details", entry.Details ?? string.Empty),
            new("Owner", ownerDisplay),
            new("Key Status", keyStatus),
            new("Key Created", FormatDate(entry.ApiKeyCreatedAt)),
            new("Key Updated", FormatDate(entry.ApiKeyUpdatedAt)),
            new("Key Last Used", FormatDate(entry.ApiKeyLastUsedAt))
        };

        var key = entry.Id != 0
            ? entry.Id.ToString(CultureInfo.InvariantCulture)
            : $"{entry.EffectiveTimestamp:O}|{entry.ApiKeyId}|{entry.Action}";

        var status = keyStatus.Length > 0 ? keyStatus : null;

        return new ModuleRecord(
            key,
            entry.ApiKeyDisplayLabel,
            entry.Action,
            status,
            entry.Details ?? string.Empty,
            inspector);
    }

    private void UpdateActionOptions(IReadOnlyList<ApiAuditEntryDto> entries)
    {
        var desired = new List<string> { "All" };
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            var action = entry.Action?.Trim();
            if (string.IsNullOrWhiteSpace(action) || !seen.Add(action))
            {
                continue;
            }

            desired.Add(action);
        }

        desired = desired.Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(option => option.Equals("All", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(option => option, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!_actionOptions.SequenceEqual(desired))
        {
            _actionOptions.Clear();
            foreach (var option in desired)
            {
                _actionOptions.Add(option);
            }
        }

        if (string.IsNullOrWhiteSpace(SelectedAction) || !_actionOptions.Contains(SelectedAction))
        {
            SelectedAction = _actionOptions.FirstOrDefault();
        }
    }

    private static (DateTime QueryFrom, DateTime QueryTo, DateTime FilterFrom, DateTime FilterTo) NormalizeDateRange(DateTime? from, DateTime? to)
    {
        var today = DateTime.Today;
        var normalizedFrom = from?.Date ?? today.AddDays(-30);
        var normalizedTo = to?.Date ?? (from.HasValue ? normalizedFrom : today);

        if (normalizedFrom > normalizedTo)
        {
            (normalizedFrom, normalizedTo) = (normalizedTo, normalizedFrom);
        }

        var queryFrom = normalizedFrom;
        var queryTo = normalizedTo.Date.AddDays(1).AddTicks(-1);

        return (queryFrom, queryTo, normalizedFrom, normalizedTo);
    }

    private static string FormatDate(DateTime? value)
    {
        if (!value.HasValue)
        {
            return string.Empty;
        }

        return value.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
    }
}
