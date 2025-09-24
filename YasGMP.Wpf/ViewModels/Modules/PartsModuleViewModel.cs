using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed class PartsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "Parts";

    public PartsModuleViewModel(
        DatabaseService databaseService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Parts & Stock", databaseService, cflDialogService, shellInteraction, navigation)
    {
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var parts = await Database.GetAllPartsAsync().ConfigureAwait(false);
        return parts.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new(
                "PRT-1001",
                "Pressure Gauge",
                "PG-001",
                "Active",
                "Stainless pressure gauge",
                new[]
                {
                    new InspectorField("SKU", "PG-001"),
                    new InspectorField("Category", "Instrumentation"),
                    new InspectorField("Stock", "12"),
                    new InspectorField("Min Stock", "5"),
                    new InspectorField("Location", "Aisle 3")
                },
                SuppliersModuleViewModel.ModuleKey,
                1),
            new(
                "PRT-1002",
                "Filter Cartridge",
                "FC-010",
                "Low",
                "0.2 µm sterilising filter",
                new[]
                {
                    new InspectorField("SKU", "FC-010"),
                    new InspectorField("Category", "Filtration"),
                    new InspectorField("Stock", "2"),
                    new InspectorField("Min Stock", "10"),
                    new InspectorField("Location", "Cold Room")
                },
                SuppliersModuleViewModel.ModuleKey,
                2)
        };

    private static ModuleRecord ToRecord(Part part)
    {
        var fields = new List<InspectorField>
        {
            new("SKU", part.Sku ?? part.Code),
            new("Category", string.IsNullOrWhiteSpace(part.Category) ? "-" : part.Category),
            new("Stock", part.Stock?.ToString(CultureInfo.InvariantCulture) ?? "-"),
            new("Min Stock", part.MinStockAlert?.ToString(CultureInfo.InvariantCulture) ?? "-"),
            new("Location", part.Location ?? "-"),
            new("Supplier", part.DefaultSupplierName ?? part.DefaultSupplier?.Name ?? "-"),
        };

        // TODO: Surface stock transaction drill-down once warehouse adapters are ported.
        return new ModuleRecord(
            part.Id.ToString(CultureInfo.InvariantCulture),
            part.Name,
            part.Code,
            part.Status,
            part.Description,
            fields,
            SuppliersModuleViewModel.ModuleKey,
            part.DefaultSupplierId);
    }
}
