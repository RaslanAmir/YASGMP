using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;
/// <summary>
/// Represents the Dashboard Module View Model.
/// </summary>

public sealed class DashboardModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>
    /// Represents the module key value.
    /// </summary>
    public const string ModuleKey = "Dashboard";
    /// <summary>
    /// Initializes a new instance of the DashboardModuleViewModel class.
    /// </summary>

    public DashboardModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization,
        ISignalRClientService signalRClient)
        : base(ModuleKey, localization.GetString("Module.Title.Dashboard"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _signalRClient = signalRClient ?? throw new ArgumentNullException(nameof(signalRClient));
        _signalRClient.AuditReceived += OnAuditReceived;
        _shellInteraction = shellInteraction ?? throw new ArgumentNullException(nameof(shellInteraction));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _inventoryAlerts = new ObservableCollection<InventoryAlertRow>();
        _inventoryMovements = new ObservableCollection<InventoryMovementRow>();
        _kpiWidgets = new ObservableCollection<KpiWidget>();
        _chartSeries = new ObservableCollection<DashboardChartSeries>();

        if (IsInDesignMode())
        {
            PopulateDesignTimeCollections();
        }
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        ResetDashboardSnapshot();

        var eventsTask = Database.GetRecentDashboardEventsAsync(25);
        var kpiTask = LoadKpiWidgetsAsync();
        var chartTask = LoadChartSeriesAsync();
        var inventoryTask = LoadInventoryInsightsAsync();

        await Task.WhenAll(kpiTask, chartTask, inventoryTask).ConfigureAwait(false);

        var events = await eventsTask.ConfigureAwait(false);
        return events.Select(ToRecord).ToList();
    }

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

    private readonly ISignalRClientService _signalRClient;
    private readonly IShellInteractionService _shellInteraction;
    private readonly ILocalizationService _localization;
    private readonly ObservableCollection<InventoryAlertRow> _inventoryAlerts;
    private readonly ObservableCollection<InventoryMovementRow> _inventoryMovements;
    private readonly ObservableCollection<KpiWidget> _kpiWidgets;
    private readonly ObservableCollection<DashboardChartSeries> _chartSeries;

    private string? _kpiError;
    private string? _chartError;
    private string? _inventoryAlertsError;
    private string? _inventoryMovementsError;

    private async void OnAuditReceived(object? sender, AuditEventArgs e)
    {
        try
        {
            await RefreshAsync().ConfigureAwait(false);
        }
        catch
        {
            // Best-effort refresh – real-time failures should not crash the dashboard.
        }
    }

    public ObservableCollection<InventoryAlertRow> InventoryAlerts => _inventoryAlerts;

    public ObservableCollection<InventoryMovementRow> InventoryMovements => _inventoryMovements;

    public ObservableCollection<KpiWidget> KpiWidgets => _kpiWidgets;

    public ObservableCollection<DashboardChartSeries> ChartSeries => _chartSeries;

    protected override string FormatLoadedStatus(int count)
    {
        var summary = _localization.GetString(
            "Dashboard.Status.Summary",
            Title,
            count,
            _kpiWidgets.Count,
            _chartSeries.Count,
            _inventoryAlerts.Count,
            _inventoryMovements.Count);

        var issues = new List<string>();
        if (!string.IsNullOrWhiteSpace(_kpiError))
        {
            issues.Add(_localization.GetString("Dashboard.Status.KpiError", _kpiError!));
        }

        if (!string.IsNullOrWhiteSpace(_chartError))
        {
            issues.Add(_localization.GetString("Dashboard.Status.ChartError", _chartError!));
        }

        if (!string.IsNullOrWhiteSpace(_inventoryAlertsError))
        {
            issues.Add(_localization.GetString("Dashboard.Status.InventoryAlertError", _inventoryAlertsError!));
        }

        if (!string.IsNullOrWhiteSpace(_inventoryMovementsError))
        {
            issues.Add(_localization.GetString("Dashboard.Status.InventoryMovementError", _inventoryMovementsError!));
        }

        if (issues.Count == 0)
        {
            return summary;
        }

        return string.Join(" | ", new[] { summary }.Concat(issues));
    }

    protected override Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        var summaryFields = CreateSummaryInspectorFields();
        if (record is null)
        {
            _shellInteraction.UpdateInspector(new InspectorContext(ModuleKey, Title, null, Title, summaryFields));
            return Task.CompletedTask;
        }

        var merged = record.InspectorFields.Concat(summaryFields).ToList();
        _shellInteraction.UpdateInspector(new InspectorContext(ModuleKey, Title, record.Key, record.Title, merged));
        return Task.CompletedTask;
    }

    private async Task LoadInventoryInsightsAsync()
    {
        DataTable? zoneTable = null;
        DataTable? movementTable = null;
        _inventoryAlertsError = null;
        _inventoryMovementsError = null;

        try
        {
            zoneTable = await Database.GetInventoryZoneDashboardAsync(25).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _inventoryAlertsError = ex.Message;
        }

        try
        {
            movementTable = await Database.GetInventoryMovementPreviewAsync(null, null, 15).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _inventoryMovementsError = ex.Message;
        }

        await RunOnUiThreadAsync(() =>
        {
            _inventoryAlerts.Clear();
            if (zoneTable is not null)
            {
                foreach (DataRow row in zoneTable.Rows)
                {
                    _inventoryAlerts.Add(new InventoryAlertRow(
                        SafeInt(row, "part_id"),
                        row["part_name"]?.ToString() ?? string.Empty,
                        row["part_code"]?.ToString() ?? string.Empty,
                        SafeInt(row, "warehouse_id"),
                        row["warehouse_name"]?.ToString() ?? string.Empty,
                        row["zone"]?.ToString() ?? string.Empty,
                        SafeInt(row, "quantity"),
                        SafeNullableInt(row, "min_threshold"),
                        SafeNullableInt(row, "max_threshold")));
                }
            }

            _inventoryMovements.Clear();
            if (movementTable is not null)
            {
                foreach (DataRow row in movementTable.Rows)
                {
                    _inventoryMovements.Add(new InventoryMovementRow(
                        SafeDate(row, "transaction_date"),
                        row["transaction_type"]?.ToString() ?? string.Empty,
                        SafeInt(row, "quantity"),
                        row.Table.Columns.Contains("related_document") ? row["related_document"]?.ToString() : null,
                        row.Table.Columns.Contains("note") ? row["note"]?.ToString() : null,
                        SafeNullableInt(row, "performed_by_id")));
                }
            }
        }).ConfigureAwait(false);
    }

    private async Task LoadKpiWidgetsAsync()
    {
        _kpiError = null;
        try
        {
            var widgets = await Database.GetKpiWidgetsAsync().ConfigureAwait(false) ?? new List<KpiWidget>();
            await RunOnUiThreadAsync(() =>
            {
                _kpiWidgets.Clear();
                foreach (var widget in widgets)
                {
                    _kpiWidgets.Add(widget);
                }
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _kpiError = ex.Message;
            await RunOnUiThreadAsync(() => _kpiWidgets.Clear()).ConfigureAwait(false);
        }
    }

    private async Task LoadChartSeriesAsync()
    {
        _chartError = null;
        try
        {
            var charts = await Database.GetDashboardChartsAsync().ConfigureAwait(false) ?? new List<ChartData>();
            var defaultGroup = _localization.GetString("Dashboard.Chart.Group.Default");
            var defaultSeries = _localization.GetString("Dashboard.Chart.Series.Default");

            var grouped = charts
                .GroupBy(c => new
                {
                    Group = string.IsNullOrWhiteSpace(c.Group) ? defaultGroup : c.Group!,
                    Series = string.IsNullOrWhiteSpace(c.Series) ? defaultSeries : c.Series!
                })
                .OrderBy(g => g.Key.Group, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(g => g.Key.Series, StringComparer.CurrentCultureIgnoreCase)
                .Select(g => new DashboardChartSeries(g.Key.Group, g.Key.Series, g.ToList()))
                .ToList();

            await RunOnUiThreadAsync(() =>
            {
                _chartSeries.Clear();
                foreach (var series in grouped)
                {
                    _chartSeries.Add(series);
                }
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _chartError = ex.Message;
            await RunOnUiThreadAsync(() => _chartSeries.Clear()).ConfigureAwait(false);
        }
    }

    private void ResetDashboardSnapshot()
    {
        _kpiError = null;
        _chartError = null;
        _inventoryAlertsError = null;
        _inventoryMovementsError = null;
    }

    private IReadOnlyList<InspectorField> CreateSummaryInspectorFields()
    {
        var fields = new List<InspectorField>
        {
            new(_localization.GetString("Dashboard.Inspector.KpiCount"), _kpiWidgets.Count.ToString(CultureInfo.CurrentCulture)),
            new(_localization.GetString("Dashboard.Inspector.ChartSeriesCount"), _chartSeries.Count.ToString(CultureInfo.CurrentCulture)),
            new(_localization.GetString("Dashboard.Inspector.InventoryAlertCount"), _inventoryAlerts.Count.ToString(CultureInfo.CurrentCulture)),
            new(_localization.GetString("Dashboard.Inspector.InventoryMovementCount"), _inventoryMovements.Count.ToString(CultureInfo.CurrentCulture))
        };

        if (!string.IsNullOrWhiteSpace(_kpiError))
        {
            fields.Add(new InspectorField(_localization.GetString("Dashboard.Inspector.KpiError"), _kpiError!));
        }

        if (!string.IsNullOrWhiteSpace(_chartError))
        {
            fields.Add(new InspectorField(_localization.GetString("Dashboard.Inspector.ChartError"), _chartError!));
        }

        if (!string.IsNullOrWhiteSpace(_inventoryAlertsError))
        {
            fields.Add(new InspectorField(_localization.GetString("Dashboard.Inspector.InventoryAlertError"), _inventoryAlertsError!));
        }

        if (!string.IsNullOrWhiteSpace(_inventoryMovementsError))
        {
            fields.Add(new InspectorField(_localization.GetString("Dashboard.Inspector.InventoryMovementError"), _inventoryMovementsError!));
        }

        return fields;
    }

    private void PopulateDesignTimeCollections()
    {
        _kpiWidgets.Clear();
        _kpiWidgets.Add(new KpiWidget
        {
            Title = "Open Work Orders",
            Value = 12,
            Unit = "WO",
            Trend = "up",
            ValueText = "12",
            Color = "#0d6efd",
            LastUpdated = DateTime.Now.AddMinutes(-5)
        });
        _kpiWidgets.Add(new KpiWidget
        {
            Title = "Critical Incidents",
            Value = 3,
            Trend = "up",
            IsAlert = true,
            Color = "#dc3545",
            ValueText = "3",
            LastUpdated = DateTime.Now.AddMinutes(-12)
        });
        _kpiWidgets.Add(new KpiWidget
        {
            Title = "Compliance Score",
            Value = 98.7m,
            Unit = "%",
            Trend = "neutral",
            Color = "#20c997",
            ValueText = "98.7%",
            LastUpdated = DateTime.Now.AddMinutes(-30)
        });

        _chartSeries.Clear();
        var today = DateTime.Today;
        var openedPoints = new List<ChartData>
        {
            new() { Label = today.AddDays(-2).ToString("MMM dd", CultureInfo.CurrentCulture), Value = 4 },
            new() { Label = today.AddDays(-1).ToString("MMM dd", CultureInfo.CurrentCulture), Value = 6 },
            new() { Label = today.ToString("MMM dd", CultureInfo.CurrentCulture), Value = 5 }
        };

        var closedPoints = new List<ChartData>
        {
            new() { Label = today.AddDays(-2).ToString("MMM dd", CultureInfo.CurrentCulture), Value = 2 },
            new() { Label = today.AddDays(-1).ToString("MMM dd", CultureInfo.CurrentCulture), Value = 5 },
            new() { Label = today.ToString("MMM dd", CultureInfo.CurrentCulture), Value = 7 }
        };

        _chartSeries.Add(new DashboardChartSeries("Work Orders", "Opened", openedPoints));
        _chartSeries.Add(new DashboardChartSeries("Work Orders", "Closed", closedPoints));

        if (_inventoryAlerts.Count == 0)
        {
            _inventoryAlerts.Add(new InventoryAlertRow(101, "HEPA Filter", "HF-01", 5, "Main Warehouse", "Zone A", 2, 3, 10));
            _inventoryAlerts.Add(new InventoryAlertRow(102, "Calibration Kit", "CK-02", 5, "Main Warehouse", "Zone C", 12, 5, 20));
        }

        if (_inventoryMovements.Count == 0)
        {
            _inventoryMovements.Add(new InventoryMovementRow(DateTime.Now.AddHours(-2), "Issue", 4, "WO-2001", "Used for line setup", 21));
            _inventoryMovements.Add(new InventoryMovementRow(DateTime.Now.AddHours(-1), "Receipt", 10, "PO-8874", "Vendor delivery", 18));
        }

        StatusMessage = _localization.GetString(
            "Dashboard.Status.Summary",
            Title,
            2,
            _kpiWidgets.Count,
            _chartSeries.Count,
            _inventoryAlerts.Count,
            _inventoryMovements.Count);
    }

    private static bool IsInDesignMode()
        => DesignerProperties.GetIsInDesignMode(new DependencyObject());

    private static Task RunOnUiThreadAsync(Action action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(action, DispatcherPriority.DataBind).Task;
    }

    private static int SafeInt(DataRow row, string column)
    {
        if (row.Table.Columns.Contains(column) && row[column] != DBNull.Value)
        {
            return Convert.ToInt32(row[column], CultureInfo.InvariantCulture);
        }

        return 0;
    }

    private static int? SafeNullableInt(DataRow row, string column)
    {
        if (row.Table.Columns.Contains(column) && row[column] != DBNull.Value)
        {
            return Convert.ToInt32(row[column], CultureInfo.InvariantCulture);
        }

        return null;
    }

    private static DateTime SafeDate(DataRow row, string column)
    {
        if (row.Table.Columns.Contains(column) && row[column] != DBNull.Value)
        {
            if (row[column] is DateTime dt)
            {
                return dt;
            }

            if (DateTime.TryParse(row[column]?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                return parsed;
            }
        }

        return DateTime.UtcNow;
    }

    public sealed record InventoryAlertRow(
        int PartId,
        string PartName,
        string PartCode,
        int WarehouseId,
        string WarehouseName,
        string Zone,
        int Quantity,
        int? Minimum,
        int? Maximum);

    public sealed record InventoryMovementRow(
        DateTime Timestamp,
        string Type,
        int Quantity,
        string? Document,
        string? Note,
        int? PerformedById);
}

public sealed class DashboardChartSeries
{
    public DashboardChartSeries(string group, string series, IEnumerable<ChartData> points)
    {
        Group = string.IsNullOrWhiteSpace(group) ? string.Empty : group;
        Series = string.IsNullOrWhiteSpace(series) ? string.Empty : series;
        Points = new ObservableCollection<ChartData>(points ?? Enumerable.Empty<ChartData>());
    }

    public string Group { get; }

    public string Series { get; }

    public ObservableCollection<ChartData> Points { get; }

    public string Header
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Group))
            {
                return Series;
            }

            if (string.IsNullOrWhiteSpace(Series))
            {
                return Group;
            }

            return string.Format(CultureInfo.CurrentCulture, "{0} – {1}", Group, Series);
        }
    }
}
