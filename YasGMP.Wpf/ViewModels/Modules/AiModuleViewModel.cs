using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.AppCore.Services.Ai;
using YasGMP.Services;
using YasGMP.Wpf.Services;
using YasGMP.Models;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Docked AI assistant module that can receive context from other modules and
/// trigger one-click summaries of Audit and Work Orders.
/// </summary>
public sealed partial class AiModuleViewModel : ModuleDocumentViewModel
{
    public new const string ModuleKey = "AI";

    private readonly IAiAssistantService _ai;
    private readonly DatabaseService _db;

    [ObservableProperty]
    private string _prompt = string.Empty;

    [ObservableProperty]
    private string _response = string.Empty;

    public IAsyncRelayCommand AskCommand { get; }
    public IAsyncRelayCommand SummarizeAuditCommand { get; }
    public IAsyncRelayCommand SummarizeWorkOrdersCommand { get; }

    public AiModuleViewModel(
        IAiAssistantService ai,
        DatabaseService database,
        ICflDialogService cfl,
        IShellInteractionService shell,
        IModuleNavigationService nav)
        : base(ModuleKey, "AI Assistant", cfl, shell, nav)
    {
        _ai = ai ?? throw new ArgumentNullException(nameof(ai));
        _db = database ?? throw new ArgumentNullException(nameof(database));

        AskCommand = new AsyncRelayCommand(AskAsync, CanAsk);
        SummarizeAuditCommand = new AsyncRelayCommand(SummarizeAuditAsync, () => !IsBusy);
        SummarizeWorkOrdersCommand = new AsyncRelayCommand(SummarizeWorkOrdersAsync, () => !IsBusy);

        Toolbar.Add(new ModuleToolbarCommand("Summarize Audit", SummarizeAuditCommand));
        Toolbar.Add(new ModuleToolbarCommand("Summarize Work Orders", SummarizeWorkOrdersCommand));
    }

    protected override Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        // AI module uses a simple single-record surface; nothing to preload.
        var record = new ModuleRecord("ai", "AI", code: null, status: null, description: "Ask the assistant or run a one-click summary.", inspectorFields: Array.Empty<InspectorField>(), null, null);
        if (parameter is string text)
        {
            if (string.Equals(text, "summary:audit", StringComparison.OrdinalIgnoreCase))
            {
                _ = SummarizeAuditAsync();
            }
            else if (string.Equals(text, "summary:workorders", StringComparison.OrdinalIgnoreCase))
            {
                _ = SummarizeWorkOrdersAsync();
            }
            else if (text.StartsWith("prompt:", StringComparison.OrdinalIgnoreCase))
            {
                Prompt = text.Substring("prompt:".Length);
                _ = AskAsync();
            }
        }
        return Task.FromResult<IReadOnlyList<ModuleRecord>>(new[] { record });
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new[] { new ModuleRecord("ai", "AI", null, null, "Design-time AI surface.", Array.Empty<InspectorField>(), null, null) };

    private bool CanAsk() => !IsBusy && !string.IsNullOrWhiteSpace(Prompt);

    private async Task AskAsync()
    {
        if (!CanAsk()) return;
        try
        {
            IsBusy = true;
            var reply = await _ai.ChatAsync(Prompt, systemPrompt: "You are YasGMP's AI. Be concise and actionable.").ConfigureAwait(false);
            Response = reply;
            StatusMessage = "AI: response received.";
            Prompt = string.Empty;
        }
        catch (Exception ex)
        {
            Response = $"AI error: {ex.Message}";
            StatusMessage = Response;
        }
        finally
        {
            IsBusy = false;
            (AskCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private async Task SummarizeAuditAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Summarizing audit...";
            var events = await _db.GetRecentDashboardEventsAsync(50).ConfigureAwait(false);
            var text = BuildAuditSummaryPrompt(events);
            Response = await _ai.ChatAsync(text, systemPrompt: "Summarize audit events as bullet points with counts and notable anomalies. Keep it under 10 bullets.").ConfigureAwait(false);
            StatusMessage = "Audit summary ready.";
        }
        catch (Exception ex)
        {
            Response = $"Audit summary failed: {ex.Message}";
            StatusMessage = Response;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SummarizeWorkOrdersAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Summarizing work orders...";
            var all = await _db.GetAllWorkOrdersFullAsync().ConfigureAwait(false);
            var recent = all.OrderByDescending(w => w.DateOpen).Take(25).ToList();
            var text = BuildWorkOrdersPrompt(recent);
            Response = await _ai.ChatAsync(text, systemPrompt: "Generate an operational summary of work orders (status counts, overdue, risks). Use plain text bullets under 10 items.").ConfigureAwait(false);
            StatusMessage = "Work Orders summary ready.";
        }
        catch (Exception ex)
        {
            Response = $"Work Orders summary failed: {ex.Message}";
            StatusMessage = Response;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string BuildAuditSummaryPrompt(IReadOnlyList<DashboardEvent> events)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Context: These are system events.");
        foreach (var e in events)
        {
            sb.AppendLine($"- {e.Timestamp:O} | {e.Severity} | {e.EventType} | {e.Description}");
        }
        sb.AppendLine();
        sb.AppendLine("Task: Summarize key trends and anomalies in <= 10 bullets.");
        return sb.ToString();
    }

    private static string BuildWorkOrdersPrompt(IEnumerable<YasGMP.Models.WorkOrder> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Context: Recent work orders (title, status, priority, dates).");
        foreach (var w in items)
        {
            sb.AppendLine($"- #{w.Id} | {w.Title} | status={w.Status} | prio={w.Priority} | open={w.DateOpen:yyyy-MM-dd} | due={w.DueDate:yyyy-MM-dd} | close={(w.DateClose?.ToString("yyyy-MM-dd") ?? "-")}");
        }
        sb.AppendLine();
        sb.AppendLine("Task: Summarize operations, flag overdue/high-priority risks, counts by status, <= 10 bullets.");
        return sb.ToString();
    }
}
