// ==============================================================================
// File: Services/DatabaseService.CapaExtensions.cs
// Purpose: CAPA audit helpers as extension methods (no partial/virtual).
// NOTE: Stubbed implementations so project compiles; wire up to SQL later.
//       Likely tables: capa_action_log, capa_status_history (per your SQL).
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.Models.Enums;

namespace YasGMP.Services
{
    public static class DatabaseServiceCapaExtensions
    {
        // ---- CAPA AUDIT QUERIES ------------------------------------------------

        public static async Task<CapaAudit?> GetCapaAuditByIdAsync(
            this DatabaseService db,
            int id,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"SELECT id, capa_case_id, old_status, new_status, changed_by, changed_at, note, source_ip, digital_signature, change_version, is_deleted
                                 FROM capa_audit_log WHERE id=@id LIMIT 1;";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@id", id) }, cancellationToken).ConfigureAwait(false);
            return dt.Rows.Count == 1 ? MapCapaAudit(dt.Rows[0]) : null;
        }

        public static async Task<List<CapaAudit>> GetCapaAuditsByCapaIdAsync(
            this DatabaseService db,
            int capaId,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"SELECT id, capa_case_id, old_status, new_status, changed_by, changed_at, note, source_ip, digital_signature, change_version, is_deleted
                                 FROM capa_audit_log WHERE capa_case_id=@capa ORDER BY changed_at DESC, id DESC;";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@capa", capaId) }, cancellationToken).ConfigureAwait(false);
            var list = new List<CapaAudit>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(MapCapaAudit(r));
            return list;
        }

        public static async Task<List<CapaAudit>> GetCapaAuditsByUserIdAsync(
            this DatabaseService db,
            int userId,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"SELECT id, capa_case_id, old_status, new_status, changed_by, changed_at, note, source_ip, digital_signature, change_version, is_deleted
                                 FROM capa_audit_log WHERE changed_by=@uid ORDER BY changed_at DESC, id DESC;";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@uid", userId) }, cancellationToken).ConfigureAwait(false);
            var list = new List<CapaAudit>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(MapCapaAudit(r));
            return list;
        }

        public static async Task<List<CapaAudit>> GetCapaAuditsByActionAsync(
            this DatabaseService db,
            CapaActionType action,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"SELECT id, capa_case_id, old_status, new_status, changed_by, changed_at, note, source_ip, digital_signature, change_version, is_deleted
                                 FROM capa_audit_log WHERE new_status=@action ORDER BY changed_at DESC, id DESC;";
            var pars = new[] { new MySqlParameter("@action", action.ToString()) };
            var dt = await db.ExecuteSelectAsync(sql, pars, cancellationToken).ConfigureAwait(false);
            var list = new List<CapaAudit>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(MapCapaAudit(r));
            return list;
        }

        public static async Task<List<CapaAudit>> GetCapaAuditsByDateRangeAsync(
            this DatabaseService db,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"SELECT id, capa_case_id, old_status, new_status, changed_by, changed_at, note, source_ip, digital_signature, change_version, is_deleted
                                 FROM capa_audit_log WHERE changed_at BETWEEN @from AND @to ORDER BY changed_at DESC, id DESC;";
            var pars = new[] { new MySqlParameter("@from", from), new MySqlParameter("@to", to) };
            var dt = await db.ExecuteSelectAsync(sql, pars, cancellationToken).ConfigureAwait(false);
            var list = new List<CapaAudit>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(MapCapaAudit(r));
            return list;
        }

        private static CapaAudit MapCapaAudit(DataRow r)
        {
            var audit = new CapaAudit
            {
                Id = r["id"] != DBNull.Value ? Convert.ToInt32(r["id"]) : 0,
                CapaId = r["capa_case_id"] != DBNull.Value ? Convert.ToInt32(r["capa_case_id"]) : 0,
                UserId = r["changed_by"] != DBNull.Value ? Convert.ToInt32(r["changed_by"]) : 0,
                ChangedAt = r["changed_at"] != DBNull.Value ? Convert.ToDateTime(r["changed_at"]) : DateTime.UtcNow,
                Details = r["note"]?.ToString() ?? string.Empty,
                OldValue = r["old_status"]?.ToString() ?? string.Empty,
                NewValue = r["new_status"]?.ToString() ?? string.Empty,
                DigitalSignature = r.Table.Columns.Contains("digital_signature") ? r["digital_signature"]?.ToString() ?? string.Empty : string.Empty,
                SourceIp = r.Table.Columns.Contains("source_ip") ? r["source_ip"]?.ToString() ?? string.Empty : string.Empty,
                ChangeVersion = r.Table.Columns.Contains("change_version") && r["change_version"] != DBNull.Value ? Convert.ToInt32(r["change_version"]) : 1,
                IsDeleted = r.Table.Columns.Contains("is_deleted") && r["is_deleted"] != DBNull.Value && Convert.ToBoolean(r["is_deleted"])
            };

            if (Enum.TryParse<CapaActionType>(audit.NewValue, true, out var parsed))
                audit.Action = parsed;
            else
                audit.Action = CapaActionType.StatusChange;

            return audit;
        }
    }
}
