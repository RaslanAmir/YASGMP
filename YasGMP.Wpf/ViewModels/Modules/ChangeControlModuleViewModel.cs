using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Models.DTO;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed class ChangeControlModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "ChangeControl";

    public ChangeControlModuleViewModel(
        DatabaseService databaseService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Change Control", databaseService, cflDialogService, shellInteraction, navigation)
    {
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var changeControls = await Database.GetChangeControlsAsync().ConfigureAwait(false);
        return changeControls.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new(
                "CC-2024-001",
                "Formulation update",
                "CC-2024-001",
                "Under Review",
                "Change request to introduce new excipient supplier",
                new[]
                {
                    new InspectorField("Requested", System.DateTime.Now.AddDays(-7).ToString("d", CultureInfo.CurrentCulture)),
                    new InspectorField("Status", "Under Review"),
                    new InspectorField("Owner", "Quality Lead"),
                },
                CapaModuleViewModel.ModuleKey,
                2001),
            new(
                "CC-2024-002",
                "Clean room HVAC tweak",
                "CC-2024-002",
                "Draft",
                "Adjust HVAC schedule for energy savings",
                new[]
                {
                    new InspectorField("Requested", System.DateTime.Now.AddDays(-2).ToString("d", CultureInfo.CurrentCulture)),
                    new InspectorField("Status", "Draft"),
                    new InspectorField("Owner", "Facilities"),
                },
                null,
                null)
        };

    private static ModuleRecord ToRecord(ChangeControlSummaryDto changeControl)
    {
        var fields = new List<InspectorField>
        {
            new("Status", string.IsNullOrWhiteSpace(changeControl.Status) ? "-" : changeControl.Status),
            new("Requested", changeControl.DateRequested?.ToString("d", CultureInfo.CurrentCulture) ?? "-"),
        };

        // TODO: Hydrate approvers/impacted systems once the extended DTO is exposed via AppCore.
        return new ModuleRecord(
            changeControl.Id.ToString(CultureInfo.InvariantCulture),
            changeControl.Title,
            changeControl.Code,
            changeControl.Status,
            changeControl.Title,
            fields,
            CapaModuleViewModel.ModuleKey,
            changeControl.Id);
    }
}
