using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed class QualityModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "Quality";

    public QualityModuleViewModel(
        DatabaseService databaseService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Quality", databaseService, cflDialogService, shellInteraction, navigation)
    {
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var capaCases = await Database.GetAllCapaCasesAsync().ConfigureAwait(false);
        return capaCases.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new("CAPA-001", "Deviation CAPA", "CAPA-001", "Open", "CAPA for deviation 2024-01",
                new[]
                {
                    new InspectorField("Priority", "High"),
                    new InspectorField("Opened", System.DateTime.Now.AddDays(-15).ToString("d", CultureInfo.CurrentCulture)),
                    new InspectorField("Assigned To", "QA Lead")
                },
                AuditModuleViewModel.ModuleKey, 10),
            new("CAPA-002", "Audit Finding CAPA", "CAPA-002", "In Progress", "CAPA for external audit finding",
                new[]
                {
                    new InspectorField("Priority", "Medium"),
                    new InspectorField("Opened", System.DateTime.Now.AddDays(-30).ToString("d", CultureInfo.CurrentCulture)),
                    new InspectorField("Assigned To", "Quality Manager")
                },
                AuditModuleViewModel.ModuleKey, 11)
        };

    private static ModuleRecord ToRecord(CapaCase capa)
    {
        var fields = new List<InspectorField>
        {
            new("Priority", string.IsNullOrWhiteSpace(capa.Priority) ? "-" : capa.Priority),
            new("Status", capa.Status),
            new("Opened", capa.OpenedAt.ToString("d", CultureInfo.CurrentCulture)),
            new("Assigned To", capa.AssignedTo?.FullName ?? capa.AssignedTo?.Username ?? "-"),
            new("Component", capa.ComponentId.ToString(CultureInfo.InvariantCulture))
        };

        return new ModuleRecord(
            capa.Id.ToString(CultureInfo.InvariantCulture),
            capa.Title,
            capa.CapaCode,
            capa.Status,
            capa.Description,
            fields,
            AuditModuleViewModel.ModuleKey,
            capa.Id);
    }
}
