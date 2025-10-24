using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Projects dashboard event telemetry into the WPF shell with SAP B1 scaffolding.</summary>
/// <remarks>
/// Form Modes: Operates in Find/View to browse events; Add/Update are surfaced for consistency but the dashboard stream is read-only.
/// Audit &amp; Logging: Reads recent events from the shared database service without adding new audit entries.
/// Localization: Uses inline strings like `"Dashboard"` and severity labels until a resource file supplies keys.
/// Navigation: ModuleKey `Dashboard` registers the tab and allows Golden Arrow to route to `RelatedModule` targets surfaced in each `ModuleRecord`, with status strings updating the shell status bar during refresh.
/// </remarks>
public sealed class DashboardModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Shell registration key that binds Dashboard into the docking layout.</summary>
    /// <remarks>Execution: Resolved when the shell composes modules and persists layouts. Form Mode: Identifier applies across Find/Add/View/Update. Localization: Currently paired with the inline caption "Dashboard" until `Modules_Dashboard_Title` is introduced.</remarks>
    public new const string ModuleKey = "Dashboard";

    /// <summary>Initializes the Dashboard module view model with domain and shell services.</summary>
    /// <remarks>Execution: Invoked when the shell activates the module or Golden Arrow navigation materializes it. Form Mode: Seeds Find/View immediately while deferring Add/Update wiring to later transitions. Localization: Relies on inline strings for tab titles and prompts until module resources exist.</remarks>
    public DashboardModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Dashboard", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        SummarizeWithAiCommand = new RelayCommand(OpenAiSummary);
        Toolbar.Add(new ModuleToolbarCommand("Summarize (AI)", SummarizeWithAiCommand));
    }

    /// <summary>Opens the AI module to summarize dashboard events.</summary>
    public IRelayCommand SummarizeWithAiCommand { get; }

    /// <summary>Loads Dashboard records from domain services.</summary>
    /// <remarks>Execution: Triggered by Find refreshes and shell activation. Form Mode: Supplies data for Find/View while Add/Update reuse cached results. Localization: Emits inline status strings pending `Status_Dashboard_Loaded` resources.</remarks>
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var events = await Database.GetRecentDashboardEventsAsync(25).ConfigureAwait(false);
        SetProvenance("source: system_event_log, order: event_time desc, limit: 25");
        return events.Select(ToRecord).ToList();
    }

    /// <summary>Provides design-time sample data for the Dashboard designer experience.</summary>
    /// <remarks>Execution: Invoked only by design-mode checks to support Blend/preview tooling. Form Mode: Mirrors Find mode to preview list layouts. Localization: Sample literals remain inline for clarity.</remarks>
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var now = DateTime.Now;
        return new List<ModuleRecord>
        {
            new("evt-001", "Work order closed", "work_order_closed", "info", "Preventive maintenance completed",
                new[]
                {
                    new InspectorField("Timestamp", now.ToString("g", CultureInfo.CurrentCulture)),
                    new InspectorField("Module", "WorkOrders"),
                    new InspectorField("Severity", "info")
                },
                "WorkOrders", 101),
            new("evt-002", "CAPA escalated", "capa_escalated", "critical", "Deviation escalated to quality director",
                new[]
                {
                    new InspectorField("Timestamp", now.AddMinutes(-42).ToString("g", CultureInfo.CurrentCulture)),
                    new InspectorField("Module", "Quality"),
                    new InspectorField("Severity", "critical")
                },
                "Quality", 55)
        };
    }

    private static ModuleRecord ToRecord(DashboardEvent evt)
    {
        var fields = new List<InspectorField>
        {
            new("Timestamp", evt.Timestamp.ToString("g", CultureInfo.CurrentCulture)),
            new("Severity", evt.Severity),
            new("Module", evt.RelatedModule ?? "-"),
            new("Record Id", evt.RelatedRecordId?.ToString(CultureInfo.InvariantCulture) ?? "-")
        };

        if (!string.IsNullOrWhiteSpace(evt.Note))
        {
            fields.Add(new InspectorField("Notes", evt.Note));
        }

        var title = string.IsNullOrWhiteSpace(evt.Description) ? evt.EventType : evt.Description!;
        return new ModuleRecord(
            evt.Id.ToString(CultureInfo.InvariantCulture),
            title,
            evt.EventType,
            evt.Severity,
            evt.Description,
            fields,
            evt.RelatedModule,
            evt.RelatedRecordId);
    }

    private void OpenAiSummary()
    {
        var shell = YasGMP.Common.ServiceLocator.GetRequiredService<IShellInteractionService>();
        // Reuse AI module built-in audit summary
        var doc = shell.OpenModule(AiModuleViewModel.ModuleKey, "summary:audit");
        shell.Activate(doc);
    }
}


