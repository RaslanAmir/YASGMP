using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace YasGMP.Services
{
    /// <summary>
    /// Background scheduler that runs small periodic checks in-app:
    /// - PPM generator: create work orders for due preventive plans
    /// - Calibration alerts: 30/14/7 days before next_due
    /// - PQ renewal alerts: before validation_sets.pq_renewal_due
    /// - Low stock alerts: parts where stock &lt; min_stock_alert
    ///
    /// Notes:
    /// - Designed to be idempotent: uses external_ref markers to avoid duplicate WOs
    /// - Safe: catches all exceptions; never crashes UI
    /// - Lightweight: one pass on startup, then every 30 minutes
    /// </summary>
    public sealed class BackgroundScheduler : IDisposable
    {
        private readonly DatabaseService _db;
        private readonly Timer _timer;
        private bool _running;
        private readonly AttachmentRetentionEnforcer _retention;
        /// <summary>
        /// Initializes a new instance of the BackgroundScheduler class.
        /// </summary>

        public BackgroundScheduler(DatabaseService db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _retention = new AttachmentRetentionEnforcer(_db);
            // fire once after short delay, then every 30 minutes
            _timer = new Timer(async _ => await SafeRunAsync().ConfigureAwait(false), null,
                               dueTime: TimeSpan.FromSeconds(15),
                               period: TimeSpan.FromMinutes(30));
            // also run immediately (non-blocking)
            _ = SafeRunAsync();
        }

        private async Task SafeRunAsync()
        {
            if (_running) return; // prevent overlap
            _running = true;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                await RunPpmGeneratorAsync(cts.Token).ConfigureAwait(false);
                await RunCalibrationAlertsAsync(cts.Token).ConfigureAwait(false);
                await RunPqRenewalAlertsAsync(cts.Token).ConfigureAwait(false);
                await RunLowStockAlertsAsync(cts.Token).ConfigureAwait(false);
                await _retention.RunOnceAsync(cts.Token).ConfigureAwait(false);
            }
            catch
            {
                // swallow: diagnostics are handled inside the db logger when available
            }
            finally { _running = false; }
        }

        // ------------------- PPM generator -------------------
        private async Task RunPpmGeneratorAsync(CancellationToken token)
        {
            const string sqlDue = @"SELECT id, code, name, machine_id, component_id, next_due
                                     FROM preventive_maintenance_plans
                                     WHERE next_due IS NOT NULL AND next_due <= CURDATE()";
            DataTable dt;
            try { dt = await _db.ExecuteSelectAsync(sqlDue, null, token).ConfigureAwait(false); }
            catch { return; }

            foreach (DataRow r in dt.Rows)
            {
                int planId = ToInt(r, "id");
                string code = ToStr(r, "code");
                string name = ToStr(r, "name");
                int? machineId = ToIntN(r, "machine_id");
                int? componentId = ToIntN(r, "component_id");
                var nextDue = ToDateN(r, "next_due") ?? DateTime.UtcNow.Date;

                string extRef = $"PPM:{planId}:{nextDue:yyyyMMdd}";

                // skip if already generated
                var exist = await _db.ExecuteSelectAsync(
                    "SELECT id FROM work_orders WHERE external_ref=@x LIMIT 1",
                    new[] { new MySqlParameter("@x", extRef) }, token
                ).ConfigureAwait(false);
                if (exist.Rows.Count > 0) continue;

                const string insert = @"INSERT INTO work_orders
                  (title, task_description, machine_id, component_id, type, status, priority, date_open, due_date, external_ref)
                  VALUES
                  (@t, @d, @m, @c, 'preventivni', 'planiran', 'srednji', CURDATE(), @due, @ref)";
                try
                {
                    var pars = new[]
                    {
                        new MySqlParameter("@t", string.IsNullOrWhiteSpace(name) ? (object?)code ?? DBNull.Value : name),
                        new MySqlParameter("@d", (object?)$"PPM: {code} {name}" ?? DBNull.Value),
                        new MySqlParameter("@m", (object?)machineId ?? DBNull.Value),
                        new MySqlParameter("@c", (object?)componentId ?? DBNull.Value),
                        new MySqlParameter("@due", nextDue),
                        new MySqlParameter("@ref", extRef)
                    };
                    await _db.ExecuteNonQueryAsync(insert, pars, token).ConfigureAwait(false);
                    await _db.LogSystemEventAsync(
                        userId: null,
                        eventType: "PPM_WO_CREATE",
                        tableName: "work_orders",
                        module: "BackgroundScheduler",
                        recordId: null,
                        description: $"PPM plan {code} → WO (due {nextDue:yyyy-MM-dd})",
                        ip: "system",
                        severity: "info",
                        deviceInfo: "scheduler",
                        sessionId: null,
                        token: token
                    ).ConfigureAwait(false);
                }
                catch
                {
                    // ignore individual failures
                }
            }
        }

        // ------------------- Calibration alerts -------------------
        private async Task RunCalibrationAlertsAsync(CancellationToken token)
        {
            const string sql = @"SELECT id, component_id, next_due
                                 FROM calibrations
                                 WHERE next_due IS NOT NULL AND DATEDIFF(next_due, CURDATE()) IN (30,14,7)";
            DataTable dt;
            try { dt = await _db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false); }
            catch { return; }

            foreach (DataRow r in dt.Rows)
            {
                int id = ToInt(r, "id");
                var due = ToDateN(r, "next_due");
                await _db.LogSystemEventAsync(
                    userId: null,
                    eventType: "CALIB_DUE",
                    tableName: "calibrations",
                    module: "BackgroundScheduler",
                    recordId: id,
                    description: $"Calibration due {due:yyyy-MM-dd}",
                    ip: "system",
                    severity: "info",
                    deviceInfo: "scheduler",
                    sessionId: null,
                    token: token
                ).ConfigureAwait(false);
            }
        }

        // ------------------- PQ renewal alerts -------------------
        private async Task RunPqRenewalAlertsAsync(CancellationToken token)
        {
            const string sql = @"SELECT id, pq_renewal_due FROM validation_sets
                                 WHERE pq_renewal_due IS NOT NULL AND DATEDIFF(pq_renewal_due, CURDATE()) IN (30,14,7)";
            DataTable dt;
            try { dt = await _db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false); }
            catch { return; }

            foreach (DataRow r in dt.Rows)
            {
                int id = ToInt(r, "id");
                var due = ToDateN(r, "pq_renewal_due");
                await _db.LogSystemEventAsync(
                    userId: null,
                    eventType: "PQ_RENEWAL_DUE",
                    tableName: "validation_sets",
                    module: "BackgroundScheduler",
                    recordId: id,
                    description: $"PQ renewal due {due:yyyy-MM-dd}",
                    ip: "system",
                    severity: "info",
                    deviceInfo: "scheduler",
                    sessionId: null,
                    token: token
                ).ConfigureAwait(false);
            }
        }

        // ------------------- Low stock alerts -------------------
        private async Task RunLowStockAlertsAsync(CancellationToken token)
        {
            const string sql = @"SELECT id, code, name, stock, min_stock_alert FROM parts
                                 WHERE min_stock_alert IS NOT NULL AND stock < min_stock_alert";
            DataTable dt;
            try { dt = await _db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false); }
            catch { return; }

            foreach (DataRow r in dt.Rows)
            {
                int id = ToInt(r, "id");
                string code = ToStr(r, "code");
                int stock = ToInt(r, "stock");
                int min = ToInt(r, "min_stock_alert");
                await _db.LogSystemEventAsync(
                    userId: null,
                    eventType: "LOW_STOCK",
                    tableName: "parts",
                    module: "BackgroundScheduler",
                    recordId: id,
                    description: $"Part {code} low stock: {stock} < {min}",
                    ip: "system",
                    severity: "warn",
                    deviceInfo: "scheduler",
                    sessionId: null,
                    token: token
                ).ConfigureAwait(false);
            }
        }

        // ------------------- helpers -------------------
        private static string ToStr(DataRow r, string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
        private static int ToInt(DataRow r, string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
        private static int? ToIntN(DataRow r, string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
        private static DateTime? ToDateN(DataRow r, string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;
        /// <summary>
        /// Executes the dispose operation.
        /// </summary>

        public void Dispose()
        {
            try { _timer.Dispose(); } catch { }
        }
    }
}

