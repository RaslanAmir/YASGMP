using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed class AssetsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public new const string ModuleKey = "Assets";

    public AssetsModuleViewModel(
        DatabaseService databaseService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Assets", databaseService, cflDialogService, shellInteraction, navigation)
    {
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var assets = await Database.GetAllAssetsFullAsync().ConfigureAwait(false);
        return assets.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new("AST-001", "Autoclave", "AUTO-001", "Active", "Steam autoclave",
                new[]
                {
                    new InspectorField("Location", "Building A - Cleanroom"),
                    new InspectorField("Manufacturer", "Steris"),
                    new InspectorField("Status", "Active")
                },
                WorkOrdersModuleViewModel.ModuleKey, 1001),
            new("AST-002", "pH Meter", "LAB-PH-12", "Calibration Due", "Metrohm pH meter",
                new[]
                {
                    new InspectorField("Location", "QC Lab"),
                    new InspectorField("Manufacturer", "Metrohm"),
                    new InspectorField("Status", "Calibration Due")
                },
                CalibrationModuleViewModel.ModuleKey, 502)
        };

    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var assets = await Database.GetAllAssetsFullAsync().ConfigureAwait(false);
        var items = assets
            .Select(asset =>
            {
                var key = asset.Id.ToString(CultureInfo.InvariantCulture);
                var label = string.IsNullOrWhiteSpace(asset.Name) ? key : asset.Name;
                var descriptionParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(asset.Code))
                {
                    descriptionParts.Add(asset.Code);
                }

                if (!string.IsNullOrWhiteSpace(asset.Location))
                {
                    descriptionParts.Add(asset.Location!);
                }

                if (!string.IsNullOrWhiteSpace(asset.Status))
                {
                    descriptionParts.Add(asset.Status!);
                }

                var description = descriptionParts.Count > 0
                    ? string.Join(" • ", descriptionParts)
                    : null;

                return new CflItem(key, label, description);
            })
            .ToList();

        return new CflRequest("Select Asset", items);
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

    private static ModuleRecord ToRecord(Asset asset)
    {
        var fields = new List<InspectorField>
        {
            new("Location", asset.Location ?? "-"),
            new("Model", asset.Model ?? "-"),
            new("Manufacturer", asset.Manufacturer ?? "-"),
            new("Status", asset.Status ?? "-"),
            new("Installed", asset.InstallDate?.ToString("d", CultureInfo.CurrentCulture) ?? "-")
        };

        return new ModuleRecord(
            asset.Id.ToString(CultureInfo.InvariantCulture),
            asset.Name,
            asset.Code,
            asset.Status,
            asset.Description,
            fields,
            WorkOrdersModuleViewModel.ModuleKey,
            asset.Id);
    }
}
