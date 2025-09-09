// ==============================================================================
// File: Services/DatabaseService.WorkOrders.Extensions.cs
// Purpose: Work Orders CRUD + Audit helpers on DatabaseService (schema-tolerant)
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Extension methods providing Work Order queries and commands on <see cref="DatabaseService"/>.
    /// SQL is parameterized and mapping is tolerant to missing columns.
    /// </summary>
    public static class DatabaseServiceWorkOrderExtensions
    {
        // ========================= READ =========================

        public static async Task<List<WorkOrder>> GetAllWorkOrdersAsync(
            this DatabaseService db,
            CancellationToken token = default)
        {
            const string sql = @"SELECT id, machine_id, component_id, type, created_by, assigned_to, date_open, date_close,
    description, result, status, digital_signature, priority, related_incident, title, task_description, due_date,
    closed_at, requested_by_id, created_by_id, assigned_to_id, incident_id, notes, last_modified, last_modified_by_id,
    device_info, source_ip, session_id
FROM work_orders ORDER BY date_open DESC, id DESC";
            var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
            var list = new List<WorkOrder>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(ParseWorkOrder(r));
            return list;
        }

        public static Task<List<WorkOrder>> GetAllWorkOrdersFullAsync(
            this DatabaseService db,
            CancellationToken token = default)
            => GetAllWorkOrdersAsync(db, token);

        public static async Task<WorkOrder?> GetWorkOrderByIdAsync(
            this DatabaseService db,
            int id,
            CancellationToken token = default)
        {
            const string sql = @"SELECT id, machine_id, component_id, type, created_by, assigned_to, date_open, date_close,
    description, result, status, digital_signature, priority, related_incident, title, task_description, due_date,
    closed_at, requested_by_id, created_by_id, assigned_to_id, incident_id, notes, last_modified, last_modified_by_id,
    device_info, source_ip, session_id
FROM work_orders WHERE id=@id LIMIT 1";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            return dt.Rows.Count == 1 ? ParseWorkOrder(dt.Rows[0]) : null;
        }

        // ========================= WRITE =========================

        /// <summary>
        /// Insert or update a WorkOrder. Minimal column set to align with common schema.
        /// </summary>
        public static async Task<int> InsertOrUpdateWorkOrderAsync(
            this DatabaseService db,
            WorkOrder order,
            bool update,
            int actorUserId,
            string ip,
            string device,
            CancellationToken token = default)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            string insert = @"INSERT INTO work_orders
                (machine_id, component_id, type, created_by, assigned_to, date_open, date_close,
                 description, result, status, digital_signature, priority, related_incident)
            VALUES
                (@mid, @cid, @type, @cby, @asgn, @open, @close, @desc, @res, @status, @sig, @prio, @inc)";

            string updateSql = @"UPDATE work_orders SET
                machine_id=@mid, component_id=@cid, type=@type, created_by=@cby, assigned_to=@asgn,
                date_open=@open, date_close=@close, description=@desc, result=@res, status=@status,
                digital_signature=@sig, priority=@prio, related_incident=@inc
            WHERE id=@id";

            var pars = new List<MySqlParameter>
            {
                new("@mid",  (object?)order.MachineId),
                new("@cid",  (object?)order.ComponentId ?? DBNull.Value),
                new("@type", order.Type ?? string.Empty),
                new("@cby",  order.CreatedById),
                new("@asgn", order.AssignedToId),
                new("@open", order.DateOpen == default ? (object)DBNull.Value : order.DateOpen),
                new("@close", (object?)order.DateClose ?? DBNull.Value),
                new("@desc", order.Description ?? order.Title ?? string.Empty),
                new("@res",  order.Result ?? string.Empty),
                new("@status", order.Status ?? "otvoren"),
                new("@sig", order.DigitalSignature ?? string.Empty),
                new("@prio", order.Priority ?? "srednji"),
                new("@inc",  (object?)order.IncidentId ?? DBNull.Value)
            };
            if (update) pars.Add(new MySqlParameter("@id", order.Id));

            try
            {
                if (!update)
                {
                    await db.ExecuteNonQueryAsync(insert, pars, token).ConfigureAwait(false);
                    var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
                    order.Id = Convert.ToInt32(idObj);
                }
                else
                {
                    await db.ExecuteNonQueryAsync(updateSql, pars, token).ConfigureAwait(false);
                }
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                // Schema differences: try a reduced set
                string insertMin = @"INSERT INTO work_orders (machine_id, type, description, status, priority)
                                     VALUES (@mid, @type, @desc, @status, @prio)";
                string updateMin = @"UPDATE work_orders SET machine_id=@mid, type=@type, description=@desc, status=@status, priority=@prio WHERE id=@id";
                var parsMin = new List<MySqlParameter>
                {
                    new("@mid",  (object?)order.MachineId),
                    new("@type", order.Type ?? string.Empty),
                    new("@desc", order.Description ?? order.Title ?? string.Empty),
                    new("@status", order.Status ?? "otvoren"),
                    new("@prio", order.Priority ?? "srednji")
                };
                if (update) parsMin.Add(new MySqlParameter("@id", order.Id));

                if (!update)
                {
                    await db.ExecuteNonQueryAsync(insertMin, parsMin, token).ConfigureAwait(false);
                    var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
                    order.Id = Convert.ToInt32(idObj);
                }
                else
                {
                    await db.ExecuteNonQueryAsync(updateMin, parsMin, token).ConfigureAwait(false);
                }
            }

            // Audit (system event)
            await db.LogSystemEventAsync(
                userId: actorUserId,
                eventType: update ? "WO_UPDATE" : "WO_CREATE",
                tableName: "work_orders",
                module: "WorkOrders",
                recordId: order.Id,
                description: update ? $"Work order updated (Id={order.Id})" : $"Work order created (Id={order.Id})",
                ip: ip,
                severity: "audit",
                deviceInfo: device,
                sessionId: null,
                token: token
            ).ConfigureAwait(false);

            return order.Id;
        }

        public static async Task<int> InsertOrUpdateWorkOrderAsync(
            this DatabaseService db,
            WorkOrder order,
            bool update,
            int actorUserId,
            string ip,
            string device,
            string? sessionId,
            CancellationToken token = default)
        {
            var id = await db.InsertOrUpdateWorkOrderAsync(order, update, actorUserId, ip, device, token).ConfigureAwait(false);
            // Session-context audit line
            await db.LogSystemEventAsync(
                userId: actorUserId,
                eventType: update ? "WO_UPDATE_CTX" : "WO_CREATE_CTX",
                tableName: "work_orders",
                module: "WorkOrders",
                recordId: id,
                description: null,
                ip: ip,
                severity: "info",
                deviceInfo: device,
                sessionId: sessionId,
                token: token
            ).ConfigureAwait(false);
            return id;
        }

        public static async Task DeleteWorkOrderAsync(
            this DatabaseService db,
            int id,
            int actorUserId,
            string ip,
            string device,
            CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync("DELETE FROM work_orders WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            await db.LogSystemEventAsync(actorUserId, "WO_DELETE", "work_orders", "WorkOrders", id, "Work order deleted", ip, "audit", device, null, token: token).ConfigureAwait(false);
        }

        public static async Task DeleteWorkOrderAsync(
            this DatabaseService db,
            int id,
            int actorUserId,
            string ip,
            string device,
            string? sessionId,
            CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync("DELETE FROM work_orders WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            await db.LogSystemEventAsync(actorUserId, "WO_DELETE", "work_orders", "WorkOrders", id, "Work order deleted", ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
        }

        public static async Task<int> AddWorkOrderAsync(
            this DatabaseService db,
            WorkOrder order,
            int actorUserId,
            string ip,
            string device,
            string? sessionId,
            CancellationToken token = default)
        {
            return await db.InsertOrUpdateWorkOrderAsync(order, update: false, actorUserId, ip, device, sessionId, token).ConfigureAwait(false);
        }

        // ========================= EXTRAS (VM actions) =========================

        public static Task ExportWorkOrdersAsync(this DatabaseService db, List<WorkOrder> items, string ip, string device, string sessionId, int actorUserId, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "WO_EXPORT", "work_orders", "WorkOrders", null, $"count={items?.Count ?? 0}", ip, "info", device, sessionId, token: token);

        // Overload matching VM call order (items, actorUserId, ip, device, session)
        public static Task ExportWorkOrdersAsync(this DatabaseService db, List<WorkOrder> items, int actorUserId, string ip, string device, string sessionId, CancellationToken token = default)
            => db.ExportWorkOrdersAsync(items, ip, device, sessionId, actorUserId, token);

        public static async Task ApproveWorkOrderAsync(this DatabaseService db, int workOrderId, int actorUserId, string ip, string device, CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync("UPDATE work_orders SET status='zavrsen' WHERE id=@id", new[] { new MySqlParameter("@id", workOrderId) }, token).ConfigureAwait(false);
            await db.LogSystemEventAsync(actorUserId, "WO_APPROVE", "work_orders", "WorkOrders", workOrderId, null, ip, "audit", device, null, token: token).ConfigureAwait(false);
        }

        // Overload with optional note
        public static Task ApproveWorkOrderAsync(this DatabaseService db, int workOrderId, int actorUserId, string? note, string ip, string device, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "WO_APPROVE", "work_orders", "WorkOrders", workOrderId, note, ip, "audit", device, null, token: token);

        public static async Task CloseWorkOrderAsync(this DatabaseService db, int workOrderId, int actorUserId, string ip, string device, CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync("UPDATE work_orders SET status='zavrsen', date_close=NOW() WHERE id=@id", new[] { new MySqlParameter("@id", workOrderId) }, token).ConfigureAwait(false);
            await db.LogSystemEventAsync(actorUserId, "WO_CLOSE", "work_orders", "WorkOrders", workOrderId, null, ip, "audit", device, null, token: token).ConfigureAwait(false);
        }

        // Overload with optional note
        public static Task CloseWorkOrderAsync(this DatabaseService db, int workOrderId, int actorUserId, string? note, string ip, string device, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "WO_CLOSE", "work_orders", "WorkOrders", workOrderId, note, ip, "audit", device, null, token: token);

        public static Task EscalateWorkOrderAsync(this DatabaseService db, int workOrderId, int actorUserId, string ip, string device, string? sessionId, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "WO_ESCALATE", "work_orders", "WorkOrders", workOrderId, null, ip, "warn", device, sessionId, token: token);

        // Overload with note
        public static Task EscalateWorkOrderAsync(this DatabaseService db, int workOrderId, int actorUserId, string? note, string ip, string device, string? sessionId = null, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "WO_ESCALATE", "work_orders", "WorkOrders", workOrderId, note, ip, "warn", device, sessionId, token: token);

        public static Task AddWorkOrderCommentAsync(this DatabaseService db, int workOrderId, int actorUserId, string comment, string ip, string device, string? sessionId, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "WO_COMMENT", "work_orders", "WorkOrders", workOrderId, comment, ip, "info", device, sessionId, token: token);

        // Back-compat overload used by VM (only 3 params)
        public static Task AddWorkOrderCommentAsync(this DatabaseService db, int workOrderId, int actorUserId, string comment, CancellationToken token = default)
            => db.AddWorkOrderCommentAsync(workOrderId, actorUserId, comment, ip: string.Empty, device: string.Empty, sessionId: null, token: token);

        // ========================= AUDIT =========================

        /// <summary>
        /// Writes a row into work_order_audit if present; falls back to system_event_log.
        /// </summary>
        public static async Task LogWorkOrderAuditAsync(
            this DatabaseService db,
            int workOrderId,
            int userId,
            string action,
            string? note,
            string? ip,
            string? device,
            CancellationToken token = default)
        {
            try
            {
                const string sql = @"INSERT INTO work_order_audit
                    (work_order_id, user_id, action, note, source_ip, device_info)
                 VALUES (@wo, @uid, @act, @note, @ip, @dev)";
                await db.ExecuteNonQueryAsync(sql, new[]
                {
                    new MySqlParameter("@wo", workOrderId),
                    new MySqlParameter("@uid", userId),
                    new MySqlParameter("@act", action ?? "UPDATE"),
                    new MySqlParameter("@note", (object?)note ?? DBNull.Value),
                    new MySqlParameter("@ip", (object?)ip ?? DBNull.Value),
                    new MySqlParameter("@dev", (object?)device ?? DBNull.Value)
                }, token).ConfigureAwait(false);
                return;
            }
            catch (MySqlException ex) when (ex.Number == 1146) // table missing
            {
                await db.LogSystemEventAsync(userId, $"WO_{action}", "work_orders", "WorkOrders", workOrderId, note, ip, "audit", device, null, token: token).ConfigureAwait(false);
            }
        }

        // ========================= Mapping =========================

        private static WorkOrder ParseWorkOrder(DataRow r)
        {
            int? GetInt(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
            string? GetString(string c) => r.Table.Columns.Contains(c) ? r[c]?.ToString() : null;
            DateTime? GetDate(string c)
            {
                if (!r.Table.Columns.Contains(c) || r[c] == DBNull.Value) return null;
                try { return Convert.ToDateTime(r[c]); } catch { return null; }
            }

            var wo = new WorkOrder();
            SetIfExists(wo, nameof(WorkOrder.Id), GetInt("id") ?? 0);
            SetIfExists(wo, nameof(WorkOrder.MachineId), GetInt("machine_id") ?? wo.MachineId);
            SetIfExists(wo, nameof(WorkOrder.ComponentId), GetInt("component_id"));
            SetIfExists(wo, nameof(WorkOrder.Type), GetString("type") ?? wo.Type);
            SetIfExists(wo, nameof(WorkOrder.CreatedById), GetInt("created_by") ?? wo.CreatedById);
            SetIfExists(wo, nameof(WorkOrder.AssignedToId), GetInt("assigned_to") ?? wo.AssignedToId);
            SetIfExists(wo, nameof(WorkOrder.DateOpen), GetDate("date_open") ?? wo.DateOpen);
            SetIfExists(wo, nameof(WorkOrder.DateClose), GetDate("date_close"));
            SetIfExists(wo, nameof(WorkOrder.Description), GetString("description") ?? wo.Description);
            SetIfExists(wo, nameof(WorkOrder.Result), GetString("result") ?? wo.Result);
            SetIfExists(wo, nameof(WorkOrder.Status), GetString("status") ?? wo.Status);
            SetIfExists(wo, nameof(WorkOrder.DigitalSignature), GetString("digital_signature") ?? wo.DigitalSignature);
            SetIfExists(wo, nameof(WorkOrder.Priority), GetString("priority") ?? wo.Priority);
            SetIfExists(wo, nameof(WorkOrder.IncidentId), GetInt("related_incident"));

            // For enriched schemas, try reading these if they exist
            SetIfExists(wo, nameof(WorkOrder.LastModified), GetDate("updated_at") ?? wo.LastModified);

            return wo;
        }

        private static void SetIfExists<TTarget>(TTarget target, string propertyName, object? value)
        {
            if (target == null || string.IsNullOrWhiteSpace(propertyName)) return;
            var p = typeof(TTarget).GetProperty(propertyName);
            if (p == null || !p.CanWrite) return;
            try
            {
                if (value == null || value is DBNull)
                {
                    if (!p.PropertyType.IsValueType || Nullable.GetUnderlyingType(p.PropertyType) != null)
                        p.SetValue(target, null);
                    return;
                }
                var dest = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                if (value is IConvertible) p.SetValue(target, Convert.ChangeType(value, dest));
                else p.SetValue(target, value);
            }
            catch
            {
                // swallow to stay schema-tolerant
            }
        }
    }
}
