using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Surfaces administration settings in the WPF shell using the shared SAP B1 document pattern.</summary>
/// <remarks>
/// Form Modes: Supports Find/View for configuration search; Add/Update toggles exist for parity but persist actions are currently read-only.
/// Audit &amp; Logging: This module does not call into <see cref="AuditService"/> directly and instead defers to database-level history for any change tracking.
/// Localization: Consumes inline strings such as `"Administration"` and inspector labels (e.g. `"Category"`, `"Value"`); no resource keys are wired yet.
/// Navigation: Registers ModuleKey `Admin` with the shell so status strings populate the status bar and Golden Arrow or CFL navigation can route back to the settings list when other modules reference the key.
/// </remarks>
public sealed class AdminModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Shell registration key that binds Administration into the docking layout.</summary>
    /// <remarks>Execution: Resolved when the shell composes modules and persists layouts. Form Mode: Identifier applies across Find/Add/View/Update. Localization: Currently paired with the inline caption "Administration" until `Modules_Admin_Title` is introduced.</remarks>
    public new const string ModuleKey = "Admin";

    /// <summary>Initializes the Administration module view model with domain and shell services.</summary>
    /// <remarks>Execution: Invoked when the shell activates the module or Golden Arrow navigation materializes it. Form Mode: Seeds Find/View immediately while deferring Add/Update wiring to later transitions. Localization: Relies on inline strings for tab titles and prompts until module resources exist.</remarks>
    public AdminModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Administration", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        SummarizeWithAiCommand = new RelayCommand(OpenAiSummary);
        Toolbar.Add(new ModuleToolbarCommand("Summarize (AI)", SummarizeWithAiCommand));
    }

    /// <summary>Opens the AI module to summarize admin settings or the selected setting.</summary>
    public IRelayCommand SummarizeWithAiCommand { get; }

    /// <summary>Loads Administration records from domain services.</summary>
    /// <remarks>Execution: Triggered by Find refreshes and shell activation. Form Mode: Supplies data for Find/View while Add/Update reuse cached results. Localization: Emits inline status strings pending `Status_Admin_Loaded` resources.</remarks>
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var result = await Database.GetAllSettingsWithProvenanceAsync();
        SetProvenance($"source: {result.Source}, order: {result.OrderBy}");
        return result.Items.Select(ToRecord).ToList();
    }

    /// <summary>Provides design-time sample data for the Administration designer experience.</summary>
    /// <remarks>Execution: Invoked only by design-mode checks to support Blend/preview tooling. Form Mode: Mirrors Find mode to preview list layouts. Localization: Sample literals remain inline for clarity.</remarks>
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
            new("Updated", setting.UpdatedAt.ToString("g")),
        };

        return new ModuleRecord(
            setting.Id.ToString(),
            setting.Key,
            setting.Key,
            setting.Status ?? "active",
            setting.Description,
            fields,
            null,
            null);
    }

    private void OpenAiSummary()
    {
        string prompt;
        if (SelectedRecord is null)
        {
            // Summarize overall settings snapshot
            prompt = "Summarize current administration settings, highlighting risky defaults and recommended changes in <= 8 bullets.";
        }
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Key={SelectedRecord.Title}");
            foreach (var f in SelectedRecord.InspectorFields)
            {
                sb.AppendLine($"{f.Label}={f.Value}");
            }
            prompt = $"Summarize this setting in <= 8 bullets and suggest safe defaults if applicable.\n{sb}";
        }

        var shell = YasGMP.Common.ServiceLocator.GetRequiredService<IShellInteractionService>();
        var doc = shell.OpenModule(AiModuleViewModel.ModuleKey, $"prompt:{prompt}");
        shell.Activate(doc);
    }
}


