using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed class AttachmentsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "Attachments";

    public AttachmentsModuleViewModel(
        DatabaseService databaseService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Attachments", databaseService, cflDialogService, shellInteraction, navigation)
    {
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var attachments = await Database.GetAttachmentsFilteredAsync(null, null, null).ConfigureAwait(false);
        return attachments.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new("ATT-001", "Calibration Certificate", "certificate.pdf", "Approved", "Calibration PDF",
                new[]
                {
                    new InspectorField("Table", "calibrations"),
                    new InspectorField("Record", "100"),
                    new InspectorField("Uploaded", System.DateTime.Now.AddDays(-3).ToString("g", CultureInfo.CurrentCulture))
                },
                CalibrationModuleViewModel.ModuleKey, 100),
            new("ATT-002", "Work Order Photo", "photo.jpg", "Pending", "Machine photo",
                new[]
                {
                    new InspectorField("Table", "work_orders"),
                    new InspectorField("Record", "1001"),
                    new InspectorField("Uploaded", System.DateTime.Now.AddDays(-1).ToString("g", CultureInfo.CurrentCulture))
                },
                WorkOrdersModuleViewModel.ModuleKey, 1001)
        };

    private static ModuleRecord ToRecord(Attachment attachment)
    {
        var moduleKey = attachment.EntityTable?.ToLowerInvariant() switch
        {
            "calibrations" => CalibrationModuleViewModel.ModuleKey,
            "work_orders" => WorkOrdersModuleViewModel.ModuleKey,
            "capa_cases" => CapaModuleViewModel.ModuleKey,
            "users" => SecurityModuleViewModel.ModuleKey,
            _ => null
        };

        var fields = new List<InspectorField>
        {
            new("Table", attachment.EntityTable ?? "-"),
            new("Record", attachment.EntityId?.ToString(CultureInfo.InvariantCulture) ?? "-"),
            new("File Type", attachment.FileType ?? "-"),
            new("Created", attachment.CreatedAt.ToString("g", CultureInfo.CurrentCulture)),
            new("Status", attachment.Status ?? "-"),
        };

        return new ModuleRecord(
            attachment.Id.ToString(CultureInfo.InvariantCulture),
            attachment.FileName,
            attachment.FileName,
            attachment.Status,
            attachment.Description,
            fields,
            moduleKey,
            attachment.EntityId);
    }
}
