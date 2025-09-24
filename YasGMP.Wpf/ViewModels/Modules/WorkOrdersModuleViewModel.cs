using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed class WorkOrdersModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "WorkOrders";

    public WorkOrdersModuleViewModel(
        DatabaseService databaseService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Work Orders", databaseService, cflDialogService, shellInteraction, navigation)
    {
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var workOrders = await Database.GetAllWorkOrdersFullAsync().ConfigureAwait(false);
        return workOrders.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new("WO-1001", "Preventive maintenance - Autoclave", "WO-1001", "In Progress", "Monthly PM",
                new[]
                {
                    new InspectorField("Assigned To", "Technician A"),
                    new InspectorField("Priority", "High"),
                    new InspectorField("Due", System.DateTime.Now.AddDays(2).ToString("d", CultureInfo.CurrentCulture))
                },
                AssetsModuleViewModel.ModuleKey, 1),
            new("WO-1002", "Calibration - pH meter", "WO-1002", "Open", "Annual calibration",
                new[]
                {
                    new InspectorField("Assigned To", "Technician B"),
                    new InspectorField("Priority", "Medium"),
                    new InspectorField("Due", System.DateTime.Now.AddDays(5).ToString("d", CultureInfo.CurrentCulture))
                },
                CalibrationModuleViewModel.ModuleKey, 2)
        };

    private static ModuleRecord ToRecord(WorkOrder workOrder)
    {
        var fields = new List<InspectorField>
        {
            new("Assigned To", workOrder.AssignedTo?.FullName ?? workOrder.AssignedTo?.Username ?? "-"),
            new("Priority", workOrder.Priority),
            new("Status", workOrder.Status),
            new("Due Date", workOrder.DueDate?.ToString("d", CultureInfo.CurrentCulture) ?? "-"),
            new("Machine", workOrder.Machine?.Name ?? workOrder.MachineId.ToString(CultureInfo.InvariantCulture))
        };

        return new ModuleRecord(
            workOrder.Id.ToString(CultureInfo.InvariantCulture),
            workOrder.Title,
            workOrder.Title,
            workOrder.Status,
            workOrder.Description,
            fields,
            AssetsModuleViewModel.ModuleKey,
            workOrder.MachineId);
    }
}
