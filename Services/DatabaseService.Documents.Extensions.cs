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
    public static class DatabaseServiceDocumentsExtensions
    {
        public static async Task<List<SopDocument>> GetAllDocumentsFullAsync(this DatabaseService db, CancellationToken token = default)
        {
            const string sql = @"SELECT 
    id, code, title, description, document_type, file_path, revision, status,
    revision_history, related_case_type, related_case_id, source_ip
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
            // Best-effort append to a CSV/JSON column
            try
            {
                await db.ExecuteNonQueryAsync("UPDATE documentcontrol SET linked_change_controls = CONCAT(IFNULL(linked_change_controls,''), CASE WHEN linked_change_controls IS NULL OR linked_change_controls='' THEN '' ELSE ',' END, @cc) WHERE id=@id",
                    new[] { new MySqlParameter("@cc", changeControlId.ToString()), new MySqlParameter("@id", documentId) }, token).ConfigureAwait(false);
            }
            catch { }
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

            return new SopDocument
            {
                Id = I("id"),
                Code = S("code"),
                Name = S("title"),
                Description = S("description"),
                Process = S("document_type"),
                FilePath = S("file_path"),
                VersionNo = ParseRev(S("revision")),
                Status = S("status"),
                ReviewNotes = S("revision_history"),
                RelatedType = S("related_case_type"),
                RelatedId = I("related_case_id"),
                SourceIp = S("source_ip")
            };
        }
    }
}
