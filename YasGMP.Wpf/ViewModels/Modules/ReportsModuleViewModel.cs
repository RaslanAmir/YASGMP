using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Minimal Reports document that lists available reports and provides a Summarize (AI) action.
/// Uses the common SAP B1 document pattern and can be extended to execute/export in the future.
/// </summary>
public sealed class ReportsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public new const string ModuleKey = "Reports";

    public ReportsModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Reports", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        SummarizeWithAiCommand = new RelayCommand(OpenAiSummary);
        Toolbar.Add(new ModuleToolbarCommand("Summarize (AI)", SummarizeWithAiCommand));
        RunReportCommand = new AsyncRelayCommand(RunReportAsync, () => !IsBusy);
        ExportReportCommand = new AsyncRelayCommand(ExportReportAsync, () => !IsBusy);
        Toolbar.Add(new ModuleToolbarCommand("Run", RunReportCommand));
        Toolbar.Add(new ModuleToolbarCommand("Export", ExportReportCommand));
    }

    /// <summary>Summarizes the selected report or overall report set in the AI module.</summary>
    public IRelayCommand SummarizeWithAiCommand { get; }
    public IAsyncRelayCommand RunReportCommand { get; }
    public IAsyncRelayCommand ExportReportCommand { get; }

    // Simple parameters (persisted)
    public DateTime? FromDate { get => _fromDate; set { _fromDate = value; OnPropertyChanged(); } }
    public DateTime? ToDate { get => _toDate; set { _toDate = value; OnPropertyChanged(); } }
    private DateTime? _fromDate = DateTime.Today.AddDays(-30);
    private DateTime? _toDate = DateTime.Today;

    public IRelayCommand SavePresetCommand => _savePreset ??= new RelayCommand(SavePreset);
    public IRelayCommand LoadPresetCommand => _loadPreset ??= new RelayCommand(LoadPreset);
    private RelayCommand? _savePreset;
    private RelayCommand? _loadPreset;

    public int ExportProgress
    {
        get => _exportProgress;
        private set { _exportProgress = value; OnPropertyChanged(); }
    }
    private int _exportProgress;

    public IRelayCommand CancelExportCommand => _cancelExport ??= new RelayCommand(CancelExport, () => _cts != null);
    private RelayCommand? _cancelExport;
    private System.Threading.CancellationTokenSource? _cts;

    protected override Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
        => Task.FromResult(CreateSampleRecords());

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => CreateSampleRecords();

    private static IReadOnlyList<ModuleRecord> CreateSampleRecords()
    {
        var now = DateTime.Now;
        var list = new List<ModuleRecord>
        {
            new(
                key: "RPT-001",
                title: "Calibration Due (30 days)",
                code: "calibration_due_30",
                status: "ready",
                description: "Shows instruments due for calibration within 30 days",
                inspectorFields: new[]
                {
                    new InspectorField("Category", "Quality"),
                    new InspectorField("Last Run", now.AddDays(-1).ToString("g", CultureInfo.CurrentCulture)),
                    new InspectorField("Owner", "QA")
                },
                relatedModuleKey: CalibrationModuleViewModel.ModuleKey,
                relatedParameter: null),
            new(
                key: "RPT-002",
                title: "Warehouse Stock Alerts",
                code: "warehouse_stock_alerts",
                status: "ready",
                description: "Low stock and negative balances across warehouses",
                inspectorFields: new[]
                {
                    new InspectorField("Category", "Supply Chain"),
                    new InspectorField("Last Run", now.AddHours(-6).ToString("g", CultureInfo.CurrentCulture)),
                    new InspectorField("Owner", "Maintenance")
                },
                relatedModuleKey: WarehouseModuleViewModel.ModuleKey,
                relatedParameter: null),
            new(
                key: "RPT-003",
                title: "Work Orders Overdue",
                code: "work_orders_overdue",
                status: "ready",
                description: "Open work orders past their due date",
                inspectorFields: new[]
                {
                    new InspectorField("Category", "Maintenance"),
                    new InspectorField("Owner", "Maintenance")
                },
                relatedModuleKey: WorkOrdersModuleViewModel.ModuleKey,
                relatedParameter: null),
            new(
                key: "RPT-004",
                title: "Supplier Risk Ranking",
                code: "supplier_risk",
                status: "ready",
                description: "Rank suppliers by risk level",
                inspectorFields: new[]
                {
                    new InspectorField("Category", "Supply Chain"),
                    new InspectorField("Owner", "QA")
                },
                relatedModuleKey: SuppliersModuleViewModel.ModuleKey,
                relatedParameter: null),
            new(
                key: "RPT-005",
                title: "Incident Trends",
                code: "incident_trends",
                status: "ready",
                description: "Counts by type and priority between dates",
                inspectorFields: new[]
                {
                    new InspectorField("Category", "Quality"),
                    new InspectorField("Owner", "QA")
                },
                relatedModuleKey: IncidentsModuleViewModel.ModuleKey,
                relatedParameter: null),
            new(
                key: "RPT-006",
                title: "CAPA Cycle Times",
                code: "capa_cycle_times",
                status: "ready",
                description: "Days from open to close; mean/median",
                inspectorFields: new[]
                {
                    new InspectorField("Category", "Quality"),
                    new InspectorField("Owner", "QA")
                },
                relatedModuleKey: CapaModuleViewModel.ModuleKey,
                relatedParameter: null),
            new(
                key: "RPT-007",
                title: "Validation Due Calendar",
                code: "validation_due_calendar",
                status: "ready",
                description: "Upcoming validations (NextDue) as CSV",
                inspectorFields: new[]
                {
                    new InspectorField("Category", "Quality"),
                    new InspectorField("Owner", "QA")
                },
                relatedModuleKey: ValidationsModuleViewModel.ModuleKey,
                relatedParameter: null)
        };
        return list;
    }

    private void OpenAiSummary()
    {
        string prompt;
        if (SelectedRecord is null)
        {
            prompt = "Summarize available reports and suggest the top 3 most useful for daily operations in <= 8 bullets.";
        }
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Title={SelectedRecord.Title}");
            if (!string.IsNullOrWhiteSpace(SelectedRecord.Description))
            {
                sb.AppendLine($"Description={SelectedRecord.Description}");
            }
            foreach (var f in SelectedRecord.InspectorFields)
            {
                sb.AppendLine($"{f.Label}={f.Value}");
            }
            prompt = $"Summarize this report in <= 8 bullets: purpose, key filters, and who should run it.\\n{sb}";
        }

        var shell = YasGMP.Common.ServiceLocator.GetRequiredService<IShellInteractionService>();
        var doc = shell.OpenModule(AiModuleViewModel.ModuleKey, $"prompt:{prompt}");
        shell.Activate(doc);
    }

    private async Task RunReportAsync()
    {
        if (SelectedRecord is null)
        {
            StatusMessage = "Select a report to run.";
            return;
        }

        try
        {
            _cts = new System.Threading.CancellationTokenSource();
            IsBusy = true;
            StatusMessage = $"Running '{SelectedRecord.Title}'...";

            var code = (SelectedRecord.Code ?? SelectedRecord.Title).Trim().ToLowerInvariant();
            var progress = new System.Progress<int>(p => ExportProgress = p);
            ExportProgress = 0;
            string path = await RunProviderAsync(code, _cts.Token, progress).ConfigureAwait(false);

            TryOpen(path);
            StatusMessage = $"Report output saved: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Report run failed: {ex.Message}";
            throw;
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
            (CancelExportCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private async Task ExportReportAsync()
    {
        // Export to Excel/PDF where supported by ExportService; fallback to CSV
        if (SelectedRecord is null)
        {
            StatusMessage = "Select a report to export.";
            return;
        }

        try
        {
            _cts = new System.Threading.CancellationTokenSource();
            IsBusy = true;
            ExportProgress = 0;
            var code = (SelectedRecord.Code ?? SelectedRecord.Title).Trim().ToLowerInvariant();
            var progress = new System.Progress<int>(p => ExportProgress = p);
            string path = await ExportProviderAsync(code, _cts.Token, progress).ConfigureAwait(false);
            TryOpen(path);
            StatusMessage = $"Report exported: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
        finally { IsBusy = false; _cts?.Dispose(); _cts = null; (CancelExportCommand as RelayCommand)?.NotifyCanExecuteChanged(); }
    }

    private void CancelExport()
    {
        try { _cts?.Cancel(); }
        catch { /* ignore */ }
        finally { (CancelExportCommand as RelayCommand)?.NotifyCanExecuteChanged(); }
    }

    private static string Escape(string? s)
    {
        s ??= string.Empty;
        if (s.Contains(',') || s.Contains('"'))
        {
            return '"' + s.Replace("\"", "\"\"") + '"';
        }
        return s;
    }

    private string PresetPath
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = System.IO.Path.Combine(appData, "YasGMP", "reports");
            System.IO.Directory.CreateDirectory(dir);
            return System.IO.Path.Combine(dir, "preset.json");
        }
    }

    private void SavePreset()
    {
        var preset = new { from = FromDate, to = ToDate };
        var json = System.Text.Json.JsonSerializer.Serialize(preset);
        System.IO.File.WriteAllText(PresetPath, json);
        StatusMessage = "Preset saved.";
    }

    private void LoadPreset()
    {
        try
        {
            if (!System.IO.File.Exists(PresetPath)) { StatusMessage = "No preset found."; return; }
            var json = System.IO.File.ReadAllText(PresetPath);
            var preset = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
            if (preset.TryGetProperty("from", out var f) && f.ValueKind == System.Text.Json.JsonValueKind.String && DateTime.TryParse(f.GetString(), out var from))
                FromDate = from;
            if (preset.TryGetProperty("to", out var t) && t.ValueKind == System.Text.Json.JsonValueKind.String && DateTime.TryParse(t.GetString(), out var to))
                ToDate = to;
            StatusMessage = "Preset loaded.";
        }
        catch { StatusMessage = "Failed to load preset."; }
    }

    private async Task<string> RunProviderAsync(string code, System.Threading.CancellationToken token, System.IProgress<int>? progress)
    {
        // Return a CSV path by default; dedicated providers may use different formats
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = System.IO.Path.Combine(appData, "YasGMP", "reports");
        System.IO.Directory.CreateDirectory(dir);
        string fileBase = $"{code}_{DateTime.UtcNow:yyyyMMdd_HHmm}";
        string path = System.IO.Path.Combine(dir, fileBase + ".csv");

        if (code == "calibration_due_30")
        {
            var list = await Database.GetAllCalibrationsAsync().ConfigureAwait(false);
            var now = DateTime.Today;
            var max = now.AddDays(30);
            var due = list.Where(c => c.NextDue != default && c.NextDue <= max && c.NextDue >= now).ToList();
            var export = YasGMP.Common.ServiceLocator.GetRequiredService<ExportService>();
            // Use Excel by default for calibrations
            return await export.ExportToExcelAsync(due, token: token, progress: progress).ConfigureAwait(false);
        }
        if (code == "warehouse_stock_alerts")
        {
            var parts = await Database.GetAllPartsAsync().ConfigureAwait(false);
            var low = parts.Where(p => p.IsWarehouseStockCritical || (p.MinStockAlert.HasValue && p.Stock < p.MinStockAlert.Value)).ToList();
            // Write CSV for now (DB has ExportSparePartsAsync, but requires actor context)
            using var sw = new System.IO.StreamWriter(path, false, System.Text.Encoding.UTF8);
            await sw.WriteLineAsync("id,code,name,stock,min,wh_summary").ConfigureAwait(false);
            int total = Math.Max(1, low.Count);
            for (int idx = 0; idx < low.Count; idx++)
            {
                token.ThrowIfCancellationRequested();
                var p = low[idx];
                var line = string.Join(',', p.Id, Escape(p.Code), Escape(p.Name), (p.Stock?.ToString(CultureInfo.InvariantCulture) ?? string.Empty), (p.MinStockAlert?.ToString() ?? string.Empty), Escape(p.WarehouseSummary));
                await sw.WriteLineAsync(line).ConfigureAwait(false);
                progress?.Report((int)((idx + 1) * 100.0 / total));
            }
            return path;
        }

        if (code == "work_orders_overdue")
        {
            var orders = await Database.GetAllWorkOrdersFullAsync().ConfigureAwait(false);
            var today = DateTime.Today;
            var overdue = orders.Where(w => (w.DateClose == null || w.DateClose == default) && w.DueDate != null && w.DueDate < today).ToList();
            using var sw = new System.IO.StreamWriter(path, false, System.Text.Encoding.UTF8);
            await sw.WriteLineAsync("id,title,status,priority,open,due,machine").ConfigureAwait(false);
            int totalWo = Math.Max(1, overdue.Count);
            for (int idx = 0; idx < overdue.Count; idx++)
            {
                token.ThrowIfCancellationRequested();
                var w = overdue[idx];
                var line = string.Join(',', w.Id, Escape(w.Title), Escape(w.Status), Escape(w.Priority), w.DateOpen.ToString("yyyy-MM-dd"), w.DueDate?.ToString("yyyy-MM-dd"), Escape(w.Machine?.Name));
                await sw.WriteLineAsync(line).ConfigureAwait(false);
                progress?.Report((int)((idx + 1) * 100.0 / totalWo));
            }
            return path;
        }

        if (code == "supplier_risk")
        {
            var suppliers = await Database.GetAllSuppliersAsync().ConfigureAwait(false);
            var ordered = suppliers.OrderByDescending(s => s.RiskLevel).ToList();
            using var sw = new System.IO.StreamWriter(path, false, System.Text.Encoding.UTF8);
            await sw.WriteLineAsync("id,name,type,status,risk,country").ConfigureAwait(false);
            int totalSup = Math.Max(1, ordered.Count);
            for (int idx = 0; idx < ordered.Count; idx++)
            {
                token.ThrowIfCancellationRequested();
                var s = ordered[idx];
                var line = string.Join(',', s.Id, Escape(s.Name), Escape(s.SupplierType), Escape(s.Status), Escape(s.RiskLevel), Escape(s.Country));
                await sw.WriteLineAsync(line).ConfigureAwait(false);
                progress?.Report((int)((idx + 1) * 100.0 / totalSup));
            }
            return path;
        }

        if (code == "incident_trends")
        {
            var incidents = await Database.GetAllIncidentsAsync().ConfigureAwait(false);
            var from = FromDate ?? DateTime.Today.AddDays(-30);
            var to = (ToDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);
            var range = incidents.Where(i => i.DetectedAt >= from && i.DetectedAt <= to).ToList();
            var grouped = range.GroupBy(i => new { Type = i.Type, Priority = i.Priority })
                               .Select(g => new { g.Key.Type, g.Key.Priority, Count = g.Count() })
                               .OrderByDescending(x => x.Count).ToList();
            using var sw = new System.IO.StreamWriter(path, false, System.Text.Encoding.UTF8);
            await sw.WriteLineAsync("type,priority,count").ConfigureAwait(false);
            int totalInc = Math.Max(1, grouped.Count);
            for (int idx = 0; idx < grouped.Count; idx++)
            {
                token.ThrowIfCancellationRequested();
                var row = grouped[idx];
                var line = string.Join(',', Escape(row.Type), Escape(row.Priority), row.Count);
                await sw.WriteLineAsync(line).ConfigureAwait(false);
                progress?.Report((int)((idx + 1) * 100.0 / totalInc));
            }
            return path;
        }

        // Fallback: dashboard CSV
        var charts = await Database.GetDashboardChartsAsync("last30").ConfigureAwait(false);
        using (var sw = new System.IO.StreamWriter(path, false, System.Text.Encoding.UTF8))
        {
            await sw.WriteLineAsync("label,series,group,value,secondary,note").ConfigureAwait(false);
            int total = Math.Max(1, charts.Count);
            for (int idx = 0; idx < charts.Count; idx++)
            {
                token.ThrowIfCancellationRequested();
                var c = charts[idx];
                var line = string.Join(',', Escape(c.Label), Escape(c.Series), Escape(c.Group), c.Value.ToString(CultureInfo.InvariantCulture), (c.SecondaryValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty), Escape(c.Note));
                await sw.WriteLineAsync(line).ConfigureAwait(false);
                progress?.Report((int)((idx + 1) * 100.0 / total));
            }
        }
        return path;
    }

    private async Task<string> ExportProviderAsync(string code, System.Threading.CancellationToken token, System.IProgress<int>? progress)
    {
        if (code == "calibration_due_30")
        {
            var list = await Database.GetAllCalibrationsAsync().ConfigureAwait(false);
            var now = DateTime.Today;
            var max = now.AddDays(30);
            var due = list.Where(c => c.NextDue != default && c.NextDue <= max && c.NextDue >= now).ToList();
            var export = YasGMP.Common.ServiceLocator.GetRequiredService<ExportService>();
            // Prefer PDF export for sharing; Excel also available
            return await export.ExportToPdfAsync(due, token: token, progress: progress).ConfigureAwait(false);
        }
        if (code == "warehouse_stock_alerts")
        {
            var parts = await Database.GetAllPartsAsync().ConfigureAwait(false);
            var low = parts.Where(p => p.IsWarehouseStockCritical || (p.MinStockAlert.HasValue && p.Stock < p.MinStockAlert.Value)).ToList();
            var export = YasGMP.Common.ServiceLocator.GetRequiredService<ExportService>();
            return await export.ExportPartsAsync(low, "xlsx", filterUsed: "stock_alerts", token: token, progress: progress).ConfigureAwait(false);
        }

        if (code == "work_orders_overdue")
        {
            var orders = await Database.GetAllWorkOrdersFullAsync().ConfigureAwait(false);
            var today = DateTime.Today;
            var overdue = orders.Where(w => (w.DateClose == null || w.DateClose == default) && w.DueDate != null && w.DueDate < today).ToList();
            // CSV export fallback for work orders
            return await RunProviderAsync(code, token, progress).ConfigureAwait(false);
        }

        if (code == "supplier_risk")
        {
            var suppliers = await Database.GetAllSuppliersAsync().ConfigureAwait(false);
            // Excel export via Csv fallback for now
            return await RunProviderAsync(code, token, progress).ConfigureAwait(false);
        }

        if (code == "incident_trends")
        {
            return await RunProviderAsync(code, token, progress).ConfigureAwait(false);
        }

        // Fallback to CSV
        return await RunProviderAsync(code, token, progress).ConfigureAwait(false);
    }

    private static void TryOpen(string path)
    {
        try
        {
            if (System.IO.File.Exists(path))
            {
                var psi = new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true };
                System.Diagnostics.Process.Start(psi);
            }
        }
        catch { }
    }
}
