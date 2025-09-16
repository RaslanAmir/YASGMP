// ==============================================================================
// File: Services/DatabaseService.Sop.Extensions.cs
// Purpose: Extension helpers for SOP document access used by SopViewModel.
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Extension methods over <see cref="DatabaseService"/> for interacting with the
    /// legacy <c>sop_documents</c> and <c>sop_document_log</c> tables.
    /// </summary>
    public static class DatabaseServiceSopExtensions
    {
        /// <summary>Returns all SOP documents ordered by <c>updated_at</c> (desc).</summary>
        public static async Task<List<SopDocument>> GetSopDocumentsAsync(
            this DatabaseService db,
            CancellationToken token = default)
        {
            if (db is null) throw new ArgumentNullException(nameof(db));

            const string sql = @"
SELECT
    id, code, name, version, status, created_by, created_at, approved_by, approved_at,
    file_path, notes, updated_at, description, process, language, date_issued,
    date_expiry, next_review_date, attachments, responsible_user_id, responsible_user,
    created_by_id, version_no, file_hash, digital_signature, chain_hash, approver_ids,
    approvers, approval_timestamps, review_notes, pdf_metadata, related_type, related_id,
    comment, last_modified, last_modified_by_id, last_modified_by, source_ip, ai_tags
FROM sop_documents
ORDER BY COALESCE(updated_at, last_modified) DESC, id DESC;";

            var table = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
            var list = new List<SopDocument>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                list.Add(Map(row));
            }

            return list;
        }

        /// <summary>Creates a new SOP document and returns the inserted identifier.</summary>
        public static async Task<int> CreateSopDocumentAsync(
            this DatabaseService db,
            SopDocument document,
            int actorUserId,
            string ipAddress,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            if (db is null) throw new ArgumentNullException(nameof(db));
            if (document is null) throw new ArgumentNullException(nameof(document));

            var now = DateTime.UtcNow;
            var issued = document.DateIssued == default ? now : document.DateIssued;
            var modified = document.LastModified == default ? now : document.LastModified;
            var expiry = document.DateExpiry;
            var review = document.NextReviewDate;
            var responsibleId = document.ResponsibleUserId == 0 ? (actorUserId == 0 ? (int?)null : actorUserId) : document.ResponsibleUserId;
            var createdById = document.CreatedById.HasValue && document.CreatedById.Value != 0 ? document.CreatedById : (actorUserId == 0 ? null : actorUserId);

            string attachments = ToDelimited(document.Attachments);
            string approverIds = ToDelimited(document.ApproverIds);
            string approvers = ToDelimited(document.Approvers?.Select(u => u?.FullName ?? u?.Username ?? string.Empty));
            string approvalTs = ToDelimited(document.ApprovalTimestamps?.Select(t => t.ToString("o", CultureInfo.InvariantCulture)));

            const string sql = @"
INSERT INTO sop_documents
    (code, name, version, status, file_path, description, process, language,
     date_issued, date_expiry, next_review_date, attachments, responsible_user_id,
     created_by_id, version_no, file_hash, digital_signature, chain_hash,
     approver_ids, approvers, approval_timestamps, review_notes, pdf_metadata,
     related_type, related_id, comment, last_modified, last_modified_by_id,
     source_ip, ai_tags)
VALUES
    (@code,@name,@version,@status,@file,@description,@process,@language,
     @issued,@expiry,@review,@attachments,@responsible,@createdBy,
     @versionNo,@fileHash,@digital,@chain,@approverIds,@approvers,@approvalTs,
     @reviewNotes,@metadata,@relatedType,@relatedId,@comment,@lastModified,
     @modifiedBy,@sourceIp,@aiTags);";

            var parameters = new List<MySqlParameter>
            {
                new("@code", document.Code ?? string.Empty),
                new("@name", document.Name ?? string.Empty),
                new("@version", (object?)(document.VersionNo > 0 ? document.VersionNo.ToString(CultureInfo.InvariantCulture) : null) ?? DBNull.Value),
                new("@status", document.Status ?? "draft"),
                new("@file", (object?)document.FilePath ?? DBNull.Value),
                new("@description", (object?)document.Description ?? DBNull.Value),
                new("@process", (object?)document.Process ?? DBNull.Value),
                new("@language", (object?)document.Language ?? DBNull.Value),
                new("@issued", issued),
                new("@expiry", (object?)expiry ?? DBNull.Value),
                new("@review", (object?)review ?? DBNull.Value),
                new("@attachments", (object?)attachments ?? DBNull.Value),
                new("@responsible", (object?)responsibleId ?? DBNull.Value),
                new("@createdBy", (object?)createdById ?? DBNull.Value),
                new("@versionNo", document.VersionNo > 0 ? document.VersionNo : 1),
                new("@fileHash", (object?)document.FileHash ?? DBNull.Value),
                new("@digital", (object?)document.DigitalSignature ?? DBNull.Value),
                new("@chain", (object?)document.ChainHash ?? DBNull.Value),
                new("@approverIds", (object?)approverIds ?? DBNull.Value),
                new("@approvers", (object?)approvers ?? DBNull.Value),
                new("@approvalTs", (object?)approvalTs ?? DBNull.Value),
                new("@reviewNotes", (object?)(document.ReviewNotes ?? document.Comment) ?? DBNull.Value),
                new("@metadata", (object?)document.PdfMetadata ?? DBNull.Value),
                new("@relatedType", (object?)document.RelatedType ?? DBNull.Value),
                new("@relatedId", (object?)document.RelatedId ?? DBNull.Value),
                new("@comment", (object?)document.Comment ?? DBNull.Value),
                new("@lastModified", modified),
                new("@modifiedBy", actorUserId == 0 ? (object)DBNull.Value : actorUserId),
                new("@sourceIp", (object?)(document.SourceIp ?? ipAddress) ?? DBNull.Value),
                new("@aiTags", (object?)document.AiTags ?? DBNull.Value)
            };

            await db.ExecuteNonQueryAsync(sql, parameters, token).ConfigureAwait(false);
            var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID();", null, token).ConfigureAwait(false);
            int newId = Convert.ToInt32(idObj, CultureInfo.InvariantCulture);

            string note = $"Created SOP {document.Code ?? newId.ToString(CultureInfo.InvariantCulture)}";
            await db.LogSystemEventAsync(actorUserId == 0 ? null : actorUserId, "SOP_CREATE", "sop_documents", "SopDocuments", newId, note, ipAddress, "audit", deviceInfo, sessionId, token: token).ConfigureAwait(false);
            await db.LogSopDocumentAuditAsync(newId, "create", actorUserId, note, ipAddress, deviceInfo, sessionId, token).ConfigureAwait(false);

            return newId;
        }

        /// <summary>Updates an existing SOP document.</summary>
        public static async Task UpdateSopDocumentAsync(
            this DatabaseService db,
            SopDocument document,
            int actorUserId,
            string ipAddress,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            if (db is null) throw new ArgumentNullException(nameof(db));
            if (document is null) throw new ArgumentNullException(nameof(document));

            var modified = document.LastModified == default ? DateTime.UtcNow : document.LastModified;
            var expiry = document.DateExpiry;
            var review = document.NextReviewDate;

            string attachments = ToDelimited(document.Attachments);
            string approverIds = ToDelimited(document.ApproverIds);
            string approvers = ToDelimited(document.Approvers?.Select(u => u?.FullName ?? u?.Username ?? string.Empty));
            string approvalTs = ToDelimited(document.ApprovalTimestamps?.Select(t => t.ToString("o", CultureInfo.InvariantCulture)));

            const string sql = @"
UPDATE sop_documents
SET code=@code,
    name=@name,
    version=@version,
    status=@status,
    file_path=@file,
    description=@description,
    process=@process,
    language=@language,
    date_issued=@issued,
    date_expiry=@expiry,
    next_review_date=@review,
    attachments=@attachments,
    responsible_user_id=@responsible,
    version_no=@versionNo,
    file_hash=@fileHash,
    digital_signature=@digital,
    chain_hash=@chain,
    approver_ids=@approverIds,
    approvers=@approvers,
    approval_timestamps=@approvalTs,
    review_notes=@reviewNotes,
    pdf_metadata=@metadata,
    related_type=@relatedType,
    related_id=@relatedId,
    comment=@comment,
    last_modified=@lastModified,
    last_modified_by_id=@modifiedBy,
    source_ip=@sourceIp,
    ai_tags=@aiTags
WHERE id=@id;";

            var parameters = new List<MySqlParameter>
            {
                new("@code", document.Code ?? string.Empty),
                new("@name", document.Name ?? string.Empty),
                new("@version", (object?)(document.VersionNo > 0 ? document.VersionNo.ToString(CultureInfo.InvariantCulture) : null) ?? DBNull.Value),
                new("@status", document.Status ?? "draft"),
                new("@file", (object?)document.FilePath ?? DBNull.Value),
                new("@description", (object?)document.Description ?? DBNull.Value),
                new("@process", (object?)document.Process ?? DBNull.Value),
                new("@language", (object?)document.Language ?? DBNull.Value),
                new("@issued", document.DateIssued == default ? DateTime.UtcNow : document.DateIssued),
                new("@expiry", (object?)expiry ?? DBNull.Value),
                new("@review", (object?)review ?? DBNull.Value),
                new("@attachments", (object?)attachments ?? DBNull.Value),
                new("@responsible", document.ResponsibleUserId == 0 ? (object)DBNull.Value : document.ResponsibleUserId),
                new("@versionNo", document.VersionNo > 0 ? document.VersionNo : 1),
                new("@fileHash", (object?)document.FileHash ?? DBNull.Value),
                new("@digital", (object?)document.DigitalSignature ?? DBNull.Value),
                new("@chain", (object?)document.ChainHash ?? DBNull.Value),
                new("@approverIds", (object?)approverIds ?? DBNull.Value),
                new("@approvers", (object?)approvers ?? DBNull.Value),
                new("@approvalTs", (object?)approvalTs ?? DBNull.Value),
                new("@reviewNotes", (object?)document.ReviewNotes ?? DBNull.Value),
                new("@metadata", (object?)document.PdfMetadata ?? DBNull.Value),
                new("@relatedType", (object?)document.RelatedType ?? DBNull.Value),
                new("@relatedId", (object?)document.RelatedId ?? DBNull.Value),
                new("@comment", (object?)document.Comment ?? DBNull.Value),
                new("@lastModified", modified),
                new("@modifiedBy", actorUserId == 0 ? (object)DBNull.Value : actorUserId),
                new("@sourceIp", (object?)(document.SourceIp ?? ipAddress) ?? DBNull.Value),
                new("@aiTags", (object?)document.AiTags ?? DBNull.Value),
                new("@id", document.Id)
            };

            await db.ExecuteNonQueryAsync(sql, parameters, token).ConfigureAwait(false);

            string note = $"Updated SOP {document.Code ?? document.Id.ToString(CultureInfo.InvariantCulture)}";
            await db.LogSystemEventAsync(actorUserId == 0 ? null : actorUserId, "SOP_UPDATE", "sop_documents", "SopDocuments", document.Id, note, ipAddress, "audit", deviceInfo, sessionId, token: token).ConfigureAwait(false);
            await db.LogSopDocumentAuditAsync(document.Id, "update", actorUserId, note, ipAddress, deviceInfo, sessionId, token).ConfigureAwait(false);
        }

        /// <summary>Deletes a SOP document (hard delete).</summary>
        public static async Task DeleteSopDocumentAsync(
            this DatabaseService db,
            int documentId,
            int actorUserId,
            string ipAddress,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            if (db is null) throw new ArgumentNullException(nameof(db));

            await db.ExecuteNonQueryAsync("DELETE FROM sop_documents WHERE id=@id", new[] { new MySqlParameter("@id", documentId) }, token).ConfigureAwait(false);

            string note = $"Deleted SOP {documentId}";
            await db.LogSystemEventAsync(actorUserId == 0 ? null : actorUserId, "SOP_DELETE", "sop_documents", "SopDocuments", documentId, note, ipAddress, "audit", deviceInfo, sessionId, token: token).ConfigureAwait(false);
            await db.LogSopDocumentAuditAsync(documentId, "archive", actorUserId, note, ipAddress, deviceInfo, sessionId, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes an entry to <c>sop_document_log</c>. Falls back to a system event if the audit table is missing.
        /// </summary>
        public static async Task LogSopDocumentAuditAsync(
            this DatabaseService db,
            int documentId,
            string action,
            int actorUserId,
            string? note,
            string ipAddress,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            if (db is null) throw new ArgumentNullException(nameof(db));

            try
            {
                const string sql = @"
INSERT INTO sop_document_log (sop_document_id, action, performed_by, note)
VALUES (@docId,@action,@userId,@note);";

                var pars = new[]
                {
                    new MySqlParameter("@docId", documentId == 0 ? (object)DBNull.Value : documentId),
                    new MySqlParameter("@action", action ?? "update"),
                    new MySqlParameter("@userId", actorUserId == 0 ? (object)DBNull.Value : actorUserId),
                    new MySqlParameter("@note", (object?)note ?? DBNull.Value)
                };

                await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);
            }
            catch (MySqlException)
            {
                await db.LogSystemEventAsync(actorUserId == 0 ? null : actorUserId, $"SOP_{(action ?? "AUDIT").ToUpperInvariant()}", "sop_documents", "SopDocuments", documentId == 0 ? null : documentId, note, ipAddress, "audit", deviceInfo, sessionId, token: token).ConfigureAwait(false);
            }
        }

        private static SopDocument Map(DataRow row)
        {
            string GetString(string column)
                => row.Table.Columns.Contains(column) && row[column] != DBNull.Value
                    ? row[column]?.ToString() ?? string.Empty
                    : string.Empty;

            int GetInt(string column)
                => row.Table.Columns.Contains(column) && row[column] != DBNull.Value
                    ? Convert.ToInt32(row[column], CultureInfo.InvariantCulture)
                    : 0;

            int? GetNullableInt(string column)
                => row.Table.Columns.Contains(column) && row[column] != DBNull.Value
                    ? Convert.ToInt32(row[column], CultureInfo.InvariantCulture)
                    : null;

            DateTime? GetDate(string column)
            {
                if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
                {
                    return null;
                }

                if (row[column] is DateTime dt)
                {
                    return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                }

                if (DateTime.TryParse(row[column]?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
                {
                    return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
                }

                if (DateTime.TryParse(row[column]?.ToString(), out parsed))
                {
                    return parsed;
                }

                return null;
            }

            List<string> SplitStrings(string column)
            {
                var raw = GetString(column);
                if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
                return raw
                    .Split(new[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
            }

            List<int> SplitInts(string column)
            {
                var raw = SplitStrings(column);
                var list = new List<int>(raw.Count);
                foreach (var item in raw)
                {
                    if (int.TryParse(item, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                    {
                        list.Add(value);
                    }
                }

                return list;
            }

            List<DateTime> SplitDates(string column)
            {
                var raw = SplitStrings(column);
                var list = new List<DateTime>(raw.Count);
                foreach (var item in raw)
                {
                    if (DateTime.TryParse(item, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var value))
                    {
                        list.Add(DateTime.SpecifyKind(value, DateTimeKind.Utc));
                    }
                    else if (DateTime.TryParse(item, out value))
                    {
                        list.Add(value);
                    }
                }

                return list;
            }

            var doc = new SopDocument
            {
                Id = GetInt("id"),
                Code = GetString("code"),
                Name = GetString("name"),
                Description = string.IsNullOrWhiteSpace(GetString("description")) ? GetString("notes") : GetString("description"),
                Process = GetString("process"),
                Language = GetString("language"),
                DateIssued = GetDate("date_issued") ?? default,
                DateExpiry = GetDate("date_expiry"),
                NextReviewDate = GetDate("next_review_date"),
                FilePath = GetString("file_path"),
                Attachments = SplitStrings("attachments"),
                ResponsibleUserId = GetInt("responsible_user_id"),
                CreatedById = GetNullableInt("created_by_id"),
                VersionNo = GetInt("version_no"),
                FileHash = GetString("file_hash"),
                Status = GetString("status"),
                DigitalSignature = GetString("digital_signature"),
                ChainHash = GetString("chain_hash"),
                ApproverIds = SplitInts("approver_ids"),
                ApprovalTimestamps = SplitDates("approval_timestamps"),
                ReviewNotes = !string.IsNullOrWhiteSpace(GetString("review_notes")) ? GetString("review_notes") : GetString("notes"),
                PdfMetadata = GetString("pdf_metadata"),
                RelatedType = GetString("related_type"),
                RelatedId = GetNullableInt("related_id"),
                Comment = GetString("comment"),
                LastModified = GetDate("last_modified") ?? GetDate("updated_at") ?? default,
                LastModifiedById = GetInt("last_modified_by_id"),
                SourceIp = GetString("source_ip"),
                AiTags = GetString("ai_tags")
            };

            if (doc.VersionNo <= 0)
            {
                var versionString = GetString("version");
                if (int.TryParse(versionString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedVersion))
                {
                    doc.VersionNo = parsedVersion;
                }
                else
                {
                    doc.VersionNo = 1;
                }
            }

            return doc;
        }

        private static string ToDelimited(IEnumerable<string>? values)
        {
            if (values == null) return string.Empty;
            var list = values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).ToArray();
            return list.Length == 0 ? string.Empty : string.Join(",", list);
        }

        private static string ToDelimited(IEnumerable<int>? values)
        {
            if (values == null) return string.Empty;
            var list = values.ToArray();
            return list.Length == 0 ? string.Empty : string.Join(",", list);
        }

        private static string ToDelimited(IEnumerable<DateTime>? values)
        {
            if (values == null) return string.Empty;
            var list = values.Select(v => v.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)).ToArray();
            return list.Length == 0 ? string.Empty : string.Join(",", list);
        }
    }
}