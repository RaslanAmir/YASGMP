using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Provides a diagnostics placeholder module in the WPF shell using SAP B1 scaffolding.</summary>
/// <remarks>
/// Form Modes: Currently uses Find/View for static diagnostics stubs; Add/Update are present for parity pending live instrumentation.
/// Audit &amp; Logging: Emits no audit calls yet—once telemetry is wired the module will surface read-only health data fetched from shared services.
/// Localization: Inline strings such as `"Diagnostics"`, `"Healthy"`, and inspector labels remain until RESX keys are created.
/// Navigation: ModuleKey `Diagnostics` allows Golden Arrow jumps (e.g. to `Audit`) using the related module values supplied in design-time records, while status updates keep the shell informed during refresh.
/// </remarks>
public sealed class DiagnosticsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Shell registration key that binds Diagnostics into the docking layout.</summary>
    /// <remarks>Execution: Resolved when the shell composes modules and persists layouts. Form Mode: Identifier applies across Find/Add/View/Update. Localization: Currently paired with the inline caption "Diagnostics" until `Modules_Diagnostics_Title` is introduced.</remarks>
    public new const string ModuleKey = "Diagnostics";

    /// <summary>Initializes the Diagnostics module view model with domain and shell services.</summary>
    /// <remarks>Execution: Invoked when the shell activates the module or Golden Arrow navigation materializes it. Form Mode: Seeds Find/View immediately while deferring Add/Update wiring to later transitions. Localization: Relies on inline strings for tab titles and prompts until module resources exist.</remarks>
    public DiagnosticsModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Diagnostics", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
    }

    /// <summary>Loads Diagnostics records from domain services.</summary>
    /// <remarks>Execution: Triggered by Find refreshes and shell activation. Form Mode: Supplies data for Find/View while Add/Update reuse cached results. Localization: Emits inline status strings pending `Status_Diagnostics_Loaded` resources.</remarks>
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        // TODO: Replace with live diagnostics snapshot once logging/health adapters are wired for WPF.
        await Task.CompletedTask.ConfigureAwait(false);
        return CreateDesignTimeRecords();
    }

    /// <summary>Provides design-time sample data for the Diagnostics designer experience.</summary>
    /// <remarks>Execution: Invoked only by design-mode checks to support Blend/preview tooling. Form Mode: Mirrors Find mode to preview list layouts. Localization: Sample literals remain inline for clarity.</remarks>
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




