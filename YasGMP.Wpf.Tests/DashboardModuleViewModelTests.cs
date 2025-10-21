using System;
using System.Data;
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
        // Arrange
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

        var localization = new FakeLocalizationService(
            new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Module.Title.Dashboard"] = "Dashboard"
                }
            },
            "en");

        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var realtime = new StubSignalRClientService();

        var viewModel = new DashboardModuleViewModel(database, auditService, cfl, shell, navigation, localization, realtime);

        // Act
        await viewModel.InitializeAsync(null);

        // Assert
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
    public async Task InitializeAsync_WhenInventoryHelpersFail_SetsErrorStatus()
    {
        // Arrange
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

        var localization = new FakeLocalizationService(
            new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Module.Title.Dashboard"] = "Dashboard"
                }
            },
            "en");

        var viewModel = new DashboardModuleViewModel(
            database,
            new AuditService(database),
            new StubCflDialogService(),
            new TestShellInteractionService(),
            new StubModuleNavigationService(),
            localization,
            new StubSignalRClientService());

        // Act
        await viewModel.InitializeAsync(null);

        // Assert
        Assert.Empty(viewModel.InventoryAlerts);
        Assert.Empty(viewModel.InventoryMovements);
        Assert.Contains("Unable to load inventory movements", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
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
    }
}
