using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed class SuppliersModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "Suppliers";

    public SuppliersModuleViewModel(
        DatabaseService databaseService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Suppliers", databaseService, cflDialogService, shellInteraction, navigation)
    {
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var suppliers = await Database.GetAllSuppliersAsync().ConfigureAwait(false);
        return suppliers.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new("SUP-001", "Acme Calibration", "SUP-001", "Approved", "Calibration vendor",
                new[]
                {
                    new InspectorField("Category", "Calibration"),
                    new InspectorField("Phone", "+385 91 2222"),
                    new InspectorField("Email", "support@acme-calibration.com")
                },
                CalibrationModuleViewModel.ModuleKey, null),
            new("SUP-002", "Steris Service", "SUP-002", "Approved", "Maintenance vendor",
                new[]
                {
                    new InspectorField("Category", "Maintenance"),
                    new InspectorField("Phone", "+385 91 3333"),
                    new InspectorField("Email", "service@steris.com")
                },
                WorkOrdersModuleViewModel.ModuleKey, null)
        };

    private static ModuleRecord ToRecord(Supplier supplier)
    {
        var fields = new List<InspectorField>
        {
            new("Category", supplier.Category ?? "-"),
            new("Phone", supplier.Phone ?? "-"),
            new("Email", supplier.Email ?? "-"),
            new("Status", supplier.Status ?? "-"),
            new("Rating", supplier.QualityRating?.ToString(CultureInfo.InvariantCulture) ?? "-"),
        };

        return new ModuleRecord(
            supplier.Id.ToString(CultureInfo.InvariantCulture),
            supplier.Name,
            supplier.Code,
            supplier.Status,
            supplier.Description,
            fields,
            CalibrationModuleViewModel.ModuleKey,
            supplier.Id);
    }
}
