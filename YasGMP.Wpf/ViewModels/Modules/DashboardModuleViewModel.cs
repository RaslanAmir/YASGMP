using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed class DashboardModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "Dashboard";

    public DashboardModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, "Dashboard", databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var events = await Database.GetRecentDashboardEventsAsync(25).ConfigureAwait(false);
        return events.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var now = DateTime.Now;
        return new List<ModuleRecord>
        {
            new("evt-001", "Work order closed", "work_order_closed", "info", "Preventive maintenance completed",
                new[]
                {
                    new InspectorField("Timestamp", now.ToString("g", CultureInfo.CurrentCulture)),
                    new InspectorField("Module", "WorkOrders"),
                    new InspectorField("Severity", "info")
                },
                "WorkOrders", 101),
            new("evt-002", "CAPA escalated", "capa_escalated", "critical", "Deviation escalated to quality director",
                new[]
                {
                    new InspectorField("Timestamp", now.AddMinutes(-42).ToString("g", CultureInfo.CurrentCulture)),
                    new InspectorField("Module", "Quality"),
                    new InspectorField("Severity", "critical")
                },
                "Quality", 55)
        };
    }

    private static ModuleRecord ToRecord(DashboardEvent evt)
    {
        var fields = new List<InspectorField>
        {
            new("Timestamp", evt.Timestamp.ToString("g", CultureInfo.CurrentCulture)),
            new("Severity", evt.Severity),
            new("Module", evt.RelatedModule ?? "-"),
            new("Record Id", evt.RelatedRecordId?.ToString(CultureInfo.InvariantCulture) ?? "-")
        };

        if (!string.IsNullOrWhiteSpace(evt.Note))
        {
            fields.Add(new InspectorField("Notes", evt.Note));
        }

        var title = string.IsNullOrWhiteSpace(evt.Description) ? evt.EventType : evt.Description!;
        return new ModuleRecord(
            evt.Id.ToString(CultureInfo.InvariantCulture),
            title,
            evt.EventType,
            evt.Severity,
            evt.Description,
            fields,
            evt.RelatedModule,
            evt.RelatedRecordId);
    }
}
