using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;
/// <summary>
/// Represents the Admin Module View Model.
/// </summary>

public sealed class AdminModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>
    /// Represents the module key value.
    /// </summary>
    public const string ModuleKey = "Admin";
    /// <summary>
    /// Initializes a new instance of the AdminModuleViewModel class.
    /// </summary>

    public AdminModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, localization.GetString("Module.Title.Administration"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
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
                    CreateInspectorField("CFG-001", "Default Locale", "Value", "hr-HR"),
                    CreateInspectorField("CFG-001", "Default Locale", "Category", "System"),
                    CreateInspectorField(
                        "CFG-001",
                        "Default Locale",
                        "Updated",
                        System.DateTime.Now.AddDays(-2).ToString("g"))
                },
                null, null),
            new("CFG-002", "Maintenance Window", "maintenance_window", "Active", "Weekly downtime",
                new[]
                {
                    CreateInspectorField("CFG-002", "Maintenance Window", "Value", "Sundays 02:00-03:00"),
                    CreateInspectorField("CFG-002", "Maintenance Window", "Category", "System"),
                    CreateInspectorField(
                        "CFG-002",
                        "Maintenance Window",
                        "Updated",
                        System.DateTime.Now.AddDays(-10).ToString("g"))
                },
                null, null)
        };

    private ModuleRecord ToRecord(Setting setting)
    {
        var recordKey = setting.Id.ToString();
        var recordTitle = setting.Name ?? setting.Key ?? "Setting";

        InspectorField Field(string label, string? value) => CreateInspectorField(recordKey, recordTitle, label, value);

        var fields = new List<InspectorField>
        {
            Field("Category", setting.Category ?? "-"),
            Field("Value", setting.Value ?? string.Empty),
            Field("Description", setting.Description ?? string.Empty),
            Field("Updated", setting.UpdatedAt?.ToString("g") ?? "-"),
        };

        return new ModuleRecord(
            recordKey,
            recordTitle,
            setting.Key,
            "Active",
            setting.Description,
            fields,
            null,
            null);
    }
}
