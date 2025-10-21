using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        _inventoryAlerts = new ObservableCollection<InventoryAlertRow>();
        _inventoryMovements = new ObservableCollection<InventoryMovementRow>();
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var events = await Database.GetRecentDashboardEventsAsync(25).ConfigureAwait(false);
        await LoadInventoryInsightsAsync().ConfigureAwait(false);
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
    private readonly ObservableCollection<InventoryAlertRow> _inventoryAlerts;
    private readonly ObservableCollection<InventoryMovementRow> _inventoryMovements;

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

    private async Task LoadInventoryInsightsAsync()
    {
        DataTable? zoneTable = null;
        DataTable? movementTable = null;
        var alertsError = false;
        var movementsError = false;

        try
        {
            zoneTable = await Database.GetInventoryZoneDashboardAsync(25).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            alertsError = true;
            StatusMessage = $"Unable to load inventory alerts: {ex.Message}";
        }

        try
        {
            movementTable = await Database.GetInventoryMovementPreviewAsync(null, null, 15).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            movementsError = true;
            StatusMessage = $"Unable to load inventory movements: {ex.Message}";
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

            if (!alertsError && !movementsError)
            {
                StatusMessage = $"{Title}: inventory insights refreshed ({_inventoryAlerts.Count} alert(s), {_inventoryMovements.Count} movement(s)).";
            }
        }).ConfigureAwait(false);
    }

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
