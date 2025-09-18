// ==============================================================================
// File: Services/DatabaseService.DigitalSignatures.Extensions.cs
// Purpose: Minimal Digital Signatures list/CRUD and export shims for ViewModel
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Helpers;
using YasGMP.Models;

namespace YasGMP.Services
{
    public static class DatabaseServiceDigitalSignaturesExtensions
    {
        public static async Task<List<DigitalSignature>> GetAllSignaturesFullAsync(this DatabaseService db, CancellationToken token = default)
        {
            const string sqlPreferred = @"SELECT id, table_name, record_id, user_id, signature_hash, method, status, signed_at, device_info, ip_address, note, public_key, session_id
FROM digital_signatures ORDER BY signed_at DESC, id DESC";
            DataTable dt;
            try
            {
                dt = await db.ExecuteSelectAsync(sqlPreferred, null, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                const string sqlLegacy = @"SELECT id, table_name, record_id, user_id, signature_hash, method, status, signed_at, device_info, ip_address, note, public_key
FROM digital_signatures ORDER BY signed_at DESC, id DESC";
                dt = await db.ExecuteSelectAsync(sqlLegacy, null, token).ConfigureAwait(false);
            }
            var list = new List<DigitalSignature>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        public static async Task<int> InsertDigitalSignatureAsync(this DatabaseService db, DigitalSignature sig, CancellationToken token = default)
        {
            if (sig == null) throw new ArgumentNullException(nameof(sig));
            const string sqlPreferred = @"INSERT INTO digital_signatures (table_name, record_id, user_id, signature_hash, method, status, signed_at, device_info, ip_address, note, public_key, session_id)
                                VALUES (@table,@rid,@uid,@hash,@method,@status,@at,@dev,@ip,@note,@pk,@sid)";
            const string sqlLegacy = @"INSERT INTO digital_signatures (table_name, record_id, user_id, signature_hash, method, status, signed_at, device_info, ip_address, note, public_key)
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
                new("@pk", (object?)sig.PublicKey ?? DBNull.Value),
                new("@sid", (object?)sig.SessionId ?? DBNull.Value)
            };
            try
            {
                await db.ExecuteNonQueryAsync(sqlPreferred, pars, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                var legacyPars = new List<MySqlParameter>(pars);
                legacyPars.RemoveAll(p => p.ParameterName.Equals("@sid", StringComparison.OrdinalIgnoreCase));
                await db.ExecuteNonQueryAsync(sqlLegacy, legacyPars, token).ConfigureAwait(false);
            }
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

        public static async Task<bool> VerifySignatureAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));

            var signature = await LoadSignatureAsync(db, id, token).ConfigureAwait(false);
            if (signature == null)
            {
                await LogSignatureFailureAsync(db, null, id, "missing_signature", token).ConfigureAwait(false);
                return false;
            }

            if (!string.Equals(signature.Status, "valid", StringComparison.OrdinalIgnoreCase))
            {
                await LogSignatureFailureAsync(db, signature, id, $"status={signature.Status}", token).ConfigureAwait(false);
                return false;
            }

            var expected = await ComputeExpectedSignatureAsync(db, signature, token).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(expected.FailureReason))
            {
                await LogSignatureFailureAsync(db, signature, id, expected.FailureReason!, token).ConfigureAwait(false);
                return false;
            }

            var method = signature.Method?.Trim().ToLowerInvariant() ?? string.Empty;
            bool isValid = method switch
            {
                "pin" or "password" => FixedTimeEquals(signature.SignatureHash, expected.Hash),
                "certificate" => VerifyCertificateSignature(signature, expected.Payload),
                "biometric" => false,
                _ => false
            };

            if (!isValid)
            {
                string reason = method == "biometric"
                    ? "biometric_not_supported"
                    : "mismatch";
                await LogSignatureFailureAsync(db, signature, id, reason, token).ConfigureAwait(false);
                return false;
            }

            return true;
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
                SessionId = S("session_id"),
                UserName = S("user_name")
            };
        }

        private sealed record ExpectedSignatureResult(string? Hash, string? Payload, string? FailureReason);

        private static async Task<DigitalSignature?> LoadSignatureAsync(DatabaseService db, int id, CancellationToken token)
        {
            var pars = new[] { new MySqlParameter("@id", id) };
            const string sqlPreferred = @"SELECT id, table_name, record_id, user_id, signature_hash, method, status, signed_at, device_info, ip_address, note, public_key, session_id
FROM digital_signatures WHERE id=@id LIMIT 1";

            DataTable dt;
            try
            {
                dt = await db.ExecuteSelectAsync(sqlPreferred, pars, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                const string sqlLegacy = @"SELECT id, table_name, record_id, user_id, signature_hash, method, status, signed_at, device_info, ip_address, note, public_key
FROM digital_signatures WHERE id=@id LIMIT 1";
                dt = await db.ExecuteSelectAsync(sqlLegacy, pars, token).ConfigureAwait(false);
            }

            if (dt.Rows.Count != 1) return null;
            return Map(dt.Rows[0]);
        }

        private static async Task<ExpectedSignatureResult> ComputeExpectedSignatureAsync(DatabaseService db, DigitalSignature signature, CancellationToken token)
        {
            string sessionId = signature.SessionId ?? string.Empty;
            string deviceInfo = signature.DeviceInfo ?? string.Empty;
            string table = signature.TableName?.Trim().ToLowerInvariant() ?? string.Empty;

            switch (table)
            {
                case "machines":
                case "machine":
                    {
                        var machine = await db.GetMachineByIdAsync(signature.RecordId, token).ConfigureAwait(false);
                        if (machine == null)
                            return new ExpectedSignatureResult(null, null, "machine_missing");
                        var result = DigitalSignatureHelper.ComputeSignature(machine, sessionId, deviceInfo);
                        return new ExpectedSignatureResult(result.Hash, result.Payload, null);
                    }
                case "parts":
                case "part":
                    {
                        var part = await db.GetPartByIdAsync(signature.RecordId, token).ConfigureAwait(false);
                        if (part == null)
                            return new ExpectedSignatureResult(null, null, "part_missing");
                        var result = DigitalSignatureHelper.ComputeSignature(part, sessionId, deviceInfo);
                        return new ExpectedSignatureResult(result.Hash, result.Payload, null);
                    }
                case "qualifications":
                case "qualification":
                case "component_qualifications":
                    {
                        var qualification = await db.GetQualificationByIdAsync(signature.RecordId, token).ConfigureAwait(false);
                        if (qualification == null)
                            return new ExpectedSignatureResult(null, null, "qualification_missing");
                        var result = DigitalSignatureHelper.ComputeSignature(qualification, sessionId, deviceInfo);
                        return new ExpectedSignatureResult(result.Hash, result.Payload, null);
                    }
                case "capa_cases":
                case "capa_case":
                case "capa":
                    {
                        var capaCase = await db.GetCapaCaseByIdAsync(signature.RecordId, token).ConfigureAwait(false);
                        if (capaCase == null)
                            return new ExpectedSignatureResult(null, null, "capa_case_missing");
                        var result = DigitalSignatureHelper.ComputeSignature(capaCase, sessionId, deviceInfo);
                        return new ExpectedSignatureResult(result.Hash, result.Payload, null);
                    }
                default:
                    return new ExpectedSignatureResult(null, null, $"unsupported_table={signature.TableName}");
            }
        }

        private static bool VerifyCertificateSignature(DigitalSignature signature, string? payload)
        {
            if (string.IsNullOrWhiteSpace(signature.PublicKey))
                return false;
            if (string.IsNullOrWhiteSpace(signature.SignatureHash))
                return false;
            if (string.IsNullOrEmpty(payload))
                return false;

            byte[] data = Encoding.UTF8.GetBytes(payload);
            byte[] signatureBytes;
            try
            {
                signatureBytes = Convert.FromBase64String(signature.SignatureHash);
            }
            catch (FormatException)
            {
                return false;
            }

            return TryVerifyWithRsa(signature.PublicKey, data, signatureBytes)
                || TryVerifyWithEcdsa(signature.PublicKey, data, signatureBytes);
        }

        private static bool TryVerifyWithRsa(string publicKey, byte[] data, byte[] signature)
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(publicKey.AsSpan());
                return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch (CryptographicException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool TryVerifyWithEcdsa(string publicKey, byte[] data, byte[] signature)
        {
            try
            {
                using var ecdsa = ECDsa.Create();
                ecdsa.ImportFromPem(publicKey.AsSpan());
                return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
            }
            catch (CryptographicException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool FixedTimeEquals(string? left, string? right)
        {
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
                return false;

            try
            {
                var leftBytes = Convert.FromBase64String(left);
                var rightBytes = Convert.FromBase64String(right);
                if (leftBytes.Length != rightBytes.Length)
                    return false;
                return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
            }
            catch (FormatException)
            {
                var leftFallback = Encoding.UTF8.GetBytes(left);
                var rightFallback = Encoding.UTF8.GetBytes(right);
                if (leftFallback.Length != rightFallback.Length)
                    return false;
                return CryptographicOperations.FixedTimeEquals(leftFallback, rightFallback);
            }
        }

        private static Task LogSignatureFailureAsync(DatabaseService db, DigitalSignature? signature, int id, string reason, CancellationToken token)
        {
            int? userId = signature?.UserId;
            string? ip = signature?.IpAddress;
            string? device = signature?.DeviceInfo;
            string? session = signature?.SessionId;
            string description = $"verify_fail:{reason}";
            return db.LogSystemEventAsync(userId, "SIG_VERIFY_FAIL", "digital_signatures", "DigitalSignatures", signature?.Id ?? id, description, ip, "warn", device, session, token: token);
        }
    }
}
