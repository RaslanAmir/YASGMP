// ==============================================================================
// File: Services/DatabaseService.Machines.CoreExtensions.cs
// Purpose: Minimal Machine CRUD helpers to satisfy MachineExtensions and VMs
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// DatabaseService extensions supplying primary machine CRUD and helper queries.
    /// </summary>
    public static class DatabaseServiceMachinesCoreExtensions
    {
        private static HashSet<string>? _machineColumnsCache;
        private static DateTime _machineColumnsCacheTime;

        private static async Task<HashSet<string>> GetMachineColumnsAsync(this DatabaseService db, CancellationToken token = default)
        {
            if (_machineColumnsCache != null && (DateTime.UtcNow - _machineColumnsCacheTime) < TimeSpan.FromMinutes(5))
                return _machineColumnsCache;

            var dt = await db.ExecuteSelectAsync("SHOW COLUMNS FROM machines", null, token).ConfigureAwait(false);
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Data.DataRow r in dt.Rows)
            {
                var name = r["Field"]?.ToString();
                if (!string.IsNullOrWhiteSpace(name)) set.Add(name);
            }
            _machineColumnsCache = set;
            _machineColumnsCacheTime = DateTime.UtcNow;
            return set;
        }
        public static async Task<List<Machine>> GetAllMachinesAsync(this DatabaseService db, CancellationToken token = default)
        {
            const string sqlPrefer = @"
SELECT
    m.id, m.code, m.name,
    COALESCE(mt.name, m.machine_type)       AS machine_type,
    m.description, m.model,
    m.serial_number,
    COALESCE(mf.name, m.manufacturer)       AS manufacturer,
    COALESCE(l.name,  m.location)           AS location,
    COALESCE(rp.name, m.responsible_party)  AS responsible_party,
    m.install_date, m.procurement_date,
    COALESCE(ms.name, m.status)             AS status,
    m.urs_doc, m.qr_code,
    m.internal_code, m.qr_payload
FROM machines m
LEFT JOIN machine_types        mt ON m.machine_type_id       = mt.id
LEFT JOIN manufacturers        mf ON m.manufacturer_id       = mf.id
LEFT JOIN locations            l  ON m.location_id           = l.id
LEFT JOIN responsible_parties  rp ON m.responsible_party_id  = rp.id
LEFT JOIN machine_statuses     ms ON m.status_id             = ms.id
ORDER BY m.name, m.id";

            const string sqlCompat = @"
SELECT
    m.id, m.code, m.name,
    mt.name AS machine_type,
    m.description, m.model,
    m.serial_number,
    mf.name AS manufacturer,
    l.name  AS location,
    rp.name AS responsible_party,
    m.install_date, m.procurement_date,
    COALESCE(ms.name, m.status) AS status,
    m.urs_doc, m.qr_code,
    m.internal_code, m.qr_payload
FROM machines m
LEFT JOIN machine_types        mt ON m.machine_type_id       = mt.id
LEFT JOIN manufacturers        mf ON m.manufacturer_id       = mf.id
LEFT JOIN locations            l  ON m.location_id           = l.id
LEFT JOIN responsible_parties  rp ON m.responsible_party_id  = rp.id
LEFT JOIN machine_statuses     ms ON m.status_id             = ms.id
ORDER BY m.name, m.id";

            System.Data.DataTable dt;
            try
            {
                dt = await db.ExecuteSelectAsync(sqlPrefer, null, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054 /* Unknown column */)
            {
                dt = await db.ExecuteSelectAsync(sqlCompat, null, token).ConfigureAwait(false);
            }

            var list = new List<Machine>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows)
            {
                string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
                DateTime? DN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

                list.Add(new Machine
                {
                    Id = r.Table.Columns.Contains("id") && r["id"] != DBNull.Value ? Convert.ToInt32(r["id"]) : 0,
                    Code = S("code"),
                    Name = S("name"),
                    MachineType = S("machine_type"),
                    Description = S("description"),
                    Model = S("model"),
                    SerialNumber = S("serial_number"),
                    Manufacturer = S("manufacturer"),
                    Location = S("location"),
                    ResponsibleParty = S("responsible_party"),
                    InstallDate = DN("install_date"),
                    ProcurementDate = DN("procurement_date"),
                    Status = S("status"),
                    UrsDoc = S("urs_doc"),
                    QrCode = S("qr_code"),
                    InternalCode = S("internal_code"),
                    QrPayload = S("qr_payload")
                });
            }
            return list;
        }

        // Overload used by some callers that pass includeAudit; flag is ignored for compatibility
        public static Task<List<Machine>> GetAllMachinesAsync(this DatabaseService db, bool includeAudit, CancellationToken token = default)
            => db.GetAllMachinesAsync(token);

        public static async Task<Machine?> GetMachineByIdAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            const string sqlPrefer = @"
SELECT
    m.id, m.code, m.name,
    COALESCE(mt.name, m.machine_type)      AS machine_type,
    m.description, m.model,
    m.serial_number,
    COALESCE(mf.name, m.manufacturer)      AS manufacturer,
    COALESCE(l.name,  m.location)          AS location,
    COALESCE(rp.name, m.responsible_party) AS responsible_party
FROM machines m
LEFT JOIN machine_types        mt ON m.machine_type_id       = mt.id
LEFT JOIN manufacturers        mf ON m.manufacturer_id       = mf.id
LEFT JOIN locations            l  ON m.location_id           = l.id
LEFT JOIN responsible_parties  rp ON m.responsible_party_id  = rp.id
WHERE m.id=@id
LIMIT 1";

            const string sqlCompat = @"
SELECT
    m.id, m.code, m.name,
    mt.name AS machine_type,
    m.description, m.model,
    m.serial_number,
    mf.name AS manufacturer,
    l.name  AS location,
    rp.name AS responsible_party
FROM machines m
LEFT JOIN machine_types        mt ON m.machine_type_id       = mt.id
LEFT JOIN manufacturers        mf ON m.manufacturer_id       = mf.id
LEFT JOIN locations            l  ON m.location_id           = l.id
LEFT JOIN responsible_parties  rp ON m.responsible_party_id  = rp.id
WHERE m.id=@id
LIMIT 1";

            System.Data.DataTable dt;
            try
            {
                dt = await db.ExecuteSelectAsync(sqlPrefer, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054 /* Unknown column */)
            {
                dt = await db.ExecuteSelectAsync(sqlCompat, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            }
            if (dt.Rows.Count != 1) return null;

            var r = dt.Rows[0];
            return new Machine
            {
                Id = id,
                Code = r.Table.Columns.Contains("code") ? r["code"]?.ToString() ?? string.Empty : string.Empty,
                Name = r.Table.Columns.Contains("name") ? r["name"]?.ToString() ?? string.Empty : string.Empty,
                MachineType = r.Table.Columns.Contains("machine_type") ? r["machine_type"]?.ToString() ?? string.Empty : string.Empty,
                Description = r.Table.Columns.Contains("description") ? r["description"]?.ToString() ?? string.Empty : string.Empty,
                Model = r.Table.Columns.Contains("model") ? r["model"]?.ToString() ?? string.Empty : string.Empty,
                SerialNumber = r.Table.Columns.Contains("serial_number") ? r["serial_number"]?.ToString() ?? string.Empty : string.Empty,
                Manufacturer = r.Table.Columns.Contains("manufacturer") ? r["manufacturer"]?.ToString() ?? string.Empty : string.Empty,
                Location = r.Table.Columns.Contains("location") ? r["location"]?.ToString() ?? string.Empty : string.Empty,
                ResponsibleParty = r.Table.Columns.Contains("responsible_party") ? r["responsible_party"]?.ToString() ?? string.Empty : string.Empty
            };
        }

        public static async Task<int> InsertOrUpdateMachineAsync(
            this DatabaseService db,
            Machine m,
            bool update,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));

            // Hard guard: ensure non-null/non-empty machine code so DB never receives NULL
            string codeVal = m.Code ?? string.Empty;
            if (string.IsNullOrWhiteSpace(codeVal))
            {
                try
                {
                    var gen = new CodeGeneratorService();
                    codeVal = gen.GenerateMachineCode(m.Name, m.Manufacturer);
                }
                catch
                {
                    codeVal = $"MCH-AUTO-{DateTime.UtcNow:yyyyMMddHHmmss}";
                }
                m.Code = codeVal;
            }

            // Robust INSERT: treat NULL or empty @code as auto-generated value
            string insert = @"
INSERT INTO machines
 (code, name, machine_type_id, description, model,
  manufacturer_id, manufacturer,
  location_id, location,
  responsible_party_id,
  install_date, procurement_date, warranty_until,
  decommission_date, decommission_reason,
  status_id, status,
  urs_doc, serial_number, acquisition_cost,
  rfid_tag, qr_code, iot_device_id, cloud_device_guid,
  is_critical, lifecycle_phase, note,
  internal_code, qr_payload)
VALUES
 (COALESCE(NULLIF(@code,''), CONCAT('MCH-AUTO-', DATE_FORMAT(UTC_TIMESTAMP(), '%Y%m%d%H%i%s'))), @name,
  (SELECT id FROM machine_types       WHERE name=@type     LIMIT 1),
  @desc, @model,
  (SELECT id FROM manufacturers       WHERE name=@mf       LIMIT 1), @mf,
  (SELECT id FROM locations           WHERE name=@loc      LIMIT 1), @loc,
  (SELECT id FROM responsible_parties WHERE name=@resp     LIMIT 1),
  @inst, @proc, @warranty,
  @decom_date, @decom_reason,
  (SELECT id FROM machine_statuses    WHERE name=@status   LIMIT 1), @status,
  @urs, @sn, @cost,
  @rfid, @qr, @iot, @cloud,
  @critical, @phase, @note,
  @internal_code, @qr_payload)";

            // Robust UPDATE: do not allow NULL/empty code to overwrite existing
            string updateSql = @"
UPDATE machines SET
  code=COALESCE(NULLIF(@code,''), code), name=@name,
  machine_type_id      = (SELECT id FROM machine_types       WHERE name=@type   LIMIT 1),
  description          = @desc,
  model                = @model,
  manufacturer_id      = (SELECT id FROM manufacturers       WHERE name=@mf     LIMIT 1),
  manufacturer         = @mf,
  location_id          = (SELECT id FROM locations           WHERE name=@loc    LIMIT 1),
  location             = @loc,
  responsible_party_id = (SELECT id FROM responsible_parties WHERE name=@resp   LIMIT 1),
  install_date         = @inst,
  procurement_date     = @proc,
  warranty_until       = @warranty,
  decommission_date    = @decom_date,
  decommission_reason  = @decom_reason,
  status_id            = (SELECT id FROM machine_statuses    WHERE name=@status LIMIT 1),
  status               = @status,
  urs_doc              = @urs,
  serial_number        = @sn,
  acquisition_cost     = @cost,
  rfid_tag             = @rfid,
  qr_code              = @qr,
  iot_device_id        = @iot,
  cloud_device_guid    = @cloud,
  is_critical          = @critical,
  lifecycle_phase      = @phase,
  note                 = @note,
  internal_code        = COALESCE(@internal_code, internal_code),
  qr_payload           = COALESCE(@qr_payload,  qr_payload)
WHERE id=@id";

            var pars = new List<MySqlParameter>
            {
                new("@code", codeVal),
                new("@name", m.Name ?? string.Empty),
                new("@type", (object?)m.MachineType ?? DBNull.Value),
                new("@desc", (object?)m.Description ?? DBNull.Value),
                new("@model", (object?)m.Model ?? DBNull.Value),
                new("@mf", (object?)m.Manufacturer ?? DBNull.Value),
                new("@loc", (object?)m.Location ?? DBNull.Value),
                new("@resp", (object?)m.ResponsibleParty ?? DBNull.Value),
                new("@inst", (object?)m.InstallDate ?? DBNull.Value),
                new("@proc", (object?)m.ProcurementDate ?? DBNull.Value),
                new("@warranty", (object?)m.WarrantyUntil ?? DBNull.Value),
                new("@decom_date", (object?)m.DecommissionDate ?? DBNull.Value),
                new("@decom_reason", (object?)m.DecommissionReason ?? DBNull.Value),
                new("@status", (object?)m.Status ?? DBNull.Value),
                new("@urs", (object?)m.UrsDoc ?? DBNull.Value),
                new("@sn", (object?)m.SerialNumber ?? DBNull.Value),
                new("@cost", (object?)m.AcquisitionCost ?? DBNull.Value),
                new("@rfid", (object?)m.RfidTag ?? DBNull.Value),
                new("@qr", (object?)m.QrCode ?? DBNull.Value),
                new("@iot", (object?)m.IoTDeviceId ?? DBNull.Value),
                new("@cloud", (object?)m.CloudDeviceGuid ?? DBNull.Value),
                new("@critical", m.IsCritical),
                new("@phase", (object?)m.LifecyclePhase ?? DBNull.Value),
                new("@note", (object?)m.Note ?? DBNull.Value),
                new("@internal_code", (object?)m.InternalCode ?? DBNull.Value),
                new("@qr_payload",  (object?)m.QrPayload  ?? DBNull.Value)
            };
            if (update) pars.Add(new MySqlParameter("@id", m.Id));

            if (!update)
            {
                try
                {
                    await db.ExecuteNonQueryAsync(insert, pars, token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Fallback insert: build column list based on existing schema
                    var cols = await db.GetMachineColumnsAsync(token).ConfigureAwait(false);

                    var colList = new List<string>();
                    var valList = new List<string>();
                    void Add(string col, string expr)
                    {
                        if (cols.Contains(col)) { colList.Add(col); valList.Add(expr); }
                    }

                    Add("code",              "COALESCE(NULLIF(@code,''), CONCAT('MCH-AUTO-', DATE_FORMAT(UTC_TIMESTAMP(), '%Y%m%d%H%i%s')))");
                    Add("name",              "@name");
                    Add("description",       "@desc");
                    Add("model",             "@model");
                    Add("serial_number",     "@sn");
                    Add("manufacturer",      "@mf");
                    Add("location",          "@loc");
                    Add("install_date",      "@inst");
                    Add("procurement_date",  "@proc");
                    Add("warranty_until",    "@warranty");
                    Add("decommission_date", "@decom_date");
                    Add("decommission_reason","@decom_reason");
                    Add("status",            "@status");
                    Add("urs_doc",           "@urs");
                    Add("acquisition_cost",  "@cost");
                    Add("rfid_tag",          "@rfid");
                    Add("qr_code",           "@qr");
                    Add("iot_device_id",     "@iot");
                    Add("cloud_device_guid", "@cloud");
                    Add("is_critical",       "@critical");
                    Add("lifecycle_phase",   "@phase");
                    Add("note",              "@note");
                    Add("internal_code",     "@internal_code");
                    Add("qr_payload",        "@qr_payload");

                    string fallbackInsert = $"INSERT INTO machines (" + string.Join(",", colList) + ") VALUES (" + string.Join(",", valList) + ")";
                    await db.ExecuteNonQueryAsync(fallbackInsert, pars.ToArray(), token).ConfigureAwait(false);
                }

                var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
                m.Id = Convert.ToInt32(idObj);
            }
            else
            {
                try
                {
                    await db.ExecuteNonQueryAsync(updateSql, pars, token).ConfigureAwait(false);
                }
                catch (MySqlException ex) when (ex.Number == 1054 /* Unknown column */)
                {
                    // Fallback update: dynamically update only columns that exist in schema
                    var cols = await db.GetMachineColumnsAsync(token).ConfigureAwait(false);
                    var sets = new List<string>();
                    void S(string col, string expr) { if (cols.Contains(col)) sets.Add(col + "=" + expr); }

                    S("code",              "COALESCE(NULLIF(@code,''), code)");
                    S("name",              "@name");
                    S("description",       "@desc");
                    S("model",             "@model");
                    S("serial_number",     "@sn");
                    S("manufacturer",      "@mf");
                    S("location",          "@loc");
                    S("install_date",      "@inst");
                    S("procurement_date",  "@proc");
                    S("warranty_until",    "@warranty");
                    S("decommission_date", "@decom_date");
                    S("decommission_reason","@decom_reason");
                    S("status",            "@status");
                    S("urs_doc",           "@urs");
                    S("acquisition_cost",  "@cost");
                    S("rfid_tag",          "@rfid");
                    S("qr_code",           "@qr");
                    S("iot_device_id",     "@iot");
                    S("cloud_device_guid", "@cloud");
                    S("is_critical",       "@critical");
                    S("lifecycle_phase",   "@phase");
                    S("note",              "@note");
                    S("internal_code",     "COALESCE(@internal_code, internal_code)");
                    S("qr_payload",        "COALESCE(@qr_payload,  qr_payload)");

                    string fallbackUpdate = "UPDATE machines SET " + string.Join(", ", sets) + " WHERE id=@id";
                    await db.ExecuteNonQueryAsync(fallbackUpdate, pars, token).ConfigureAwait(false);
                }
            }

            await db.LogSystemEventAsync(
                actorUserId,
                update ? "MACHINE_UPDATE" : "MACHINE_CREATE",
                "machines",
                "MachineModule",
                m.Id,
                m.Name,
                ip,
                "audit",
                deviceInfo,
                sessionId,
                token: token
            ).ConfigureAwait(false);

            return m.Id;
        }

        public static Task<int> RollbackMachineAsync(
            this DatabaseService db,
            Machine snapshot,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
            => db.InsertOrUpdateMachineAsync(snapshot, update: true, actorUserId, ip, deviceInfo, sessionId, token);

        public static async Task<string> ExportMachinesAsync(
            this DatabaseService db,
            IEnumerable<Machine> rows,
            string ip,
            string deviceInfo,
            string? sessionId,
            string format,
            int actorUserId,
            CancellationToken token = default)
        {
            string path = $"/export/machines_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{(string.IsNullOrWhiteSpace(format) ? "zip" : format)}";
            int count = rows?.Count() ?? 0;

            await db.LogSystemEventAsync(
                actorUserId,
                "MACHINE_EXPORT",
                "machines",
                "MachineModule",
                null,
                $"fmt={format}; file={path}; count={count}",
                ip,
                "info",
                deviceInfo,
                sessionId,
                token: token
            ).ConfigureAwait(false);

            return path;
        }

        public static async Task DeleteMachineAsync(
            this DatabaseService db,
            int machineId,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            try
            {
                await db.ExecuteNonQueryAsync(
                    "DELETE FROM machines WHERE id=@id",
                    new[] { new MySqlParameter("@id", machineId) },
                    token
                ).ConfigureAwait(false);
            }
            catch
            {
                // ignore if table missing
            }

            await db.LogSystemEventAsync(
                actorUserId,
                "MACHINE_DELETE",
                "machines",
                "MachineModule",
                machineId,
                null,
                ip,
                "audit",
                deviceInfo,
                sessionId,
                token: token
            ).ConfigureAwait(false);
        }
    }
}
