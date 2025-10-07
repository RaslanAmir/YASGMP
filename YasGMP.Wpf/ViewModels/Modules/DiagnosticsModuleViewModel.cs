using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Provides a diagnostics placeholder module in the WPF shell using SAP B1 scaffolding.</summary>
/// <remarks>
/// Form Modes: Currently uses Find/View for static diagnostics stubs; Add/Update are present for parity pending live instrumentation.
/// Audit & Logging: Emits no audit calls yet—once telemetry is wired the module will surface read-only health data fetched from shared services.
/// Localization: Inline strings such as `"Diagnostics"`, `"Healthy"`, and inspector labels remain until RESX keys are created.
/// Navigation: ModuleKey `Diagnostics` allows Golden Arrow jumps (e.g. to `Audit`) using the related module values supplied in design-time records, while status updates keep the shell informed during refresh.
/// </remarks>
public sealed class DiagnosticsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public new const string ModuleKey = "Diagnostics";

    public DiagnosticsModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Diagnostics", databaseService, cflDialogService, shellInteraction, navigation, auditService)
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




