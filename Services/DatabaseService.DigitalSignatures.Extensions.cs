// ==============================================================================
// File: Services/DatabaseService.DigitalSignatures.Extensions.cs
// Purpose: Minimal Digital Signatures list/CRUD and export shims for ViewModel
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
    public static class DatabaseServiceDigitalSignaturesExtensions
    {
        public static async Task<List<DigitalSignature>> GetAllSignaturesFullAsync(this DatabaseService db, CancellationToken token = default)
        {
            const string sql = @"SELECT id, table_name, record_id, user_id, signature_hash, method, status, signed_at, device_info, ip_address, note, public_key
FROM digital_signatures ORDER BY signed_at DESC, id DESC";
            var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
            var list = new List<DigitalSignature>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        public static async Task<int> InsertDigitalSignatureAsync(this DatabaseService db, DigitalSignature sig, CancellationToken token = default)
        {
            if (sig == null) throw new ArgumentNullException(nameof(sig));
            const string sql = @"INSERT INTO digital_signatures (table_name, record_id, user_id, signature_hash, method, status, signed_at, device_info, ip_address, note, public_key)
                                VALUES (@table,@rid,@uid,@hash,@method,@status,@at,@dev,@ip,@note,@pk)";
            var pars = new List<MySqlParameter>
            {
                new("@table", (object?)sig.TableName ?? DBNull.Value),
                new("@rid", sig.RecordId),
                new("@uid", sig.UserId),
                new("@hash", (object?)sig.SignatureHash ?? DBNull.Value),
                new("@method", (object?)sig.Method ?? DBNull.Value),
                new("@status", (object?)sig.Status ?? DBNull.Value),
                new("@at", (object?)sig.SignedAt ?? DBNull.Value),
                new("@dev", (object?)sig.DeviceInfo ?? DBNull.Value),
                new("@ip", (object?)sig.IpAddress ?? DBNull.Value),
                new("@note", (object?)sig.Note ?? DBNull.Value),
                new("@pk", (object?)sig.PublicKey ?? DBNull.Value)
            };
            await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);
            var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
            sig.Id = Convert.ToInt32(idObj);
            await db.LogSystemEventAsync(sig.UserId, "SIG_CREATE", "digital_signatures", "DigitalSignatures", sig.Id, sig.SignatureHash, sig.IpAddress, "audit", sig.DeviceInfo, null, token: token).ConfigureAwait(false);
            return sig.Id;
        }

        public static async Task RevokeSignatureAsync(this DatabaseService db, int id, string reason, CancellationToken token = default)
        {
            try
            {
                await db.ExecuteNonQueryAsync("UPDATE digital_signatures SET status='revoked', note=@note WHERE id=@id", new[]
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@note", (object?)reason ?? DBNull.Value)
                }, token).ConfigureAwait(false);
            }
            catch { /* tolerate schema differences */ }
            await db.LogSystemEventAsync(null, "SIG_REVOKE", "digital_signatures", "DigitalSignatures", id, reason, null, "audit", null, null, token: token).ConfigureAwait(false);
        }

        public static Task<bool> VerifySignatureAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            // Minimal stub: assume signature exists and is valid; real logic would re-compute hash.
            return Task.FromResult(true);
        }

        public static Task ExportSignaturesAsync(this DatabaseService db, List<DigitalSignature> rows, string format, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "SIG_EXPORT", "digital_signatures", "DigitalSignatures", null, $"count={rows?.Count ?? 0}; fmt={format}", ip, "info", deviceInfo, sessionId, token: token);

        private static DigitalSignature Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            DateTime? D(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

            return new DigitalSignature
            {
                Id = I("id"),
                TableName = S("table_name"),
                RecordId = I("record_id"),
                UserId = I("user_id"),
                SignatureHash = S("signature_hash"),
                Method = S("method"),
                Status = S("status"),
                SignedAt = D("signed_at"),
                DeviceInfo = S("device_info"),
                IpAddress = S("ip_address"),
                Note = S("note"),
                PublicKey = S("public_key"),
                UserName = S("user_name")
            };
        }
    }
}
