using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using YasGMP.Models.DTO;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed partial class AuditModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "Audit";

    public AuditModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Audit Trail", databaseService, cflDialogService, shellInteraction, navigation)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

        FilterFrom = DateTime.Today.AddDays(-30);
        FilterTo = DateTime.Today;
        SelectedAction = ActionOptions[0];
    }

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

            var records = audits?.Select(MapToRecord).ToList() ?? new List<ModuleRecord>();
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

    public IReadOnlyList<string> ActionOptions { get; } = new[]
    {
        "All",
        "CREATE",
        "UPDATE",
        "DELETE",
        "SIGN",
        "ROLLBACK",
        "EXPORT"
    };

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

    private readonly AuditService _auditService;

    protected virtual async Task<IReadOnlyList<AuditEntryDto>> QueryAuditsAsync(
        string user,
        string entity,
        string action,
        DateTime from,
        DateTime to)
        => await _auditService.GetFilteredAudits(user, entity, action, from, to).ConfigureAwait(false);

    private static (DateTime QueryFrom, DateTime QueryTo, DateTime FilterFrom, DateTime FilterTo) NormalizeDateRange(DateTime? from, DateTime? to)
    {
        var today = DateTime.Today;

        var normalizedFrom = (from?.Date ?? today.AddDays(-30));
        var normalizedToCandidate = to?.Date
            ?? (from.HasValue ? normalizedFrom : today);

        if (normalizedToCandidate < normalizedFrom)
        {
            (normalizedFrom, normalizedToCandidate) = (normalizedToCandidate, normalizedFrom);
        }

        var normalizedTo = normalizedToCandidate.Date.AddDays(1).AddTicks(-1);

        return (normalizedFrom, normalizedTo, normalizedFrom, normalizedToCandidate);
    }

    protected override bool MatchesSearch(ModuleRecord record, string searchText)
        => base.MatchesSearch(record, searchText)
           || record.InspectorFields.Any(field =>
               field.Value?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);

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

        var key = entry.Id?.ToString(CultureInfo.InvariantCulture)
            ?? $"{entry.Timestamp:O}|{entry.Entity}|{entry.Action}";

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
}
