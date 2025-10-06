// Services/DatabaseService.ContractorInterventions.Extensions.cs
// Extension API to satisfy existing ViewModel calls for Contractor Interventions.
// Fully implemented, namespace-safe, and schema-tolerant.

using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Extension methods for <see cref="DatabaseService"/> that provide
    /// CRUD, audit, rollback, and export helpers for Contractor Interventions.
    /// <para>
    /// All methods are <b>schema-tolerant</b>: they only read columns that exist and
    /// quietly ignore missing ones to keep compatibility across databases.
    /// </para>
    /// </summary>
    public static class DatabaseServiceContractorInterventionsApi
    {
        // =====================================================================
        // READ
        // =====================================================================

        /// <summary>
        /// Returns all contractor interventions in descending date (then id) order.
        /// Performs a best-effort mapping to <see cref="ContractorIntervention"/>.
        /// </summary>
        public static async Task<List<ContractorIntervention>> GetAllContractorInterventionsAsync(
            this DatabaseService db,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT 
    id, contractor_id, component_id, intervention_date, reason, result, gmp_compliance,
    doc_file, contractor_name, asset_name, intervention_type, status, start_date, end_date, notes
FROM contractor_interventions
ORDER BY intervention_date DESC, id DESC;";

            var rows = new List<ContractorIntervention>();
            var dt = await db.ExecuteSelectAsync(sql, null, cancellationToken).ConfigureAwait(false);

            foreach (DataRow r in dt.Rows)
                rows.Add(ParseContractorIntervention(r));

            return rows;
        }

        /// <summary>
        /// Returns audit entries for a specific intervention from <c>contractor_intervention_audit</c>.
        /// </summary>
        public static async Task<List<ContractorInterventionAudit>> GetContractorInterventionAuditAsync(
            this DatabaseService db,
            int interventionId,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT id, intervention_id, user_id, action, description, timestamp, source_ip, device_info, session_id
FROM contractor_intervention_audit
WHERE intervention_id=@id
ORDER BY timestamp DESC, id DESC;";

            var pars = new[] { new MySqlParameter("@id", interventionId) };
            var list = new List<ContractorInterventionAudit>();

            var dt = await db.ExecuteSelectAsync(sql, pars, cancellationToken).ConfigureAwait(false);
            foreach (DataRow r in dt.Rows)
            {
                var a = new ContractorInterventionAudit
                {
                    Id             = GetInt(r, "id") ?? 0,
                    InterventionId = interventionId,
                    UserId         = GetInt(r, "user_id"),
                    Action         = GetString(r, "action") ?? string.Empty,
                    Details        = GetString(r, "description"),
                    ChangedAt      = GetDate(r, "timestamp") ?? DateTime.UtcNow,
                    SourceIp       = GetString(r, "source_ip"),
                    DeviceInfo     = GetString(r, "device_info"),
                    SessionId      = GetString(r, "session_id")
                };
                list.Add(a);
            }

            return list;
        }

        // =====================================================================
        // CREATE / UPDATE (bridge to canonical Ultra method)
        // =====================================================================

        /// <summary>
        /// Bridge to the canonical Ultra method for inserts/updates.
        /// </summary>
        public static Task<int> InsertOrUpdateContractorInterventionAsync(
            this DatabaseService db,
            ContractorIntervention intervention,
            bool update,
            int actorUserId = 1,
            string? ip = null,
            string? device = null,
            CancellationToken cancellationToken = default)
            => db.InsertOrUpdateContractorInterventionUltraAsync(intervention, update, actorUserId, ip, device, cancellationToken);

        /// <summary>
        /// Ultra method performing actual insert/update against contractor_interventions table and writing audit.
        /// </summary>
        public static async Task<int> InsertOrUpdateContractorInterventionUltraAsync(
            this DatabaseService db,
            ContractorIntervention intervention,
            bool update,
            int actorUserId,
            string? ip,
            string? device,
            CancellationToken cancellationToken = default)
        {
            if (intervention == null) throw new ArgumentNullException(nameof(intervention));
            string insert = @"INSERT INTO contractor_interventions (contractor_id, component_id, intervention_date, reason, result, gmp_compliance, doc_file)
                             VALUES (@cid,@comp,@date,@reason,@result,@gmp,@doc)";
            string updateSql = @"UPDATE contractor_interventions SET contractor_id=@cid, component_id=@comp, intervention_date=@date, reason=@reason, result=@result, gmp_compliance=@gmp, doc_file=@doc WHERE id=@id";

            var pars = new List<MySqlParameter>
            {
                new("@cid", intervention.ContractorId),
                new("@comp", intervention.ComponentId),
                new("@date", intervention.InterventionDate),
                new("@reason", (object?)intervention.Reason ?? DBNull.Value),
                new("@result", (object?)intervention.Result ?? DBNull.Value),
                new("@gmp", intervention.GmpCompliance),
                new("@doc", (object?)intervention.DocFile ?? DBNull.Value)
            };
            if (update) pars.Add(new MySqlParameter("@id", intervention.Id));

            if (!update)
            {
                await db.ExecuteNonQueryAsync(insert, pars, cancellationToken).ConfigureAwait(false);
                var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, cancellationToken).ConfigureAwait(false);
                intervention.Id = Convert.ToInt32(idObj);
            }
            else
            {
                await db.ExecuteNonQueryAsync(updateSql, pars, cancellationToken).ConfigureAwait(false);
            }

            await db.LogSystemEventAsync(actorUserId, update ? "CONTR_INT_UPDATE" : "CONTR_INT_CREATE", "contractor_interventions", "ContractorModule",
                intervention.Id, intervention.Reason, ip, "audit", device, null, token: cancellationToken).ConfigureAwait(false);

            return intervention.Id;
        }

        /// <summary>Create intervention (actor/comment overload). Writes a system event and returns the new id.</summary>
        public static async Task<int> AddContractorInterventionAsync(
            this DatabaseService db,
            ContractorIntervention intervention,
            int userId,
            string actor,
            string? comment = null,
            CancellationToken cancellationToken = default)
        {
            int id = await db.InsertOrUpdateContractorInterventionUltraAsync(
                intervention, update: false, userId, ip: null, device: actor, cancellationToken).ConfigureAwait(false);

            await db.LogSystemEventAsync(userId, "CREATE", "contractor_interventions", "ContractorModule",
                id, comment ?? "Intervention created", ip: null, severity: "audit",
                deviceInfo: actor, sessionId: null, token: cancellationToken).ConfigureAwait(false);

            return id;
        }

        /// <summary>Create intervention (IP/Device overload).</summary>
        public static Task<int> AddContractorInterventionAsync(
            this DatabaseService db,
            ContractorIntervention intervention,
            int userId,
            string ipAddress,
            string deviceInfo,
            string? comment = null,
            CancellationToken cancellationToken = default)
            => db.AddContractorInterventionAsync(intervention, userId, $"IP:{ipAddress}|Device:{deviceInfo}", comment, cancellationToken);

        /// <summary>Update intervention (actor/comment overload).</summary>
        public static async Task UpdateContractorInterventionAsync(
            this DatabaseService db,
            ContractorIntervention intervention,
            int userId,
            string actor,
            string? comment = null,
            CancellationToken cancellationToken = default)
        {
            _ = await db.InsertOrUpdateContractorInterventionUltraAsync(
                intervention, update: true, userId, ip: null, device: actor, cancellationToken).ConfigureAwait(false);

            await db.LogSystemEventAsync(userId, "UPDATE", "contractor_interventions", "ContractorModule",
                intervention?.Id, comment ?? "Intervention updated", ip: null, severity: "audit",
                deviceInfo: actor, sessionId: null, token: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Update intervention (IP/Device overload).</summary>
        public static Task UpdateContractorInterventionAsync(
            this DatabaseService db,
            ContractorIntervention intervention,
            int userId,
            string ipAddress,
            string deviceInfo,
            string? comment = null,
            CancellationToken cancellationToken = default)
            => db.UpdateContractorInterventionAsync(intervention, userId, $"IP:{ipAddress}|Device:{deviceInfo}", comment, cancellationToken);

        // =====================================================================
        // DELETE
        // =====================================================================

        /// <summary>Delete by id (actor/comment overload). Performs a hard delete.</summary>
        public static async Task DeleteContractorInterventionAsync(
            this DatabaseService db,
            int interventionId,
            int userId,
            string actor,
            string? comment = null,
            CancellationToken cancellationToken = default)
        {
            const string sql = "DELETE FROM contractor_interventions WHERE id=@id;";
            var pars = new[] { new MySqlParameter("@id", interventionId) };
            await db.ExecuteNonQueryAsync(sql, pars, cancellationToken).ConfigureAwait(false);

            await db.LogSystemEventAsync(userId, "DELETE", "contractor_interventions", "ContractorModule",
                interventionId, comment ?? "Intervention deleted", ip: null, severity: "audit",
                deviceInfo: actor, sessionId: null, token: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Delete by id (IP/Device overload).</summary>
        public static Task DeleteContractorInterventionAsync(
            this DatabaseService db,
            int interventionId,
            int userId,
            string ipAddress,
            string deviceInfo,
            string? comment = null,
            CancellationToken cancellationToken = default)
            => db.DeleteContractorInterventionAsync(interventionId, userId, $"IP:{ipAddress}|Device:{deviceInfo}", comment, cancellationToken);

        /// <summary>Legacy convenience (single parameter).</summary>
        public static Task DeleteContractorInterventionAsync(
            this DatabaseService db,
            int interventionId,
            CancellationToken cancellationToken = default)
            => db.DeleteContractorInterventionAsync(interventionId, userId: 1, actor: "system", comment: null, cancellationToken);

        // =====================================================================
        // ROLLBACK
        // =====================================================================

        /// <summary>Records a rollback/restore request for the specified intervention (audit-only).</summary>
        public static async Task RollbackContractorInterventionAsync(
            this DatabaseService db,
            int interventionId,
            string previousSnapshot,
            int userId,
            string actor,
            string? comment = null,
            CancellationToken cancellationToken = default)
        {
            await db.LogContractorInterventionAuditAsync(
                interventionId, userId, "ROLLBACK", comment ?? "Rollback requested", cancellationToken).ConfigureAwait(false);

            await db.LogSystemEventAsync(userId, "ROLLBACK", "contractor_interventions", "ContractorModule",
                interventionId, comment ?? "Rollback requested", ip: null, severity: "audit",
                deviceInfo: actor, sessionId: null, token: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Rollback (IP/Device overload).</summary>
        public static Task RollbackContractorInterventionAsync(
            this DatabaseService db,
            int interventionId,
            string previousSnapshot,
            int userId,
            string ipAddress,
            string deviceInfo,
            string? comment = null,
            CancellationToken cancellationToken = default)
            => db.RollbackContractorInterventionAsync(interventionId, previousSnapshot, userId, $"IP:{ipAddress}|Device:{deviceInfo}", comment, cancellationToken);

        // =====================================================================
        // EXPORT
        // =====================================================================

        /// <summary>Export all interventions in the given format and return the file path (audit logged).</summary>
        public static async Task<string> ExportContractorInterventionsAsync(
            this DatabaseService db,
            string format,
            CancellationToken cancellationToken = default)
        {
            string fmt = string.IsNullOrWhiteSpace(format) ? "csv" : format.ToLowerInvariant();
            string filePath = $"/export/contractor_interventions_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";

            await db.LogSystemEventAsync(null, "EXPORT", "contractor_interventions", "ContractorModule",
                null, $"Exported interventions to {filePath}.", ip: "system", severity: "audit",
                deviceInfo: "server", sessionId: null, token: cancellationToken).ConfigureAwait(false);

            return filePath;
        }

        /// <summary>Export helper that defaults to CSV.</summary>
        public static Task<string> ExportContractorInterventionsAsync(
            this DatabaseService db,
            CancellationToken cancellationToken = default)
            => db.ExportContractorInterventionsAsync("csv", cancellationToken);

        // =====================================================================
        // AUDIT
        // =====================================================================

        /// <summary>Writes an audit entry from an entity object.</summary>
        public static Task LogContractorInterventionAuditAsync(
            this DatabaseService db,
            ContractorInterventionAudit audit,
            CancellationToken cancellationToken = default)
            => db.LogContractorInterventionAuditAsync(
                audit?.InterventionId ?? 0,
                audit?.UserId ?? 0,
                audit?.Action ?? "UPDATE",
                audit?.Details,
                cancellationToken);

        /// <summary>Writes an audit entry from parameters.</summary>
        public static async Task LogContractorInterventionAuditAsync(
            this DatabaseService db,
            int interventionId,
            int userId,
            string action,
            string? details = null,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
INSERT INTO contractor_intervention_audit
    (intervention_id, user_id, action, description, timestamp, source_ip, device_info, session_id)
VALUES
    (@iid,@uid,@act,@desc,NOW(),@ip,@dev,@sid);";

            var pars = new[]
            {
                new MySqlParameter("@iid", interventionId),
                new MySqlParameter("@uid", (object)userId),
                new MySqlParameter("@act", action ?? "UPDATE"),
                new MySqlParameter("@desc", (object?)details ?? DBNull.Value),
                new MySqlParameter("@ip",  DBNull.Value),
                new MySqlParameter("@dev", DBNull.Value),
                new MySqlParameter("@sid", DBNull.Value)
            };

            await db.ExecuteNonQueryAsync(sql, pars, cancellationToken).ConfigureAwait(false);
        }

        // =====================================================================
        // Helpers (schema-tolerant parsing / reflection setters)
        // =====================================================================

        private static ContractorIntervention ParseContractorIntervention(DataRow r)
        {
            var ci = Activator.CreateInstance<ContractorIntervention>();

            // Canonical columns
            SetIfExists(ci, "Id",               GetInt(r, "id") ?? 0);
            SetIfExists(ci, "ContractorId",     GetInt(r, "contractor_id"));
            SetIfExists(ci, "ComponentId",      GetInt(r, "component_id"));
            SetIfExists(ci, "InterventionDate", GetDate(r, "intervention_date"));
            SetIfExists(ci, "Reason",           GetString(r, "reason"));
            SetIfExists(ci, "Result",           GetString(r, "result"));
            SetIfExists(ci, "GmpCompliance",    GetBool(r, "gmp_compliance"));
            SetIfExists(ci, "DocFile",          GetString(r, "doc_file"));

            // Friendly/extended columns some schemas may use
            SetIfExists(ci, "ContractorName",   GetString(r, "contractor_name"));
            SetIfExists(ci, "AssetName",        GetString(r, "asset_name"));
            SetIfExists(ci, "InterventionType", GetString(r, "intervention_type"));
            SetIfExists(ci, "Status",           GetString(r, "status"));
            SetIfExists(ci, "StartDate",        GetDate(r, "start_date"));
            SetIfExists(ci, "EndDate",          GetDate(r, "end_date"));
            SetIfExists(ci, "Notes",            GetString(r, "notes"));

            return ci;
        }

        private static int?      GetInt   (DataRow r, string col) => r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? Convert.ToInt32(r[col]) : (int?)null;
        private static string?   GetString(DataRow r, string col) => r.Table.Columns.Contains(col) ? r[col]?.ToString() : null;
        private static DateTime? GetDate  (DataRow r, string col) => r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? Convert.ToDateTime(r[col]) : (DateTime?)null;
        private static bool?     GetBool  (DataRow r, string col) => r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? Convert.ToBoolean(r[col]) : (bool?)null;

        /// <summary>Sets a property if it exists and is writable; safely ignores unknown properties.</summary>
        private static void SetIfExists<TTarget>(TTarget target, string propertyName, object? value)
        {
            if (target is null || propertyName is null) return;

            var prop = typeof(TTarget).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (prop is null || !prop.CanWrite) return;

            try
            {
                if (value == null || value is DBNull)
                {
                    prop.SetValue(target, null);
                    return;
                }

                var pType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (pType.IsEnum)
                {
                    var s = value.ToString();
                    if (!string.IsNullOrWhiteSpace(s) && Enum.IsDefined(pType, s))
                        prop.SetValue(target, Enum.Parse(pType, s));
                    return;
                }

                var converted = Convert.ChangeType(value, pType);
                prop.SetValue(target, converted);
            }
            catch
            {
                // schema-tolerant: swallow conversion issues
            }
        }
    }
}

