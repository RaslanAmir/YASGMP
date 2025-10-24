// ==============================================================================
// File: Services/DatabaseService.Attachments.Extensions.cs
// Purpose: Attachments minimal APIs: filter, add, approve, delete, export log
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// DatabaseService extensions for attachment CRUD, linking, and metadata lookups.
    /// </summary>
    public static class DatabaseServiceAttachmentsExtensions
    {
        public static async Task<List<Attachment>> GetAttachmentsFilteredAsync(this DatabaseService db, string? entityFilter, string? typeFilter, string? searchTerm, CancellationToken token = default)
        {
            string sql = @"SELECT a.id,
                                   a.file_name,
                                   a.file_path,
                                   a.entity_type,
                                   a.entity_id,
                                   a.file_type,
                                   a.notes,
                                   a.uploaded_by_id,
                                   a.uploaded_at,
                                   a.file_size,
                                   a.sha256,
                                   a.status,
                                   rp.policy_name       AS retention_policy_name,
                                   rp.retain_until      AS retention_retain_until,
                                   rp.legal_hold        AS retention_legal_hold,
                                   rp.review_required   AS retention_review_required,
                                   rp.notes             AS retention_notes
                            FROM attachments a
                            LEFT JOIN (
                                SELECT rp1.*
                                FROM retention_policies rp1
                                WHERE rp1.id = (
                                    SELECT rp2.id
                                    FROM retention_policies rp2
                                    WHERE rp2.attachment_id = rp1.attachment_id
                                    ORDER BY rp2.created_at DESC, rp2.id DESC
                                    LIMIT 1
                                )
                            ) rp ON rp.attachment_id = a.id
                            WHERE 1=1";
            var pars = new List<MySqlParameter>();
            if (!string.IsNullOrWhiteSpace(entityFilter)) { sql += " AND entity_type LIKE @e"; pars.Add(new MySqlParameter("@e", "%" + entityFilter + "%")); }
            if (!string.IsNullOrWhiteSpace(typeFilter))   { sql += " AND file_type LIKE @t"; pars.Add(new MySqlParameter("@t", "%" + typeFilter + "%")); }
            if (!string.IsNullOrWhiteSpace(searchTerm))   { sql += " AND (file_name LIKE @s OR notes LIKE @s)"; pars.Add(new MySqlParameter("@s", "%" + searchTerm + "%")); }
            sql += " ORDER BY a.created_at DESC, a.id DESC";

            var dt = await db.ExecuteSelectAsync(sql, pars, token).ConfigureAwait(false);
            var list = new List<Attachment>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        public static async Task<int> AddAttachmentAsync(this DatabaseService db, string filePath, string entity, int entityId, int actorUserId, string ip, string device, string sessionId, string? reason = null, IAttachmentService? attachmentService = null, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must be provided", nameof(filePath));

            attachmentService ??= ServiceLocator.GetRequiredService<IAttachmentService>();
            await using var stream = File.OpenRead(filePath);
            var upload = await attachmentService.UploadAsync(stream, new AttachmentUploadRequest
            {
                FileName = Path.GetFileName(filePath) ?? string.Empty,
                ContentType = null,
                EntityType = entity ?? string.Empty,
                EntityId = entityId,
                UploadedById = actorUserId == 0 ? (int?)null : actorUserId,
                Notes = sessionId,
                Reason = reason ?? $"db:{entity}",
                SourceIp = ip,
                SourceHost = device
            }, token).ConfigureAwait(false);

            await db.LogSystemEventAsync(
                actorUserId,
                "ATTACHMENT_CREATE",
                "attachments",
                entity,
                upload.Attachment.Id,
                upload.Attachment.FileName,
                ip,
                "audit",
                device,
                sessionId,
                token: token).ConfigureAwait(false);

            return upload.Attachment.Id;
        }

        public static async Task DeleteAttachmentAsync(this DatabaseService db, int id, int actorUserId, string ip, string device, string sessionId, CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync("DELETE FROM attachments WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            await db.LogSystemEventAsync(actorUserId, "ATTACHMENT_DELETE", "attachments", null, id, null, ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
        }

        // Back-compat overload used by AttachmentViewModel
        public static Task DeleteAttachmentAsync(this DatabaseService db, int id, CancellationToken token = default)
            => db.DeleteAttachmentAsync(id, actorUserId: 0, ip: string.Empty, device: string.Empty, sessionId: string.Empty, token: token);

        public static Task ApproveAttachmentAsync(this DatabaseService db, int attachmentId, int actorUserId, string ip, string deviceInfo, string signatureHash, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "ATTACHMENT_APPROVE", "attachments", null, attachmentId, $"sig={signatureHash}", ip, "audit", deviceInfo, null, token: token);

        public static async Task<int> SaveExportPrintLogAsync(this DatabaseService db, int userId, string format, string tableName, string filterUsed, string filePath, string sourceIp, string? note = null, CancellationToken token = default)
        {
            await db.LogSystemEventAsync(userId, "EXPORT", tableName, "ExportModule", null, $"fmt={format}; file={filePath}; filter={filterUsed}; note={note}", sourceIp, "info", null, null, token: token).ConfigureAwait(false);
            return 1;
        }

        // Overload signatures expected by AttachmentViewModel (named args: relatedTable/relatedId/fileType/search)
        public static Task<List<Attachment>> GetAttachmentsFilteredAsync(this DatabaseService db, string? relatedTable, int? relatedId, string? fileType, string? search, CancellationToken token = default)
            => db.GetAttachmentsFilteredAsync(entityFilter: relatedTable, typeFilter: fileType, searchTerm: search, token);

        public static Task<int> AddAttachmentAsync(this DatabaseService db, string relatedTable, int relatedId, string fileName, string filePath, CancellationToken token = default)
            => db.AddAttachmentAsync(filePath: filePath, entity: relatedTable, entityId: relatedId, actorUserId: 0, ip: string.Empty, device: string.Empty, sessionId: string.Empty, reason: $"ui:{relatedTable}", token: token);

        private static Attachment Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            long? L(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt64(r[c]) : (long?)null;
            bool B(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value && Convert.ToBoolean(r[c]);
            DateTime? D(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

            return new Attachment
            {
                Id = I("id"),
                FileName = S("file_name"),
                FilePath = S("file_path"),
                EntityType = S("entity_type"),
                EntityId = I("entity_id"),
                FileType = S("file_type"),
                Notes = S("notes"),
                UploadedById = I("uploaded_by_id"),
                UploadedAt = D("uploaded_at") ?? DateTime.UtcNow,
                FileSize = L("file_size"),
                Sha256 = S("sha256"),
                Status = S("status"),
                RetentionPolicyName = S("retention_policy_name"),
                RetainUntil = D("retention_retain_until"),
                RetentionLegalHold = B("retention_legal_hold"),
                RetentionReviewRequired = B("retention_review_required"),
                RetentionNotes = S("retention_notes")
            };
        }

        // -------------------- documents/document_links helpers (DocumentService compatible) --------------------
        public static async Task<DataTable> GetDocumentsForEntityAsync(
            this DatabaseService db,
            string entityType,
            int entityId,
            CancellationToken token = default)
        {
            const string sql = @"SELECT d.id, d.file_name, d.storage_provider, d.storage_path, d.content_type, d.sha256, d.uploaded_by,
       dl.entity_type, dl.entity_id
FROM document_links dl
JOIN documents d ON d.id = dl.document_id
WHERE dl.entity_type=@et AND dl.entity_id=@eid
ORDER BY d.id DESC";
            var pars = new[] { new MySqlParameter("@et", entityType), new MySqlParameter("@eid", entityId) };
            return await db.ExecuteSelectAsync(sql, pars, token).ConfigureAwait(false);
        }

        public static async Task DeleteDocumentLinkAsync(
            this DatabaseService db,
            string entityType,
            int entityId,
            int documentId,
            int? actorUserId = null,
            string? ip = null,
            string? device = null,
            string? sessionId = null,
            CancellationToken token = default)
        {
            const string sql = "DELETE FROM document_links WHERE entity_type=@et AND entity_id=@eid AND document_id=@doc";
            var pars = new[] { new MySqlParameter("@et", entityType), new MySqlParameter("@eid", entityId), new MySqlParameter("@doc", documentId) };
            await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);
            await db.LogSystemEventAsync(actorUserId, "ATTACHMENT_DELETE", "document_links", entityType, documentId, $"unlink entity={entityType}/{entityId}", ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
        }
    }
}

