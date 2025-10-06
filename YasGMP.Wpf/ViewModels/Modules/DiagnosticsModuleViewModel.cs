using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed class DiagnosticsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "Diagnostics";

    public DiagnosticsModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, localization.GetString("Module.Title.Diagnostics"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        // TODO: Replace with live diagnostics snapshot once logging/health adapters are wired for WPF.
        await Task.CompletedTask.ConfigureAwait(false);
        return CreateDesignTimeRecords();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new(
                "DIAG-LOG",
                "Recent log events",
                "Log",
                "Healthy",
                "Aggregated log feed across services",
                new[]
                {
                    new InspectorField("Errors (24h)", "2"),
                    new InspectorField("Warnings (24h)", "5"),
                    new InspectorField("Last event", System.DateTime.Now.AddMinutes(-12).ToString("g", CultureInfo.CurrentCulture))
                },
                AuditModuleViewModel.ModuleKey,
                null),
            new(
                "DIAG-HEALTH",
                "Service health",
                "Health",
                "Healthy",
                "Ping results for downstream integrations",
                new[]
                {
                    new InspectorField("Database", "OK"),
                    new InspectorField("SignalR", "Connecting"),
                    new InspectorField("Background Jobs", "Running"),
                },
                null,
                null)
        };
}
