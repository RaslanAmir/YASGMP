// ==============================================================================
// File: Services/DatabaseService.Components.QueryExtensions.cs
// Purpose: Read APIs for components expected by services/viewmodels
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
    public static class DatabaseServiceComponentsQueryExtensions
    {
        /// <summary>Returns all machine components.</summary>
        public static async Task<List<MachineComponent>> GetAllComponentsAsync(
            this DatabaseService db,
            CancellationToken token = default)
        {
            const string sql = @"SELECT 
    id, machine_id, code, name, type, model, install_date, purchase_date,
    warranty_until, warranty_expiry, status, serial_number, supplier, rfid_tag,
    io_tdevice_id AS iot_device_id, sop_doc, last_modified, last_modified_by_id,
    source_ip, is_critical, note, digital_signature, notes, lifecycle_phase
FROM machine_components ORDER BY id DESC";
            var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
            var list = new List<MachineComponent>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Parse(r));
            return list;
        }

        /// <summary>Returns a single machine component by id or null.</summary>
        public static async Task<MachineComponent?> GetComponentByIdAsync(
            this DatabaseService db,
            int id,
            CancellationToken token = default)
        {
            const string sql = @"SELECT 
    id, machine_id, code, name, type, model, install_date, purchase_date,
    warranty_until, warranty_expiry, status, serial_number, supplier, rfid_tag,
    io_tdevice_id AS iot_device_id, sop_doc, last_modified, last_modified_by_id,
    source_ip, is_critical, note, digital_signature, notes, lifecycle_phase
FROM machine_components WHERE id=@id LIMIT 1";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            return dt.Rows.Count == 1 ? Parse(dt.Rows[0]) : null;
        }

        private static MachineComponent Parse(DataRow r)
        {
            int? GetInt(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
            string? GetString(string c) => r.Table.Columns.Contains(c) ? r[c]?.ToString() : null;
            DateTime? GetDate(string c)
            {
                if (!r.Table.Columns.Contains(c) || r[c] == DBNull.Value) return null;
                try { return Convert.ToDateTime(r[c]); } catch { return null; }
            }

            var m = new MachineComponent
            {
                Id = GetInt("id") ?? 0,
                MachineId = GetInt("machine_id"),
                Code = GetString("code") ?? string.Empty,
                Name = GetString("name") ?? string.Empty,
                Type = GetString("type"),
                Model = GetString("model"),
                InstallDate = GetDate("install_date"),
                PurchaseDate = GetDate("purchase_date"),
                WarrantyUntil = GetDate("warranty_until"),
                WarrantyExpiry = GetDate("warranty_expiry"),
                Status = GetString("status"),
                SerialNumber = GetString("serial_number"),
                Supplier = GetString("supplier"),
                RfidTag = GetString("rfid_tag"),
                IoTDeviceId = GetString("iot_device_id"),
                SopDoc = GetString("sop_doc"),
                LastModified = GetDate("last_modified") ?? DateTime.UtcNow,
                LastModifiedById = GetInt("last_modified_by_id") ?? 0,
                SourceIp = GetString("source_ip"),
                IsCritical = (GetInt("is_critical") ?? 0) != 0,
                Note = GetString("note"),
                DigitalSignature = GetString("digital_signature"),
                Notes = GetString("notes"),
                LifecyclePhase = GetString("lifecycle_phase")
            };

            return m;
        }

        /// <summary>Returns components linked to a specific machine.</summary>
        public static async Task<List<MachineComponent>> GetComponentsByMachineIdAsync(
            this DatabaseService db,
            int machineId,
            CancellationToken token = default)
        {
            const string sql = @"SELECT 
    id, machine_id, code, name, type, model, install_date, purchase_date,
    warranty_until, warranty_expiry, status, serial_number, supplier, rfid_tag,
    io_tdevice_id AS iot_device_id, sop_doc, last_modified, last_modified_by_id,
    source_ip, is_critical, note, digital_signature, notes, lifecycle_phase
FROM machine_components WHERE machine_id=@mid ORDER BY name, id";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@mid", machineId) }, token).ConfigureAwait(false);
            var list = new List<MachineComponent>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Parse(r));
            return list;
        }

        /// <summary>Returns components that are not linked to any machine (machine_id is NULL or 0).</summary>
        public static async Task<List<MachineComponent>> GetUnassignedComponentsAsync(
            this DatabaseService db,
            CancellationToken token = default)
        {
            const string sql = @"SELECT 
    id, machine_id, code, name, type, model, install_date, purchase_date,
    warranty_until, warranty_expiry, status, serial_number, supplier, rfid_tag,
    io_tdevice_id AS iot_device_id, sop_doc, last_modified, last_modified_by_id,
    source_ip, is_critical, note, digital_signature, notes, lifecycle_phase
FROM machine_components WHERE machine_id IS NULL OR machine_id=0 ORDER BY name, id";
            var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
            var list = new List<MachineComponent>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Parse(r));
            return list;
        }
    }
}
