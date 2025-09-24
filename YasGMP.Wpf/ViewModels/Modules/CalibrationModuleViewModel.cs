using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed class CalibrationModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "Calibration";

    public CalibrationModuleViewModel(
        DatabaseService databaseService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Calibration", databaseService, cflDialogService, shellInteraction, navigation)
    {
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var calibrations = await Database.GetAllCalibrationsAsync().ConfigureAwait(false);
        return calibrations.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new("CAL-100", "pH Meter Calibration", "CAL-100", "Scheduled", "Annual calibration",
                new[]
                {
                    new InspectorField("Asset", "pH Meter"),
                    new InspectorField("Date", System.DateTime.Now.AddDays(-10).ToString("d", CultureInfo.CurrentCulture)),
                    new InspectorField("Due", System.DateTime.Now.AddMonths(11).ToString("d", CultureInfo.CurrentCulture))
                },
                AssetsModuleViewModel.ModuleKey, 2),
            new("CAL-200", "Balance Calibration", "CAL-200", "Completed", "Semi-annual calibration",
                new[]
                {
                    new InspectorField("Asset", "Analytical Balance"),
                    new InspectorField("Date", System.DateTime.Now.AddDays(-5).ToString("d", CultureInfo.CurrentCulture)),
                    new InspectorField("Due", System.DateTime.Now.AddMonths(6).ToString("d", CultureInfo.CurrentCulture))
                },
                AssetsModuleViewModel.ModuleKey, 3)
        };

    private static ModuleRecord ToRecord(Calibration calibration)
    {
        var fields = new List<InspectorField>
        {
            new("Component Id", calibration.ComponentId.ToString(CultureInfo.InvariantCulture)),
            new("Supplier Id", calibration.SupplierId?.ToString(CultureInfo.InvariantCulture) ?? "-"),
            new("Calibration Date", calibration.CalibrationDate.ToString("d", CultureInfo.CurrentCulture)),
            new("Next Due", calibration.NextDue.ToString("d", CultureInfo.CurrentCulture)),
            new("Result", calibration.Result)
        };

        return new ModuleRecord(
            calibration.Id.ToString(CultureInfo.InvariantCulture),
            $"Calibration #{calibration.Id}",
            calibration.CertDoc,
            calibration.Status,
            calibration.Comment,
            fields,
            AssetsModuleViewModel.ModuleKey,
            calibration.ComponentId);
    }
}
