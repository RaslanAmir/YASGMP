// ==============================================================================
// File: Services/DatabaseService.IncidentAudits.Extensions.cs
// Purpose: Minimal Incident Audit CRUD/queries used by IncidentAuditService
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
    /// DatabaseService extensions that expose incident audit history.
    /// </summary>
    public static class DatabaseServiceIncidentAuditsExtensions
    {
        /// <summary>
        /// Executes the insert incident audit async operation.
        /// </summary>
        public static async Task<int> InsertIncidentAuditAsync(this DatabaseService db, IncidentAudit a, CancellationToken token = default)
        {
            await db.LogSystemEventAsync(
                userId: a.UserId,
                eventType: "INC_AUDIT",
                tableName: "incident_audit",
                module: "IncidentAuditModule",
                recordId: a.IncidentId,
                description: a.Note,
                ip: a.SourceIp,
                severity: "audit",
                deviceInfo: a.DeviceInfo,
                sessionId: null,
                token: token
            ).ConfigureAwait(false);

            try
            {
                const string sql = @"INSERT INTO incident_audit /* ANALYZER_IGNORE: audit table */ (incident_id, user_id, action, old_value, new_value, action_at, note, source_ip, digital_signature, capa_id, work_order_id, device_info, integrity_hash, inspector_note)
                                 VALUES (@iid,@uid,@act,@old,@new,@at,@note,@ip,@sig,@capa,@wo,@dev,@ih,@ins)";
                var pars = new List<MySqlParameter>
                {
                    new("@iid", a.IncidentId),
                    new("@uid", a.UserId),
                    new("@act", a.Action.ToString()),
                    new("@old", (object?)a.OldValue ?? DBNull.Value),
                    new("@new", (object?)a.NewValue ?? DBNull.Value),
                    new("@at", a.ActionAt),
                    new("@note", (object?)a.Note ?? DBNull.Value),
                    new("@ip", (object?)a.SourceIp ?? DBNull.Value),
                    new("@sig", (object?)a.DigitalSignature ?? DBNull.Value),
                    new("@capa", (object?)a.CapaId ?? DBNull.Value),
                    new("@wo", (object?)a.WorkOrderId ?? DBNull.Value),
                    new("@dev", (object?)a.DeviceInfo ?? DBNull.Value),
                    new("@ih", (object?)a.IntegrityHash ?? DBNull.Value),
                    new("@ins", (object?)a.InspectorNote ?? DBNull.Value)
                };
                await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);
                var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
                a.Id = Convert.ToInt32(idObj);
            }
            catch (MySqlException)
            {
                // legacy table missing
            }

            return a.Id;
        }
        /// <summary>
        /// Executes the get incident audit by id async operation.
        /// </summary>

        public static async Task<IncidentAudit?> GetIncidentAuditByIdAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            try
            {
                const string sql = @"SELECT id, incident_id, user_id, action, old_value, new_value, action_at, note, source_ip, digital_signature, capa_id, work_order_id, device_info, integrity_hash, inspector_note
                                     FROM incident_audit WHERE id=@id LIMIT 1";
                var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
                return dt.Rows.Count == 1 ? Map(dt.Rows[0]) : null;
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                return null;
            }
        }
        /// <summary>
        /// Executes the get incident audits by incident id async operation.
        /// </summary>

        public static async Task<List<IncidentAudit>> GetIncidentAuditsByIncidentIdAsync(this DatabaseService db, int incidentId, CancellationToken token = default)
        {
            try
            {
                const string sql = @"SELECT id, incident_id, user_id, action, old_value, new_value, action_at, note, source_ip, digital_signature, capa_id, work_order_id, device_info, integrity_hash, inspector_note
                                     FROM incident_audit WHERE incident_id=@id ORDER BY action_at DESC, id DESC";
                var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@id", incidentId) }, token).ConfigureAwait(false);
                return MapMany(dt);
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                return new List<IncidentAudit>();
            }
        }
        /// <summary>
        /// Executes the get incident audits by user id async operation.
        /// </summary>

        public static async Task<List<IncidentAudit>> GetIncidentAuditsByUserIdAsync(this DatabaseService db, int userId, CancellationToken token = default)
        {
            try
            {
                const string sql = @"SELECT id, incident_id, user_id, action, old_value, new_value, action_at, note, source_ip, digital_signature, capa_id, work_order_id, device_info, integrity_hash, inspector_note
                                     FROM incident_audit WHERE user_id=@id ORDER BY action_at DESC, id DESC";
                var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@id", userId) }, token).ConfigureAwait(false);
                return MapMany(dt);
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                return new List<IncidentAudit>();
            }
        }

        private static List<IncidentAudit> MapMany(DataTable dt)
        {
            var list = new List<IncidentAudit>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        private static IncidentAudit Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            int? IN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
            DateTime D(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : DateTime.UtcNow;

            var a = new IncidentAudit
            {
                Id = I("id"),
                IncidentId = I("incident_id"),
                UserId = I("user_id"),
                OldValue = S("old_value"),
                NewValue = S("new_value"),
                ActionAt = D("action_at"),
                Note = S("note"),
                SourceIp = S("source_ip"),
                DigitalSignature = S("digital_signature"),
                CapaId = IN("capa_id"),
                WorkOrderId = IN("work_order_id"),
                DeviceInfo = S("device_info"),
                IntegrityHash = S("integrity_hash"),
                InspectorNote = S("inspector_note")
            };
            // Action is enum; stored as string in DB
            var actionRaw = S("action");
            if (Enum.TryParse<YasGMP.Models.Enums.IncidentActionType>(actionRaw, true, out var parsed))
                a.Action = parsed;
            return a;
        }
    }
}
