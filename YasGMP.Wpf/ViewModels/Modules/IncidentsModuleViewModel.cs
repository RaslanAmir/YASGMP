using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed class IncidentsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "Incidents";

    public IncidentsModuleViewModel(
        DatabaseService databaseService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Incidents", databaseService, cflDialogService, shellInteraction, navigation)
    {
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var incidents = await Database.GetAllIncidentsAsync().ConfigureAwait(false);
        return incidents.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new(
                "INC-2024-01",
                "Deviation in filling line",
                "INC-2024-01",
                "Investigating",
                "Operator reported pressure drop on filling line",
                new[]
                {
                    new InspectorField("Type", "Deviation"),
                    new InspectorField("Priority", "High"),
                    new InspectorField("Detected", System.DateTime.Now.AddHours(-4).ToString("g", CultureInfo.CurrentCulture)),
                    new InspectorField("Assigned", "QA Investigator"),
                    new InspectorField("Linked CAPA", "CAPA-001")
                },
                CapaModuleViewModel.ModuleKey,
                101),
            new(
                "INC-2024-02",
                "Audit trail alert",
                "INC-2024-02",
                "Open",
                "Unexpected login attempts captured by monitoring",
                new[]
                {
                    new InspectorField("Type", "Security"),
                    new InspectorField("Priority", "Medium"),
                    new InspectorField("Detected", System.DateTime.Now.AddHours(-1).ToString("g", CultureInfo.CurrentCulture)),
                    new InspectorField("Assigned", "IT Security"),
                    new InspectorField("Linked Work Order", "WO-1002")
                },
                WorkOrdersModuleViewModel.ModuleKey,
                1002)
        };

    private static ModuleRecord ToRecord(Incident incident)
    {
        var relatedModule = incident.CapaCaseId.HasValue
            ? CapaModuleViewModel.ModuleKey
            : incident.WorkOrderId.HasValue
                ? WorkOrdersModuleViewModel.ModuleKey
                : null;
        var relatedParameter = incident.CapaCaseId ?? incident.WorkOrderId;

        var fields = new List<InspectorField>
        {
            new("Type", string.IsNullOrWhiteSpace(incident.Type) ? "-" : incident.Type),
            new("Priority", string.IsNullOrWhiteSpace(incident.Priority) ? "-" : incident.Priority),
            new("Detected", incident.DetectedAt.ToString("g", CultureInfo.CurrentCulture)),
            new("Status", incident.Status),
            new("Assigned", incident.AssignedTo?.FullName ?? incident.AssignedTo?.Username ?? "-"),
        };

        if (!string.IsNullOrWhiteSpace(incident.Classification))
        {
            fields.Add(new InspectorField("Classification", incident.Classification));
        }

        // TODO: Surface incident action timeline once the action log view is available in WPF.
        return new ModuleRecord(
            incident.Id.ToString(CultureInfo.InvariantCulture),
            incident.Title,
            incident.Id.ToString(CultureInfo.InvariantCulture),
            incident.Status,
            incident.Description,
            fields,
            relatedModule,
            relatedParameter);
    }
}
