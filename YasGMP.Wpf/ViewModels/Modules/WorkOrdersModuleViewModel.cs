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
    public new const string ModuleKey = "WorkOrders";

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

    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var workOrders = await Database.GetAllWorkOrdersFullAsync().ConfigureAwait(false);
        var items = workOrders
            .Select(order =>
            {
                var key = order.Id.ToString(CultureInfo.InvariantCulture);
                var label = string.IsNullOrWhiteSpace(order.Title) ? key : order.Title;
                var descriptionParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(order.Status))
                {
                    descriptionParts.Add(order.Status!);
                }

                if (order.DueDate is not null)
                {
                    descriptionParts.Add(order.DueDate.Value.ToString("d", CultureInfo.CurrentCulture));
                }

                if (!string.IsNullOrWhiteSpace(order.Machine?.Name))
                {
                    descriptionParts.Add(order.Machine!.Name!);
                }

                var description = descriptionParts.Count > 0
                    ? string.Join(" • ", descriptionParts)
                    : null;

                return new CflItem(key, label, description);
            })
            .ToList();

        return new CflRequest("Select Work Order", items);
    }

    protected override Task OnCflSelectionAsync(CflResult result)
    {
        var search = result.Selected.Label;
        var match = Records.FirstOrDefault(r => r.Key == result.Selected.Key);
        if (match is not null)
        {
            SelectedRecord = match;
            search = match.Title;
        }

        SearchText = search;
        StatusMessage = $"Filtered {Title} by \"{search}\".";
        return Task.CompletedTask;
    }

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
