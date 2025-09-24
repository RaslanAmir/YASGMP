using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed class AuditModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "Audit";

    public AuditModuleViewModel(
        DatabaseService databaseService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Audit Trail", databaseService, cflDialogService, shellInteraction, navigation)
    {
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var events = await Database.GetRecentDashboardEventsAsync(100).ConfigureAwait(false);
        return events.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new("AUD-001", "User login", "LOGIN_SUCCESS", "info", "User admin logged in",
                new[]
                {
                    new InspectorField("User", "admin"),
                    new InspectorField("Timestamp", System.DateTime.Now.AddMinutes(-5).ToString("g", CultureInfo.CurrentCulture)),
                    new InspectorField("Module", "Security")
                },
                "Security", 1),
            new("AUD-002", "Work order closed", "WO_CLOSE", "audit", "WO-1001 closed",
                new[]
                {
                    new InspectorField("User", "tech"),
                    new InspectorField("Timestamp", System.DateTime.Now.AddMinutes(-60).ToString("g", CultureInfo.CurrentCulture)),
                    new InspectorField("Module", "WorkOrders")
                },
                "WorkOrders", 1001)
        };

    private static ModuleRecord ToRecord(DashboardEvent evt)
    {
        var fields = new List<InspectorField>
        {
            new("Timestamp", evt.Timestamp.ToString("g", CultureInfo.CurrentCulture)),
            new("Severity", evt.Severity),
            new("Table", evt.RelatedModule ?? "system"),
            new("Record Id", evt.RelatedRecordId?.ToString(CultureInfo.InvariantCulture) ?? "-"),
        };

        return new ModuleRecord(
            evt.Id.ToString(CultureInfo.InvariantCulture),
            evt.EventType,
            evt.EventType,
            evt.Severity,
            evt.Description,
            fields,
            evt.RelatedModule,
            evt.RelatedRecordId);
    }
}
