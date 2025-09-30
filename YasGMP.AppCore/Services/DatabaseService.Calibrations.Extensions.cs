// ==============================================================================
// File: Services/DatabaseService.Calibrations.Extensions.cs
// Purpose: Calibrations CRUD/read helpers expected by services/viewmodels
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
    /// DatabaseService extensions for calibration records, ranges, and scheduling.
    /// </summary>
    public static class DatabaseServiceCalibrationsExtensions
    {
        public static async Task<List<Calibration>> GetAllCalibrationsAsync(this DatabaseService db, CancellationToken token = default)
        {
            const string sqlPreferred = @"SELECT
    id, component_id, supplier_id, calibration_date, next_due, cert_doc, result, comment,
    digital_signature, digital_signature_id, last_modified, last_modified_by_id, source_ip,
    approved, approved_at, approved_by_id, previous_calibration_id, next_calibration_id,
    change_version, is_deleted
FROM calibrations
ORDER BY calibration_date DESC, id DESC";

            const string sqlLegacy = @"SELECT
    id, component_id, supplier_id, calibration_date, next_due, cert_doc, result, comment,
    digital_signature, last_modified, last_modified_by_id, source_ip,
    approved, approved_at, approved_by_id, previous_calibration_id, next_calibration_id,
    change_version, is_deleted
FROM calibrations
ORDER BY calibration_date DESC, id DESC";

            System.Data.DataTable dt;
            try
            {
                dt = await db.ExecuteSelectAsync(sqlPreferred, null, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                dt = await db.ExecuteSelectAsync(sqlLegacy, null, token).ConfigureAwait(false);
            }
            var list = new List<Calibration>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Parse(r));
            return list;
        }

        public static async Task<Calibration?> GetCalibrationByIdAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            const string sqlPreferred = @"SELECT
    id, component_id, supplier_id, calibration_date, next_due, cert_doc, result, comment,
    digital_signature, digital_signature_id, last_modified, last_modified_by_id, source_ip,
    approved, approved_at, approved_by_id, previous_calibration_id, next_calibration_id,
    change_version, is_deleted
FROM calibrations WHERE id=@id LIMIT 1";

            const string sqlLegacy = @"SELECT
    id, component_id, supplier_id, calibration_date, next_due, cert_doc, result, comment,
    digital_signature, last_modified, last_modified_by_id, source_ip,
    approved, approved_at, approved_by_id, previous_calibration_id, next_calibration_id,
    change_version, is_deleted
FROM calibrations WHERE id=@id LIMIT 1";

            System.Data.DataTable dt;
            try
            {
                dt = await db.ExecuteSelectAsync(sqlPreferred, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                dt = await db.ExecuteSelectAsync(sqlLegacy, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            }
            return dt.Rows.Count == 1 ? Parse(dt.Rows[0]) : null;
        }

        public static async Task<List<Calibration>> GetCalibrationsForComponentAsync(this DatabaseService db, int componentId, CancellationToken token = default)
        {
            const string sqlPreferred = @"SELECT
    id, component_id, supplier_id, calibration_date, next_due, cert_doc, result, comment,
    digital_signature, digital_signature_id, last_modified, last_modified_by_id, source_ip,
    approved, approved_at, approved_by_id, previous_calibration_id, next_calibration_id,
    change_version, is_deleted
FROM calibrations WHERE component_id=@cid ORDER BY calibration_date DESC, id DESC";

            const string sqlLegacy = @"SELECT
    id, component_id, supplier_id, calibration_date, next_due, cert_doc, result, comment,
    digital_signature, last_modified, last_modified_by_id, source_ip,
    approved, approved_at, approved_by_id, previous_calibration_id, next_calibration_id,
    change_version, is_deleted
FROM calibrations WHERE component_id=@cid ORDER BY calibration_date DESC, id DESC";

            System.Data.DataTable dt;
            try
            {
                dt = await db.ExecuteSelectAsync(sqlPreferred, new[] { new MySqlParameter("@cid", componentId) }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                dt = await db.ExecuteSelectAsync(sqlLegacy, new[] { new MySqlParameter("@cid", componentId) }, token).ConfigureAwait(false);
            }
            var list = new List<Calibration>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Parse(r));
            return list;
        }

        public static async Task<List<Calibration>> GetCalibrationsBySupplierAsync(this DatabaseService db, int supplierId, CancellationToken token = default)
        {
            const string sqlPreferred = @"SELECT
    id, component_id, supplier_id, calibration_date, next_due, cert_doc, result, comment,
    digital_signature, digital_signature_id, last_modified, last_modified_by_id, source_ip,
    approved, approved_at, approved_by_id, previous_calibration_id, next_calibration_id,
    change_version, is_deleted
FROM calibrations WHERE supplier_id=@sid ORDER BY calibration_date DESC, id DESC";

            const string sqlLegacy = @"SELECT
    id, component_id, supplier_id, calibration_date, next_due, cert_doc, result, comment,
    digital_signature, last_modified, last_modified_by_id, source_ip,
    approved, approved_at, approved_by_id, previous_calibration_id, next_calibration_id,
    change_version, is_deleted
FROM calibrations WHERE supplier_id=@sid ORDER BY calibration_date DESC, id DESC";

            System.Data.DataTable dt;
            try
            {
                dt = await db.ExecuteSelectAsync(sqlPreferred, new[] { new MySqlParameter("@sid", supplierId) }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                dt = await db.ExecuteSelectAsync(sqlLegacy, new[] { new MySqlParameter("@sid", supplierId) }, token).ConfigureAwait(false);
            }
            var list = new List<Calibration>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Parse(r));
            return list;
        }

        public static async Task<List<Calibration>> GetCalibrationsByDateRangeAsync(this DatabaseService db, DateTime from, DateTime to, CancellationToken token = default)
        {
            const string sqlPreferred = @"SELECT
    id, component_id, supplier_id, calibration_date, next_due, cert_doc, result, comment,
    digital_signature, digital_signature_id, last_modified, last_modified_by_id, source_ip,
    approved, approved_at, approved_by_id, previous_calibration_id, next_calibration_id,
    change_version, is_deleted
FROM calibrations WHERE calibration_date BETWEEN @f AND @t ORDER BY calibration_date DESC, id DESC";

            const string sqlLegacy = @"SELECT
    id, component_id, supplier_id, calibration_date, next_due, cert_doc, result, comment,
    digital_signature, last_modified, last_modified_by_id, source_ip,
    approved, approved_at, approved_by_id, previous_calibration_id, next_calibration_id,
    change_version, is_deleted
FROM calibrations WHERE calibration_date BETWEEN @f AND @t ORDER BY calibration_date DESC, id DESC";

            var pars = new[] { new MySqlParameter("@f", from), new MySqlParameter("@t", to) };
            System.Data.DataTable dt;
            try
            {
                dt = await db.ExecuteSelectAsync(sqlPreferred, pars, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                dt = await db.ExecuteSelectAsync(sqlLegacy, pars, token).ConfigureAwait(false);
            }
            var list = new List<Calibration>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Parse(r));
            return list;
        }

        public static async Task<int> InsertOrUpdateCalibrationAsync(this DatabaseService db, Calibration c, bool update, int actorUserId, string ip, string device, SignatureMetadataDto? signatureMetadata = null, CancellationToken token = default)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));

            string effectiveIp = !string.IsNullOrWhiteSpace(signatureMetadata?.IpAddress) ? signatureMetadata!.IpAddress! : ip ?? string.Empty;
            string effectiveDevice = signatureMetadata?.Device ?? device ?? string.Empty;
            string? effectiveSession = signatureMetadata?.Session;

            if (!string.IsNullOrWhiteSpace(signatureMetadata?.Hash))
            {
                c.DigitalSignature = signatureMetadata!.Hash!;
            }

            if (signatureMetadata?.Id.HasValue == true)
            {
                c.DigitalSignatureId = signatureMetadata.Id;
            }

            c.SourceIp = effectiveIp;

            int? initialSignatureId = c.DigitalSignatureId;

            string insert = @"INSERT INTO calibrations (component_id, supplier_id, calibration_date, next_due, cert_doc, result, comment, digital_signature, source_ip, digital_signature_id)
                              VALUES (@cid,@sid,@cd,@due,@doc,@res,@comm,@sig,@ip,@sig_id)";
            string insertLegacy = @"INSERT INTO calibrations (component_id, supplier_id, calibration_date, next_due, cert_doc, result, comment, digital_signature, source_ip)
                              VALUES (@cid,@sid,@cd,@due,@doc,@res,@comm,@sig,@ip)";
            string updateSql = @"UPDATE calibrations SET component_id=@cid, supplier_id=@sid, calibration_date=@cd, next_due=@due, cert_doc=@doc, result=@res, comment=@comm, digital_signature=@sig, source_ip=@ip, digital_signature_id=@sig_id WHERE id=@id";
            string updateLegacy = @"UPDATE calibrations SET component_id=@cid, supplier_id=@sid, calibration_date=@cd, next_due=@due, cert_doc=@doc, result=@res, comment=@comm, digital_signature=@sig, source_ip=@ip WHERE id=@id";

            var pars = new List<MySqlParameter>
            {
                new("@cid", c.ComponentId),
                new("@sid", (object?)c.SupplierId ?? DBNull.Value),
                new("@cd", c.CalibrationDate),
                new("@due", c.NextDue),
                new("@doc", c.CertDoc ?? string.Empty),
                new("@res", c.Result ?? string.Empty),
                new("@comm", c.Comment ?? string.Empty),
                new("@sig", c.DigitalSignature ?? string.Empty),
                new("@ip", effectiveIp),
                new("@sig_id", (object?)c.DigitalSignatureId ?? DBNull.Value)
            };
            if (update) pars.Add(new MySqlParameter("@id", c.Id));

            if (!update)
            {
                try
                {
                    await db.ExecuteNonQueryAsync(insert, pars, token).ConfigureAwait(false);
                }
                catch (MySqlException ex) when (ex.Number == 1054)
                {
                    var legacyPars = new List<MySqlParameter>(pars);
                    legacyPars.RemoveAll(p => p.ParameterName.Equals("@sig_id", StringComparison.OrdinalIgnoreCase));
                    await db.ExecuteNonQueryAsync(insertLegacy, legacyPars, token).ConfigureAwait(false);
                }

                var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
                c.Id = Convert.ToInt32(idObj);
            }
            else
            {
                try
                {
                    await db.ExecuteNonQueryAsync(updateSql, pars, token).ConfigureAwait(false);
                }
                catch (MySqlException ex) when (ex.Number == 1054)
                {
                    var legacyPars = new List<MySqlParameter>(pars);
                    legacyPars.RemoveAll(p => p.ParameterName.Equals("@sig_id", StringComparison.OrdinalIgnoreCase));
                    await db.ExecuteNonQueryAsync(updateLegacy, legacyPars, token).ConfigureAwait(false);
                }
            }

            await db.LogSystemEventAsync(actorUserId, update ? "CAL_UPDATE" : "CAL_CREATE", "calibrations", "CalibrationModule", c.Id, null, effectiveIp, "audit", effectiveDevice, effectiveSession, token: token).ConfigureAwait(false);

            if (signatureMetadata != null)
            {
                var signatureRecord = new DigitalSignature
                {
                    Id = signatureMetadata.Id ?? 0,
                    TableName = "calibrations",
                    RecordId = c.Id,
                    UserId = actorUserId,
                    SignatureHash = signatureMetadata.Hash ?? c.DigitalSignature,
                    Method = signatureMetadata.Method,
                    Status = signatureMetadata.Status,
                    Note = signatureMetadata.Note,
                    SignedAt = DateTime.UtcNow,
                    DeviceInfo = signatureMetadata.Device ?? effectiveDevice,
                    IpAddress = effectiveIp,
                    SessionId = signatureMetadata.Session ?? effectiveSession
                };

                var persistedId = await db.InsertDigitalSignatureAsync(signatureRecord, token).ConfigureAwait(false);
                if (persistedId > 0)
                {
                    signatureMetadata.Id = persistedId;
                    if (c.DigitalSignatureId != persistedId)
                    {
                        c.DigitalSignatureId = persistedId;
                        if (persistedId != initialSignatureId)
                        {
                            await db.TryUpdateEntitySignatureIdAsync("calibrations", "id", c.Id, "digital_signature_id", persistedId, token).ConfigureAwait(false);
                        }
                    }
                }
            }
            return c.Id;
        }

        // Overload without device parameter (back-compat with callers)
        public static Task<int> InsertOrUpdateCalibrationAsync(this DatabaseService db, Calibration c, bool update, int actorUserId, string ip, CancellationToken token = default)
            => db.InsertOrUpdateCalibrationAsync(c, update, actorUserId, ip, device: string.Empty, signatureMetadata: null, token);

        public static async Task DeleteCalibrationAsync(this DatabaseService db, int id, int actorUserId, string ip, string device, CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync("DELETE FROM calibrations WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            await db.LogSystemEventAsync(actorUserId, "CAL_DELETE", "calibrations", "CalibrationModule", id, null, ip, "audit", device, null, token: token).ConfigureAwait(false);
        }

        // Overload without device (back-compat)
        public static Task DeleteCalibrationAsync(this DatabaseService db, int id, int actorUserId, string ip, CancellationToken token = default)
            => db.DeleteCalibrationAsync(id, actorUserId, ip, device: string.Empty, token);

        private static Calibration Parse(DataRow r)
        {
            int GetInt(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            int? GetIntN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            DateTime GetDate(string c)
            {
                if (!r.Table.Columns.Contains(c) || r[c] == DBNull.Value) return DateTime.UtcNow;
                try { return Convert.ToDateTime(r[c]); } catch { return DateTime.UtcNow; }
            }

            return new Calibration
            {
                Id = GetInt("id"),
                ComponentId = GetInt("component_id"),
                SupplierId = GetIntN("supplier_id"),
                CalibrationDate = GetDate("calibration_date"),
                NextDue = GetDate("next_due"),
                CertDoc = S("cert_doc"),
                Result = S("result"),
                Comment = S("comment"),
                DigitalSignature = S("digital_signature"),
                DigitalSignatureId = GetIntN("digital_signature_id"),
                LastModified = GetDate("last_modified"),
                LastModifiedById = GetIntN("last_modified_by_id"),
                SourceIp = S("source_ip"),
                Approved = r.Table.Columns.Contains("approved") && r["approved"] != DBNull.Value && Convert.ToBoolean(r["approved"]),
                ApprovedAt = r.Table.Columns.Contains("approved_at") && r["approved_at"] != DBNull.Value ? Convert.ToDateTime(r["approved_at"]) : (DateTime?)null,
                ApprovedById = GetIntN("approved_by_id"),
                PreviousCalibrationId = GetIntN("previous_calibration_id"),
                NextCalibrationId = GetIntN("next_calibration_id"),
                ChangeVersion = GetInt("change_version"),
                IsDeleted = r.Table.Columns.Contains("is_deleted") && r["is_deleted"] != DBNull.Value && Convert.ToBoolean(r["is_deleted"])
            };
        }
    }
}
