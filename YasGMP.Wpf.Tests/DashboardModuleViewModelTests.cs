using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class DashboardModuleViewModelTests
{
    [Fact]
    public async Task InitializeAsync_LoadsInventoryCollectionsFromDatabase()
    {
        var database = new DatabaseService();
        database.DashboardEvents.Add(new DashboardEvent
        {
            Id = 1,
            EventType = "work_order_closed",
            Description = "WO closed",
            Severity = "info",
            Timestamp = DateTime.UtcNow.AddMinutes(-5),
            RelatedModule = "WorkOrders",
            RelatedRecordId = 100
        });
        database.DashboardEvents.Add(new DashboardEvent
        {
            Id = 2,
            EventType = "capa_escalated",
            Description = "CAPA escalated",
            Severity = "critical",
            Timestamp = DateTime.UtcNow,
            RelatedModule = "Quality",
            RelatedRecordId = 200
        });

        database.InventoryZoneDashboardTable = CreateZoneTable();
        database.InventoryMovementPreviewTable = CreateMovementTable();

        var localization = CreateLocalization();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var realtime = new StubSignalRClientService();

        var viewModel = new DashboardModuleViewModel(database, auditService, cfl, shell, navigation, localization, realtime);

        await viewModel.InitializeAsync(null);

        Assert.Equal(2, viewModel.Records.Count);

        Assert.Collection(
            viewModel.InventoryAlerts,
            alert =>
            {
                Assert.Equal("Critical", alert.Zone);
                Assert.Equal("Main", alert.WarehouseName);
                Assert.Equal(5, alert.Quantity);
                Assert.Equal(10, alert.Minimum);
                Assert.Null(alert.Maximum);
            },
            alert =>
            {
                Assert.Equal("Warning", alert.Zone);
                Assert.Equal("Overflow", alert.WarehouseName);
                Assert.Equal(210, alert.Quantity);
                Assert.Equal(100, alert.Maximum);
            });

        Assert.Collection(
            viewModel.InventoryMovements,
            movement =>
            {
                Assert.Equal("in", movement.Type);
                Assert.Equal(25, movement.Quantity);
                Assert.Equal("PO-15", movement.Document);
                Assert.Equal(7, movement.PerformedById);
            },
            movement =>
            {
                Assert.Equal("out", movement.Type);
                Assert.Equal(-4, movement.Quantity);
                Assert.Equal("WO-42", movement.Document);
                Assert.Equal("Issued", movement.Note);
            });

        Assert.Equal("Dashboard: inventory insights refreshed (2 alert(s), 2 movement(s)).", viewModel.StatusMessage);
    }

    [Fact]
    public async Task InitializeAsync_PopulatesKpisAndChartsFromDatabase()
    {
        var database = new DatabaseService();
        database.DashboardEvents.Add(new DashboardEvent
        {
            Id = 11,
            EventType = "incident_logged",
            Description = "New incident logged",
            Severity = "info",
            Timestamp = DateTime.UtcNow
        });

        database.KpiWidgets.Add(new KpiWidget
        {
            Title = "Open Work Orders",
            Value = 5,
            Trend = "up",
            Color = "#0d6efd",
            LastUpdated = DateTime.UtcNow
        });
        database.KpiWidgets.Add(new KpiWidget
        {
            Title = "Overdue CAPAs",
            Value = 2,
            Trend = "down",
            Color = "#dc3545",
            IsAlert = true,
            LastUpdated = DateTime.UtcNow
        });

        database.DashboardCharts.Add(new ChartData
        {
            Group = "WorkOrders",
            Series = "Opened",
            Label = "2026-08-01",
            Value = 3,
            Timestamp = DateTime.UtcNow.AddDays(-2),
            Color = "#0d6efd"
        });
        database.DashboardCharts.Add(new ChartData
        {
            Group = "WorkOrders",
            Series = "Closed",
            Label = "2026-08-01",
            Value = 4,
            Timestamp = DateTime.UtcNow.AddDays(-1),
            Color = "#198754"
        });
        database.DashboardCharts.Add(new ChartData
        {
            Group = "Incidents",
            Series = "Critical",
            Label = "2026-08-01",
            Value = 1,
            Timestamp = DateTime.UtcNow.AddDays(-1),
            Color = "#dc3545"
        });

        var viewModel = new DashboardModuleViewModel(
            database,
            new AuditService(database),
            new StubCflDialogService(),
            new TestShellInteractionService(),
            new StubModuleNavigationService(),
            CreateLocalization(),
            new StubSignalRClientService());

        await viewModel.InitializeAsync(null);

        Assert.Collection(
            viewModel.KpiWidgets,
            widget => Assert.Equal("Open Work Orders", widget.Title),
            widget => Assert.Equal("Overdue CAPAs", widget.Title));

        Assert.Equal(2, viewModel.ChartSeries.Count);
        var workOrderSeries = viewModel.ChartSeries.Single(series => series.Group == "WorkOrders");
        Assert.Equal("Opened", workOrderSeries.Series);
        Assert.Equal(1, workOrderSeries.Points.Count(point => point.Series == "Opened"));

        var incidentSeries = viewModel.ChartSeries.Single(series => series.Group == "Incidents");
        Assert.Equal(1, incidentSeries.Points.Count);
        Assert.Equal("Critical", incidentSeries.Series);

        Assert.Contains("2 KPI(s)", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2 chart(s)", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InitializeAsync_WhenKpiAndChartCollectionsEmpty_StatusReflectsCounts()
    {
        var database = new DatabaseService();
        database.DashboardEvents.Add(new DashboardEvent
        {
            Id = 21,
            EventType = "noop",
            Severity = "info",
            Timestamp = DateTime.UtcNow
        });

        var viewModel = new DashboardModuleViewModel(
            database,
            new AuditService(database),
            new StubCflDialogService(),
            new TestShellInteractionService(),
            new StubModuleNavigationService(),
            CreateLocalization(),
            new StubSignalRClientService());

        await viewModel.InitializeAsync(null);

        Assert.Empty(viewModel.KpiWidgets);
        Assert.Empty(viewModel.ChartSeries);
        Assert.Contains("0 KPI(s)", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0 chart(s)", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InitializeAsync_WhenInventoryHelpersFail_SetsErrorStatus()
    {
        var database = new DatabaseService
        {
            InventoryZoneDashboardException = new InvalidOperationException("zone error"),
            InventoryMovementPreviewException = new InvalidOperationException("movement error")
        };

        database.DashboardEvents.Add(new DashboardEvent
        {
            Id = 5,
            EventType = "test",
            Severity = "info",
            Timestamp = DateTime.UtcNow
        });

        var viewModel = new DashboardModuleViewModel(
            database,
            new AuditService(database),
            new StubCflDialogService(),
            new TestShellInteractionService(),
            new StubModuleNavigationService(),
            CreateLocalization(),
            new StubSignalRClientService());

        await viewModel.InitializeAsync(null);

        Assert.Empty(viewModel.InventoryAlerts);
        Assert.Empty(viewModel.InventoryMovements);
        Assert.Contains("Unable to load inventory movements", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InitializeAsync_WhenKpiAndChartQueriesFail_AppendsErrors()
    {
        var database = new DatabaseService
        {
            KpiWidgetsException = new InvalidOperationException("kpi failure"),
            DashboardChartsException = new InvalidOperationException("chart failure")
        };

        database.DashboardEvents.Add(new DashboardEvent
        {
            Id = 42,
            EventType = "refresh",
            Severity = "info",
            Timestamp = DateTime.UtcNow
        });

        var viewModel = new DashboardModuleViewModel(
            database,
            new AuditService(database),
            new StubCflDialogService(),
            new TestShellInteractionService(),
            new StubModuleNavigationService(),
            CreateLocalization(),
            new StubSignalRClientService());

        await viewModel.InitializeAsync(null);

        Assert.Empty(viewModel.KpiWidgets);
        Assert.Empty(viewModel.ChartSeries);
        Assert.Contains("Unable to load KPI widgets", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Unable to load charts", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static DataTable CreateZoneTable()
    {
        var table = new DataTable();
        table.Columns.Add("part_id", typeof(int));
        table.Columns.Add("part_name", typeof(string));
        table.Columns.Add("part_code", typeof(string));
        table.Columns.Add("warehouse_id", typeof(int));
        table.Columns.Add("warehouse_name", typeof(string));
        table.Columns.Add("quantity", typeof(int));
        table.Columns.Add("min_threshold", typeof(int));
        table.Columns.Add("max_threshold", typeof(int));
        table.Columns.Add("zone", typeof(string));

        table.Rows.Add(11, "Buffer A", "PRT-11", 1, "Main", 5, 10, DBNull.Value, "Critical");
        table.Rows.Add(12, "Filter", "PRT-12", 2, "Overflow", 210, DBNull.Value, 100, "Warning");

        return table;
    }

    private static DataTable CreateMovementTable()
    {
        var table = new DataTable();
        table.Columns.Add("transaction_date", typeof(DateTime));
        table.Columns.Add("transaction_type", typeof(string));
        table.Columns.Add("quantity", typeof(int));
        table.Columns.Add("related_document", typeof(string));
        table.Columns.Add("note", typeof(string));
        table.Columns.Add("performed_by_id", typeof(int));

        table.Rows.Add(DateTime.UtcNow.AddHours(-1), "in", 25, "PO-15", "Received", 7);
        table.Rows.Add(DateTime.UtcNow.AddHours(-2), "out", -4, "WO-42", "Issued", DBNull.Value);

        return table;
    }

    private static FakeLocalizationService CreateLocalization()
    {
        var resources = new Dictionary<string, IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Module.Title.Dashboard"] = "Dashboard",
                ["Dashboard.Status.Summary"] = "{0}: inventory insights refreshed ({4} alert(s), {5} movement(s)).",
                ["Dashboard.Status.KpiError"] = "Unable to load KPI widgets: {0}",
                ["Dashboard.Status.ChartError"] = "Unable to load charts: {0}",
                ["Dashboard.Status.InventoryAlertError"] = "Unable to load inventory alerts: {0}",
                ["Dashboard.Status.InventoryMovementError"] = "Unable to load inventory movements: {0}",
                ["Dashboard.Chart.Group.Default"] = "Default Group",
                ["Dashboard.Chart.Series.Default"] = "Default Series",
                ["Dashboard.Inspector.KpiCount"] = "KPI widgets",
                ["Dashboard.Inspector.ChartSeriesCount"] = "Chart series",
                ["Dashboard.Inspector.InventoryAlertCount"] = "Inventory alerts",
                ["Dashboard.Inspector.InventoryMovementCount"] = "Inventory movements",
                ["Dashboard.Inspector.KpiError"] = "KPI issue",
                ["Dashboard.Inspector.ChartError"] = "Chart issue",
                ["Dashboard.Inspector.InventoryAlertError"] = "Inventory alert issue",
                ["Dashboard.Inspector.InventoryMovementError"] = "Inventory movement issue"
            }
        };

        return new FakeLocalizationService(resources, "en");
    }

    private sealed class StubSignalRClientService : ISignalRClientService
    {
        public event EventHandler<AuditEventArgs>? AuditReceived;
        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

        public RealtimeConnectionState ConnectionState => RealtimeConnectionState.Disconnected;

        public string? LastError => null;

        public DateTimeOffset? NextRetryUtc => null;

        public void Start()
        {
            // No-op for unit tests.
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
        }
    }
}
