using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed class SchedulingModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "Scheduling";

    public SchedulingModuleViewModel(
        DatabaseService databaseService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Scheduling", databaseService, cflDialogService, shellInteraction, navigation)
    {
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var jobs = await Database.GetAllScheduledJobsFullAsync().ConfigureAwait(false);
        return jobs.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new("JOB-001", "Weekly Work Order Digest", "JOB-001", "Active", "Send weekly summary",
                new[]
                {
                    new InspectorField("Next Due", System.DateTime.Now.AddDays(1).ToString("g", CultureInfo.CurrentCulture)),
                    new InspectorField("Recurrence", "0 8 * * MON"),
                    new InspectorField("Entity", "WorkOrders")
                },
                WorkOrdersModuleViewModel.ModuleKey, null),
            new("JOB-002", "Calibration Reminder", "JOB-002", "Paused", "Daily calibration reminder",
                new[]
                {
                    new InspectorField("Next Due", System.DateTime.Now.AddHours(12).ToString("g", CultureInfo.CurrentCulture)),
                    new InspectorField("Recurrence", "0 6 * * *"),
                    new InspectorField("Entity", "Calibration")
                },
                CalibrationModuleViewModel.ModuleKey, null)
        };

    private static ModuleRecord ToRecord(ScheduledJob job)
    {
        var fields = new List<InspectorField>
        {
            new("Next Due", job.NextDue.ToString("g", CultureInfo.CurrentCulture)),
            new("Recurrence", job.RecurrencePattern ?? "-"),
            new("Status", job.Status ?? "-"),
            new("Entity", job.EntityType ?? "-"),
            new("Created By", job.CreatedBy ?? "-"),
        };

        return new ModuleRecord(
            job.Id.ToString(CultureInfo.InvariantCulture),
            job.Name,
            job.Name,
            job.Status,
            job.Comment,
            fields,
            job.EntityType,
            job.EntityId);
    }
}
