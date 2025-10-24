// ==============================================================================
// File: Services/DatabaseService.Documents.Extensions.cs
// Purpose: Minimal Document Control shims mapped to `documentcontrol` table
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
    /// DatabaseService extensions for document control records and versions.
    /// </summary>
    public static class DatabaseServiceDocumentsExtensions
    {
        public static async Task<List<SopDocument>> GetAllDocumentsFullAsync(this DatabaseService db, CancellationToken token = default)
        {
            const string sql = @"SELECT
    id,
    code,
    title,
    description,
    file_path,
    revision,
    status,
    linked_change_controls,
    device_info,
    created_at,
    updated_at
FROM documentcontrol ORDER BY id DESC";
            var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
            var list = new List<SopDocument>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        public static async Task<int> InitiateDocumentAsync(this DatabaseService db, string code, string name, string version, string? filePath, int actorUserId, string? notes, CancellationToken token = default)
        {
            const string sql = @"INSERT INTO documentcontrol (code, title, revision, status, file_path, description, device_info)
                                VALUES (@code,@title,@rev,'draft',@file,@desc,@dev)";
            var pars = new List<MySqlParameter>
            {
                new("@code", code ?? string.Empty),
                new("@title", name ?? string.Empty),
                new("@rev", version ?? "1"),
                new("@file", (object?)filePath ?? DBNull.Value),
                new("@desc", (object?)notes ?? DBNull.Value),
                new("@dev", (object?)null ?? DBNull.Value)
            };
            try { await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false); } catch { /* tolerate schema */ }
            var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
            int id = Convert.ToInt32(idObj);
            await db.LogSystemEventAsync(actorUserId, "DOC_INITIATE", "documentcontrol", "DocControl", id, notes, null, "audit", null, null, token: token).ConfigureAwait(false);
            return id;
        }

        public static async Task ReviseDocumentAsync(this DatabaseService db, int documentId, string newVersion, string? newFilePath, int actorUserId, string ip, string deviceInfo, CancellationToken token = default)
        {
            try
            {
                await db.ExecuteNonQueryAsync("UPDATE documentcontrol SET revision=@rev, file_path=@file WHERE id=@id", new[]
                {
                    new MySqlParameter("@rev", newVersion ?? ""),
                    new MySqlParameter("@file", (object?)newFilePath ?? DBNull.Value),
                    new MySqlParameter("@id", documentId)
                }, token).ConfigureAwait(false);
            }
            catch { }
            await db.LogSystemEventAsync(actorUserId, "DOC_REVISE", "documentcontrol", "DocControl", documentId, $"rev={newVersion}", ip, "audit", deviceInfo, null, token: token).ConfigureAwait(false);
        }

        public static Task AssignDocumentAsync(this DatabaseService db, int documentId, int userId, string? note, int actorUserId, string ip, string device, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "DOC_ASSIGN", "documentcontrol", "DocControl", documentId, $"user={userId}; note={note}", ip, "audit", device, null, token: token);

        public static async Task ApproveDocumentAsync(this DatabaseService db, int documentId, int approverUserId, string ip, string deviceInfo, string? signatureHash, CancellationToken token = default)
        {
            try { await db.ExecuteNonQueryAsync("UPDATE documentcontrol SET status='approved' WHERE id=@id", new[] { new MySqlParameter("@id", documentId) }, token).ConfigureAwait(false); } catch { }
            await db.LogSystemEventAsync(approverUserId, "DOC_APPROVE", "documentcontrol", "DocControl", documentId, signatureHash, ip, "audit", deviceInfo, null, token: token).ConfigureAwait(false);
        }

        public static async Task PublishDocumentAsync(this DatabaseService db, int documentId, int actorUserId, string ip, string deviceInfo, CancellationToken token = default)
        {
            try { await db.ExecuteNonQueryAsync("UPDATE documentcontrol SET status='published' WHERE id=@id", new[] { new MySqlParameter("@id", documentId) }, token).ConfigureAwait(false); } catch { }
            await db.LogSystemEventAsync(actorUserId, "DOC_PUBLISH", "documentcontrol", "DocControl", documentId, null, ip, "audit", deviceInfo, null, token: token).ConfigureAwait(false);
        }

        public static async Task ExpireDocumentAsync(this DatabaseService db, int documentId, int actorUserId, string ip, string deviceInfo, CancellationToken token = default)
        {
            try { await db.ExecuteNonQueryAsync("UPDATE documentcontrol SET status='expired' WHERE id=@id", new[] { new MySqlParameter("@id", documentId) }, token).ConfigureAwait(false); } catch { }
            await db.LogSystemEventAsync(actorUserId, "DOC_EXPIRE", "documentcontrol", "DocControl", documentId, null, ip, "audit", deviceInfo, null, token: token).ConfigureAwait(false);
        }

        public static async Task LinkChangeControlToDocumentAsync(this DatabaseService db, int documentId, int changeControlId, int actorUserId, string ip, string device, CancellationToken token = default)
        {
            if (documentId <= 0) throw new ArgumentOutOfRangeException(nameof(documentId));
            if (changeControlId <= 0) throw new ArgumentOutOfRangeException(nameof(changeControlId));

            var docExists = await db.ExecuteScalarAsync(
                "SELECT 1 FROM documentcontrol WHERE id=@docId LIMIT 1",
                new[] { new MySqlParameter("@docId", documentId) },
                token).ConfigureAwait(false);

            if (docExists == null || docExists == DBNull.Value)
                throw new DocumentControlLinkException("Document not found. Please refresh and try again.");

            var changeControlExists = await db.ExecuteScalarAsync(
                "SELECT 1 FROM change_controls WHERE id=@ccId LIMIT 1",
                new[] { new MySqlParameter("@ccId", changeControlId) },
                token).ConfigureAwait(false);

            if (changeControlExists == null || changeControlExists == DBNull.Value)
                throw new DocumentControlLinkException("Change control not found. It may have been removed.");

            bool persisted = false;
            Exception? primaryFailure = null;

            try
            {
                const string ensureSql = @"CREATE TABLE IF NOT EXISTS document_change_controls (
    document_id INT NOT NULL,
    change_control_id INT NOT NULL,
    linked_by INT NULL,
    linked_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (document_id, change_control_id),
    INDEX idx_dcc_document (document_id),
    INDEX idx_dcc_change (change_control_id),
    CONSTRAINT fk_dcc_document FOREIGN KEY (document_id) REFERENCES documentcontrol(id) ON DELETE CASCADE,
    CONSTRAINT fk_dcc_change FOREIGN KEY (change_control_id) REFERENCES change_controls(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";

                await db.ExecuteNonQueryAsync(ensureSql, null, token).ConfigureAwait(false);

                const string insertSql = @"INSERT INTO document_change_controls (document_id, change_control_id, linked_by)
VALUES (@docId, @ccId, @actor)
ON DUPLICATE KEY UPDATE linked_at = CURRENT_TIMESTAMP, linked_by = @actor";

                var insertPars = new[]
                {
                    new MySqlParameter("@docId", documentId),
                    new MySqlParameter("@ccId", changeControlId),
                    new MySqlParameter("@actor", actorUserId == 0 ? (object)DBNull.Value : actorUserId)
                };

                await db.ExecuteNonQueryAsync(insertSql, insertPars, token).ConfigureAwait(false);
                persisted = true;
            }
            catch (Exception ex)
            {
                primaryFailure = ex;
            }

            if (!persisted)
            {
                try
                {
                    await db.ExecuteNonQueryAsync(
                        "UPDATE documentcontrol SET linked_change_controls = CONCAT(IFNULL(linked_change_controls,''), CASE WHEN linked_change_controls IS NULL OR linked_change_controls='' THEN '' ELSE ',' END, @cc) WHERE id=@id",
                        new[]
                        {
                            new MySqlParameter("@cc", changeControlId.ToString()),
                            new MySqlParameter("@id", documentId)
                        },
                        token).ConfigureAwait(false);
                    persisted = true;
                }
                catch (Exception fallbackEx)
                {
                    if (primaryFailure != null)
                        throw new AggregateException("Failed to link change control to document.", primaryFailure, fallbackEx);

                    throw;
                }
            }

            if (!persisted && primaryFailure != null)
            {
                throw new InvalidOperationException("Unable to persist change control link.", primaryFailure);
            }

            await db.LogSystemEventAsync(actorUserId, "DOC_LINK_CHANGE", "documentcontrol", "DocControl", documentId, $"cc={changeControlId}", ip, "audit", device, null, token: token).ConfigureAwait(false);
        }

        public static async Task<string> ExportDocumentsAsync(this DatabaseService db, List<SopDocument> rows, string format, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
        {
            string path;
            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                path = YasGMP.Helpers.CsvExportHelper.WriteCsv(rows ?? new List<SopDocument>(), "documents",
                    new (string, Func<SopDocument, object?>)[]
                    {
                        ("Id", d => d.Id),
                        ("Code", d => d.Code),
                        ("Name", d => d.Name),
                        ("Status", d => d.Status),
                        ("VersionNo", d => d.VersionNo),
                        ("FilePath", d => d.FilePath)
                    });
            }
            else if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
            {
                path = YasGMP.Helpers.XlsxExporter.WriteSheet(rows ?? new List<SopDocument>(), "documents",
                    new (string, Func<SopDocument, object?>)[]
                    {
                        ("Id", d => d.Id),
                        ("Code", d => d.Code),
                        ("Name", d => d.Name),
                        ("Status", d => d.Status),
                        ("VersionNo", d => d.VersionNo),
                        ("FilePath", d => d.FilePath)
                    });
            }
            else if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
            {
                path = YasGMP.Helpers.PdfExporter.WriteTable(rows ?? new List<SopDocument>(), "documents",
                    new (string, Func<SopDocument, object?>)[]
                    {
                        ("Id", d => d.Id),
                        ("Code", d => d.Code),
                        ("Name", d => d.Name),
                        ("Status", d => d.Status),
                        ("VersionNo", d => d.VersionNo)
                    }, title: "Documents Export");
            }
            else
            {
                // For non-CSV formats, log intent and synthesize a human-friendly path placeholder
                path = System.IO.Path.Combine(YasGMP.Helpers.CsvExportHelper.EnsureExportDirectory(), $"documents_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{format}");
                try { System.IO.File.WriteAllText(path, "Export not implemented for this format yet."); } catch { }
            }
            await db.LogSystemEventAsync(actorUserId, "DOC_EXPORT", "documentcontrol", "DocControl", null, $"count={rows?.Count ?? 0}; file={path}", ip, "info", deviceInfo, sessionId, token: token).ConfigureAwait(false);
            return path;
        }

        public static Task LogDocumentAuditAsync(this DatabaseService db, int documentId, string action, int actorUserId, string? description, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, $"DOC_{action}", "documentcontrol", "DocControl", documentId == 0 ? null : documentId, description, ip, "audit", deviceInfo, sessionId, token: token);

        private static SopDocument Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            int ParseRev(string s) => int.TryParse(s, out var v) ? v : 0;

            DateTime D(string c)
            {
                if (!r.Table.Columns.Contains(c) || r[c] == DBNull.Value) return DateTime.UtcNow;
                if (r[c] is DateTime dt) return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                return DateTime.TryParse(r[c]?.ToString(), out var parsed) ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc) : DateTime.UtcNow;
            }

            string description = S("description");
            string linkedChangeControls = S("linked_change_controls");
            string status = S("status");
            string deviceInfo = S("device_info");

            DateTime createdAt = D("created_at");
            DateTime updatedAt = createdAt;
            if (r.Table.Columns.Contains("updated_at") && r["updated_at"] != DBNull.Value)
                updatedAt = D("updated_at");

            return new SopDocument
            {
                Id = I("id"),
                Code = S("code"),
                Name = S("title"),
                Description = description,
                FilePath = S("file_path"),
                VersionNo = ParseRev(S("revision")),
                Status = string.IsNullOrWhiteSpace(status) ? "draft" : status,
                ReviewNotes = string.IsNullOrWhiteSpace(description) ? linkedChangeControls : description,
                Comment = linkedChangeControls,
                DateIssued = createdAt,
                LastModified = updatedAt,
                SourceIp = deviceInfo,
                Process = string.Empty,
                RelatedType = string.Empty,
                RelatedId = null
            };
        }
    }
}

