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
    public new const string ModuleKey = "Calibration";

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

    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var calibrations = await Database.GetAllCalibrationsAsync().ConfigureAwait(false);
        var items = calibrations
            .Select(calibration =>
            {
                var key = calibration.Id.ToString(CultureInfo.InvariantCulture);
                var label = $"Calibration #{calibration.Id}";
                var descriptionParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(calibration.CertDoc))
                {
                    descriptionParts.Add(calibration.CertDoc!);
                }

                descriptionParts.Add(calibration.CalibrationDate.ToString("d", CultureInfo.CurrentCulture));
                descriptionParts.Add(calibration.NextDue.ToString("d", CultureInfo.CurrentCulture));

                if (!string.IsNullOrWhiteSpace(calibration.Result))
                {
                    descriptionParts.Add(calibration.Result!);
                }

                var description = string.Join(" • ", descriptionParts);

                return new CflItem(key, label, description);
            })
            .ToList();

        return new CflRequest("Select Calibration", items);
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
