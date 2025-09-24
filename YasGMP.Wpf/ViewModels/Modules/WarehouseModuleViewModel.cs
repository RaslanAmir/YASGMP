using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed class WarehouseModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "Warehouse";

    public WarehouseModuleViewModel(
        DatabaseService databaseService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Warehouse", databaseService, cflDialogService, shellInteraction, navigation)
    {
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var warehouses = await Database.GetWarehousesAsync().ConfigureAwait(false);
        return warehouses.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new("WH-01", "Main Warehouse", "WH-01", "Qualified", "Primary GMP warehouse",
                new[]
                {
                    new InspectorField("Location", "Building B"),
                    new InspectorField("Responsible", "John Doe"),
                    new InspectorField("Qualified", "Yes")
                },
                null, null),
            new("WH-02", "Cold Storage", "WH-02", "Qualified", "2-8°C storage" ,
                new[]
                {
                    new InspectorField("Location", "Building C"),
                    new InspectorField("Responsible", "Jane Smith"),
                    new InspectorField("Climate", "2-8°C")
                },
                null, null)
        };

    private static ModuleRecord ToRecord(Warehouse warehouse)
    {
        var fields = new List<InspectorField>
        {
            new("Location", warehouse.Location),
            new("Responsible", warehouse.LegacyResponsibleName ?? warehouse.Responsible?.FullName ?? "-"),
            new("Status", warehouse.Status ?? "-"),
            new("Created", warehouse.CreatedAt.ToString("g", CultureInfo.CurrentCulture))
        };

        return new ModuleRecord(
            warehouse.Id.ToString(CultureInfo.InvariantCulture),
            warehouse.Name,
            warehouse.Name,
            warehouse.Status,
            warehouse.Note,
            fields,
            null,
            warehouse.Id);
    }
}
