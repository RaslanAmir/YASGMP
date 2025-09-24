using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed class SecurityModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "Security";

    public SecurityModuleViewModel(
        DatabaseService databaseService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Security", databaseService, cflDialogService, shellInteraction, navigation)
    {
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var users = await Database.GetAllUsersAsync().ConfigureAwait(false);
        return users.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new("USR-001", "admin", "admin", "Active", "System administrator",
                new[]
                {
                    new InspectorField("Role", "Administrator"),
                    new InspectorField("Email", "admin@yasgmp.local"),
                    new InspectorField("Phone", "+385 91 0000")
                },
                AdminModuleViewModel.ModuleKey, 1),
            new("USR-002", "qa.lead", "qa.lead", "Active", "Quality manager",
                new[]
                {
                    new InspectorField("Role", "Quality"),
                    new InspectorField("Email", "qa@yasgmp.local"),
                    new InspectorField("Phone", "+385 91 1111")
                },
                QualityModuleViewModel.ModuleKey, 2)
        };

    private static ModuleRecord ToRecord(User user)
    {
        var fields = new List<InspectorField>
        {
            new("Full Name", user.FullName),
            new("Role", user.Role),
            new("Email", user.Email),
            new("Phone", user.Phone),
            new("Active", user.Active ? "Yes" : "No")
        };

        return new ModuleRecord(
            user.Id.ToString(CultureInfo.InvariantCulture),
            user.Username,
            user.Username,
            user.Active ? "Active" : "Inactive",
            user.DepartmentName,
            fields,
            AdminModuleViewModel.ModuleKey,
            user.Id);
    }
}
