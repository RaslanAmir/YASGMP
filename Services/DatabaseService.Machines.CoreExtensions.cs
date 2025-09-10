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
    public static class DatabaseServiceMachinesCoreExtensions
    {
        public static async Task<List<Machine>> GetAllMachinesAsync(this DatabaseService db, CancellationToken token = default)
        {
            const string sql = @"SELECT id, code, name, machine_type, description, model, manufacturer, location, responsible_entity,
    install_date, procurement_date, status, urs_doc
FROM machines ORDER BY name, id";
            var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
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
                    Manufacturer = S("manufacturer"),
                    Location = S("location"),
                    ResponsibleEntity = S("responsible_entity"),
                    InstallDate = DN("install_date"),
                    ProcurementDate = DN("procurement_date"),
                    Status = S("status"),
                    UrsDoc = S("urs_doc")
                });
            }
            return list;
        }

        // Overload used by some callers that pass includeAudit; flag is ignored for compatibility
        public static Task<List<Machine>> GetAllMachinesAsync(this DatabaseService db, bool includeAudit, CancellationToken token = default)
            => db.GetAllMachinesAsync(token);

        public static async Task<Machine?> GetMachineByIdAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            const string sql = @"SELECT id, code, name, machine_type, description, model, manufacturer, location, responsible_entity
FROM machines WHERE id=@id LIMIT 1";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
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
                Manufacturer = r.Table.Columns.Contains("manufacturer") ? r["manufacturer"]?.ToString() ?? string.Empty : string.Empty,
                Location = r.Table.Columns.Contains("location") ? r["location"]?.ToString() ?? string.Empty : string.Empty,
                ResponsibleEntity = r.Table.Columns.Contains("responsible_entity") ? r["responsible_entity"]?.ToString() ?? string.Empty : string.Empty
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

            string insert = @"INSERT INTO machines (code, name, machine_type, description, model, manufacturer, location, responsible_entity, install_date, procurement_date, status, urs_doc, serial_number, acquisition_cost, rfid_tag, qr_code, iot_device_id, cloud_device_guid)
                             VALUES (@code,@name,@type,@desc,@model,@mf,@loc,@resp,@inst,@proc,@status,@urs,@sn,@cost,@rfid,@qr,@iot,@cloud)";

            string updateSql = @"UPDATE machines SET code=@code, name=@name, machine_type=@type, description=@desc, model=@model, manufacturer=@mf, location=@loc, responsible_entity=@resp, install_date=@inst, procurement_date=@proc, status=@status, urs_doc=@urs, serial_number=@sn, acquisition_cost=@cost, rfid_tag=@rfid, qr_code=@qr, iot_device_id=@iot, cloud_device_guid=@cloud WHERE id=@id";

            var pars = new List<MySqlParameter>
            {
                new("@code", m.Code ?? string.Empty),
                new("@name", m.Name ?? string.Empty),
                new("@type", (object?)m.MachineType ?? DBNull.Value),
                new("@desc", (object?)m.Description ?? DBNull.Value),
                new("@model", (object?)m.Model ?? DBNull.Value),
                new("@mf", (object?)m.Manufacturer ?? DBNull.Value),
                new("@loc", (object?)m.Location ?? DBNull.Value),
                new("@resp", (object?)m.ResponsibleEntity ?? DBNull.Value),
                new("@inst", (object?)m.InstallDate ?? DBNull.Value),
                new("@proc", (object?)m.ProcurementDate ?? DBNull.Value),
                new("@status", (object?)m.Status ?? DBNull.Value),
                new("@urs", (object?)m.UrsDoc ?? DBNull.Value),
                new("@sn", (object?)m.SerialNumber ?? DBNull.Value),
                new("@cost", (object?)m.AcquisitionCost ?? DBNull.Value),
                new("@rfid", (object?)m.RfidTag ?? DBNull.Value),
                new("@qr", (object?)m.QrCode ?? DBNull.Value),
                new("@iot", (object?)m.IoTDeviceId ?? DBNull.Value),
                new("@cloud", (object?)m.CloudDeviceGuid ?? DBNull.Value)
            };
            if (update) pars.Add(new MySqlParameter("@id", m.Id));

            if (!update)
            {
                await db.ExecuteNonQueryAsync(insert, pars, token).ConfigureAwait(false);
                var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
                m.Id = Convert.ToInt32(idObj);
            }
            else
            {
                await db.ExecuteNonQueryAsync(updateSql, pars, token).ConfigureAwait(false);
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

