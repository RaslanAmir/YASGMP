// ==============================================================================
// File: Services/DatabaseService.Capa.Cases.Extensions.cs
// Purpose: Minimal CAPA case CRUD used by CAPAService; schema-tolerant
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.AppCore.Models.Signatures;

namespace YasGMP.Services
{
    /// <summary>
    /// DatabaseService extensions focused on CAPA cases and related data.
    /// </summary>
    public static class DatabaseServiceCapaCasesExtensions
    {
        public static async Task<List<CapaCase>> GetAllCapaCasesAsync(this DatabaseService db, CancellationToken token = default)
        {
            const string sql = @"SELECT 
    id, title, description, component_id, date_open, date_close, assigned_to_id, priority, status,
    root_cause, corrective_action, preventive_action, reason, actions, doc_file, digital_signature,
    last_modified, last_modified_by_id, approved, approved_at, approved_by_id, source_ip,
    root_cause_reference, linked_findings, notes, comments, change_version, is_deleted
FROM capa_cases ORDER BY id DESC";
            var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
            var list = new List<CapaCase>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        public static async Task<CapaCase?> GetCapaCaseByIdAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            const string sql = @"SELECT 
    id, title, description, component_id, date_open, date_close, assigned_to_id, priority, status,
    root_cause, corrective_action, preventive_action, reason, actions, doc_file, digital_signature,
    last_modified, last_modified_by_id, approved, approved_at, approved_by_id, source_ip,
    root_cause_reference, linked_findings, notes, comments, change_version, is_deleted
FROM capa_cases WHERE id=@id LIMIT 1";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            return dt.Rows.Count == 1 ? Map(dt.Rows[0]) : null;
        }

        public static async Task<int> AddCapaCaseAsync(this DatabaseService db, CapaCase c, string signature, string ip, string deviceInfo, string sessionId, int actorUserId, CancellationToken token = default)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            const string sql = @"INSERT INTO capa_cases (title, description, component_id, date_open, date_close, assigned_to_id, priority, status, root_cause, corrective_action, preventive_action, reason, actions, doc_file, digital_signature)
                                VALUES (@t,@d,@cid,@open,@close,@assn,@prio,@status,@rc,@corr,@prev,@reason,@acts,@doc,@sig)";
            var pars = new List<MySqlParameter>
            {
                new("@t", c.Title ?? string.Empty),
                new("@d", c.Description ?? string.Empty),
                new("@cid", c.ComponentId),
                new("@open", c.OpenedAt),
                new("@close", (object?)c.ClosedAt ?? DBNull.Value),
                new("@assn", (object?)c.AssignedToId ?? DBNull.Value),
                new("@prio", c.Priority ?? string.Empty),
                new("@status", c.Status ?? string.Empty),
                new("@rc", c.RootCause ?? string.Empty),
                new("@corr", c.CorrectiveAction ?? string.Empty),
                new("@prev", c.PreventiveAction ?? string.Empty),
                new("@reason", c.Reason ?? string.Empty),
                new("@acts", c.Actions ?? string.Empty),
                new("@doc", c.DocFile ?? string.Empty),
                new("@sig", signature ?? string.Empty)
            };
            await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);
            var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
            c.Id = Convert.ToInt32(idObj);

            await db.LogSystemEventAsync(actorUserId, "CAPA_CREATE", "capa_cases", "CAPA", c.Id, c.Title, ip, "audit", deviceInfo, sessionId, token: token).ConfigureAwait(false);
            return c.Id;
        }

        public static async Task<int> AddCapaCaseAsync(this DatabaseService db, CapaCase c, SignatureMetadataDto? signatureMetadata, string ip, string deviceInfo, string? sessionId, int actorUserId, CancellationToken token = default)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (c == null) throw new ArgumentNullException(nameof(c));

            string signature = signatureMetadata?.Hash ?? c.DigitalSignature ?? string.Empty;
            string effectiveIp = !string.IsNullOrWhiteSpace(signatureMetadata?.IpAddress) ? signatureMetadata!.IpAddress! : ip ?? string.Empty;
            string effectiveDevice = signatureMetadata?.Device ?? deviceInfo ?? string.Empty;
            string effectiveSession = signatureMetadata?.Session ?? sessionId ?? string.Empty;

            c.DigitalSignature = signature;
            c.SourceIp = effectiveIp;

            int id = await db.AddCapaCaseAsync(c, signature, effectiveIp, effectiveDevice, effectiveSession, actorUserId, token).ConfigureAwait(false);

            if (signatureMetadata != null)
            {
                signatureMetadata.Hash ??= signature;
                await PersistSignatureMetadataAsync(db, signatureMetadata, "capa_cases", c.Id, actorUserId, effectiveDevice, effectiveIp, effectiveSession, token).ConfigureAwait(false);
            }

            return id;
        }

        public static Task<int> AddCapaCaseAsync(this DatabaseService db, CapaCase c, SignatureMetadataDto? signatureMetadata, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.AddCapaCaseAsync(c, signatureMetadata, ip, deviceInfo, sessionId, actorUserId: 0, token);

        // Overload without actor parameter (VM convenience)
        public static Task<int> AddCapaCaseAsync(this DatabaseService db, CapaCase c, string signature, string ip, string deviceInfo, string sessionId, CancellationToken token = default)
            => db.AddCapaCaseAsync(c, signature, ip, deviceInfo, sessionId, actorUserId: 0, token);

        public static async Task UpdateCapaCaseAsync(this DatabaseService db, CapaCase c, string signature, string ip, string deviceInfo, string sessionId, int actorUserId, CancellationToken token = default)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            const string sql = @"UPDATE capa_cases SET title=@t, description=@d, component_id=@cid, date_open=@open, date_close=@close, assigned_to_id=@assn, priority=@prio, status=@status, root_cause=@rc, corrective_action=@corr, preventive_action=@prev, reason=@reason, actions=@acts, doc_file=@doc, digital_signature=@sig WHERE id=@id";
            var pars = new List<MySqlParameter>
            {
                new("@t", c.Title ?? string.Empty),
                new("@d", c.Description ?? string.Empty),
                new("@cid", c.ComponentId),
                new("@open", c.OpenedAt),
                new("@close", (object?)c.ClosedAt ?? DBNull.Value),
                new("@assn", (object?)c.AssignedToId ?? DBNull.Value),
                new("@prio", c.Priority ?? string.Empty),
                new("@status", c.Status ?? string.Empty),
                new("@rc", c.RootCause ?? string.Empty),
                new("@corr", c.CorrectiveAction ?? string.Empty),
                new("@prev", c.PreventiveAction ?? string.Empty),
                new("@reason", c.Reason ?? string.Empty),
                new("@acts", c.Actions ?? string.Empty),
                new("@doc", c.DocFile ?? string.Empty),
                new("@sig", signature ?? string.Empty),
                new("@id", c.Id)
            };
            await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);

            await db.LogSystemEventAsync(actorUserId, "CAPA_UPDATE", "capa_cases", "CAPA", c.Id, c.Title, ip, "audit", deviceInfo, sessionId, token: token).ConfigureAwait(false);
        }

        public static async Task UpdateCapaCaseAsync(this DatabaseService db, CapaCase c, SignatureMetadataDto? signatureMetadata, string ip, string deviceInfo, string? sessionId, int actorUserId, CancellationToken token = default)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (c == null) throw new ArgumentNullException(nameof(c));

            string signature = signatureMetadata?.Hash ?? c.DigitalSignature ?? string.Empty;
            string effectiveIp = !string.IsNullOrWhiteSpace(signatureMetadata?.IpAddress) ? signatureMetadata!.IpAddress! : ip ?? string.Empty;
            string effectiveDevice = signatureMetadata?.Device ?? deviceInfo ?? string.Empty;
            string effectiveSession = signatureMetadata?.Session ?? sessionId ?? string.Empty;

            c.DigitalSignature = signature;
            c.SourceIp = effectiveIp;

            await db.UpdateCapaCaseAsync(c, signature, effectiveIp, effectiveDevice, effectiveSession, actorUserId, token).ConfigureAwait(false);

            if (signatureMetadata != null)
            {
                signatureMetadata.Hash ??= signature;
                await PersistSignatureMetadataAsync(db, signatureMetadata, "capa_cases", c.Id, actorUserId, effectiveDevice, effectiveIp, effectiveSession, token).ConfigureAwait(false);
            }
        }

        public static Task UpdateCapaCaseAsync(this DatabaseService db, CapaCase c, SignatureMetadataDto? signatureMetadata, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.UpdateCapaCaseAsync(c, signatureMetadata, ip, deviceInfo, sessionId, actorUserId: 0, token);

        private static async Task PersistSignatureMetadataAsync(
            DatabaseService db,
            SignatureMetadataDto metadata,
            string tableName,
            int recordId,
            int actorUserId,
            string deviceInfo,
            string ip,
            string? sessionId,
            CancellationToken token)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            var signature = new DigitalSignature
            {
                Id = metadata.Id ?? 0,
                TableName = tableName,
                RecordId = recordId,
                UserId = actorUserId,
                SignatureHash = metadata.Hash,
                Method = metadata.Method,
                Status = metadata.Status,
                Note = metadata.Note,
                DeviceInfo = deviceInfo,
                IpAddress = ip,
                SessionId = sessionId,
                SignedAt = DateTime.UtcNow
            };

            var persistedId = await db.InsertDigitalSignatureAsync(signature, token).ConfigureAwait(false);
            if (!metadata.Id.HasValue)
            {
                metadata.Id = persistedId;
            }
        }

        // Overload without actor parameter (VM convenience)
        public static Task UpdateCapaCaseAsync(this DatabaseService db, CapaCase c, string signature, string ip, string deviceInfo, string sessionId, CancellationToken token = default)
            => db.UpdateCapaCaseAsync(c, signature, ip, deviceInfo, sessionId, actorUserId: 0, token);

        // Additional helpers expected by CapaCaseViewModel
        public static Task LogCapaCaseAuditAsync(this DatabaseService db, int capaId, string action, int actorUserId, string ip, string deviceInfo, string? sessionId, string? details, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, $"CAPA_{action}", "capa_cases", "CAPA", capaId, details, ip, "audit", deviceInfo, sessionId, token: token);

        public static Task RollbackCapaCaseAsync(this DatabaseService db, int capaId, string ip, string deviceInfo, string? sessionId, int actorUserId, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "CAPA_ROLLBACK", "capa_cases", "CAPA", capaId, null, ip, "audit", deviceInfo, sessionId, token: token);

        public static Task ExportCapaCasesAsync(this DatabaseService db, System.Collections.Generic.List<CapaCase> items, string ip, string deviceInfo, string? sessionId, int actorUserId, CancellationToken token = default)
        {
            var path = YasGMP.Helpers.CsvExportHelper.WriteCsv(items ?? new System.Collections.Generic.List<CapaCase>(), "capa_cases",
                new (string, System.Func<CapaCase, object?>)[]
                {
                    ("Id", c => c.Id),
                    ("Title", c => c.Title),
                    ("Status", c => c.Status),
                    ("OpenedAt", c => c.OpenedAt),
                    ("ClosedAt", c => c.ClosedAt),
                    ("Priority", c => c.Priority)
                });
            return db.LogSystemEventAsync(actorUserId, "CAPA_EXPORT", "capa_cases", "CAPA", null, $"count={items?.Count ?? 0}; file={path}", ip, "info", deviceInfo, sessionId, token: token);
        }

        // Convenience overload (no audit context)
        public static Task ExportCapaCasesAsync(this DatabaseService db, System.Collections.Generic.List<CapaCase> items, string format, CancellationToken token = default)
            => db.ExportCapaCasesAsync(items, format, actorUserId: 0, ip: string.Empty, deviceInfo: string.Empty, sessionId: null, token: token);

        // Primary overload with explicit format and audit context
        public static Task ExportCapaCasesAsync(this DatabaseService db, System.Collections.Generic.List<CapaCase> items, string format, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
        {
            string? path = null;
            if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
            {
                path = YasGMP.Helpers.XlsxExporter.WriteSheet(items ?? new System.Collections.Generic.List<CapaCase>(), "capa_cases",
                    new (string, System.Func<CapaCase, object?>)[]
                    {
                        ("Id", c => c.Id),
                        ("Title", c => c.Title),
                        ("Status", c => c.Status),
                        ("OpenedAt", c => c.OpenedAt),
                        ("ClosedAt", c => c.ClosedAt),
                        ("Priority", c => c.Priority)
                    });
            }
            else if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
            {
                path = YasGMP.Helpers.PdfExporter.WriteTable(items ?? new System.Collections.Generic.List<CapaCase>(), "capa_cases",
                    new (string, System.Func<CapaCase, object?>)[]
                    {
                        ("Id", c => c.Id),
                        ("Title", c => c.Title),
                        ("Status", c => c.Status),
                        ("OpenedAt", c => c.OpenedAt),
                        ("ClosedAt", c => c.ClosedAt)
                    }, title: "CAPA Cases Export");
            }
            else
            {
                path = YasGMP.Helpers.CsvExportHelper.WriteCsv(items ?? new System.Collections.Generic.List<CapaCase>(), "capa_cases",
                    new (string, System.Func<CapaCase, object?>)[]
                    {
                        ("Id", c => c.Id),
                        ("Title", c => c.Title),
                        ("Status", c => c.Status),
                        ("OpenedAt", c => c.OpenedAt),
                        ("ClosedAt", c => c.ClosedAt),
                        ("Priority", c => c.Priority)
                    });
            }
            return db.LogSystemEventAsync(actorUserId, "CAPA_EXPORT", "capa_cases", "CAPA", null, $"fmt={format}; count={items?.Count ?? 0}; file={path}", ip, "info", deviceInfo, sessionId, token: token);
        }

        // Back-compat overloads expected by ViewModel
        public static Task RollbackCapaCaseAsync(this DatabaseService db, int capaId, string signature, int actorUserId, string ip, string deviceInfo, CancellationToken token = default)
            => db.RollbackCapaCaseAsync(capaId, ip, deviceInfo, sessionId: null, actorUserId: actorUserId, token: token);

        // (Removed legacy duplicate overload)

        public static Task EscalateCapaCaseAsync(this DatabaseService db, int capaId, int actorUserId, string ip, string deviceInfo, string? sessionId, string? reason = null, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "CAPA_ESCALATE", "capa_cases", "CAPA", capaId, reason, ip, "warn", deviceInfo, sessionId, token: token);

        public static Task ApproveCapaCaseAsync(this DatabaseService db, int capaId, int actorUserId, string ip, string deviceInfo, string? sessionId, string? note = null, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "CAPA_APPROVE", "capa_cases", "CAPA", capaId, note, ip, "audit", deviceInfo, sessionId, token: token);

        public static Task RejectCapaCaseAsync(this DatabaseService db, int capaId, int actorUserId, string ip, string deviceInfo, string signatureHash, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "CAPA_REJECT", "capa_cases", "CAPA", capaId, $"sig={signatureHash}", ip, "audit", deviceInfo, null, token: token);

        public static async Task DeleteCapaCaseAsync(this DatabaseService db, int id, int actorUserId, string ip, string deviceInfo, CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync("DELETE FROM capa_cases WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            await db.LogSystemEventAsync(actorUserId, "CAPA_DELETE", "capa_cases", "CAPA", id, null, ip, "audit", deviceInfo, null, token: token).ConfigureAwait(false);
        }

        private static CapaCase Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            int? IN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
            DateTime D(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : DateTime.UtcNow;
            DateTime? DN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

            return new CapaCase
            {
                Id = I("id"),
                Title = S("title"),
                Description = S("description"),
                ComponentId = I("component_id"),
                OpenedAt = D("date_open"),
                ClosedAt = DN("date_close"),
                AssignedToId = IN("assigned_to_id"),
                Priority = S("priority"),
                Status = S("status"),
                RootCause = S("root_cause"),
                CorrectiveAction = S("corrective_action"),
                PreventiveAction = S("preventive_action"),
                Reason = S("reason"),
                Actions = S("actions"),
                DocFile = S("doc_file"),
                DigitalSignature = S("digital_signature"),
                LastModified = DN("last_modified") ?? DateTime.UtcNow,
                LastModifiedById = IN("last_modified_by_id"),
                Approved = r.Table.Columns.Contains("approved") && r["approved"] != DBNull.Value && Convert.ToBoolean(r["approved"]),
                ApprovedAt = DN("approved_at"),
                ApprovedById = IN("approved_by_id"),
                SourceIp = S("source_ip"),
                StatusId = IN("status_id"),
                RootCauseReference = S("root_cause_reference"),
                LinkedFindings = S("linked_findings"),
                Notes = S("notes"),
                Comments = S("comments"),
                ChangeVersion = I("change_version"),
                IsDeleted = r.Table.Columns.Contains("is_deleted") && r["is_deleted"] != DBNull.Value && Convert.ToBoolean(r["is_deleted"])
            };
        }
    }
}


