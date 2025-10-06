using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed class AdminModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "Admin";

    public AdminModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, "Administration", databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var settings = await Database.GetAllSettingsFullAsync().ConfigureAwait(false);
        return settings.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new("CFG-001", "Default Locale", "locale", "Active", "System default locale",
                new[]
                {
                    new InspectorField("Value", "hr-HR"),
                    new InspectorField("Category", "System"),
                    new InspectorField("Updated", System.DateTime.Now.AddDays(-2).ToString("g"))
                },
                null, null),
            new("CFG-002", "Maintenance Window", "maintenance_window", "Active", "Weekly downtime",
                new[]
                {
                    new InspectorField("Value", "Sundays 02:00-03:00"),
                    new InspectorField("Category", "System"),
                    new InspectorField("Updated", System.DateTime.Now.AddDays(-10).ToString("g"))
                },
                null, null)
        };

    private static ModuleRecord ToRecord(Setting setting)
    {
        var fields = new List<InspectorField>
        {
            new("Category", setting.Category ?? "-"),
            new("Value", setting.Value ?? string.Empty),
            new("Description", setting.Description ?? string.Empty),
            new("Updated", setting.UpdatedAt?.ToString("g") ?? "-"),
        };

        return new ModuleRecord(
            setting.Id.ToString(),
            setting.Name ?? setting.Key ?? "Setting",
            setting.Key,
            "Active",
            setting.Description,
            fields,
            null,
            null);
    }
}
