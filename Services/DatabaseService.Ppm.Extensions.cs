// ==============================================================================
// File: Services/DatabaseService.Ppm.Extensions.cs
// Purpose: Preventive Maintenance Plans (PPM) minimal CRUD for compile
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
    /// DatabaseService extensions for preventive maintenance planning entities.
    /// </summary>
    public static class DatabaseServicePpmExtensions
    {
        public static async Task<List<PreventiveMaintenancePlan>> GetAllPpmPlansAsync(this DatabaseService db, CancellationToken token = default)
        {
            // Prefer the richer table when present
            try
            {
                var dt = await db.ExecuteSelectAsync("SELECT id, code, name, description, machine_id, component_id, frequency, checklist_file, next_due, status FROM preventive_maintenance_plans ORDER BY id DESC", null, token).ConfigureAwait(false);
                return MapList(dt);
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                var dt = await db.ExecuteSelectAsync("SELECT id, code, name, description, machine_id, component_id, frequency, checklist_file, next_due, status FROM ppm_plans ORDER BY id DESC", null, token).ConfigureAwait(false);
                return MapList(dt);
            }
        }

        public static async Task<PreventiveMaintenancePlan?> GetPpmPlanByIdAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            try
            {
                var dt = await db.ExecuteSelectAsync("SELECT id, code, name, description, machine_id, component_id, frequency, checklist_file, next_due, status FROM preventive_maintenance_plans WHERE id=@id LIMIT 1", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
                return dt.Rows.Count == 1 ? Map(dt.Rows[0]) : null;
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                var dt = await db.ExecuteSelectAsync("SELECT id, code, name, description, machine_id, component_id, frequency, checklist_file, next_due, status FROM ppm_plans WHERE id=@id LIMIT 1", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
                return dt.Rows.Count == 1 ? Map(dt.Rows[0]) : null;
            }
        }

        public static async Task<int> InsertOrUpdatePpmPlanAsync(this DatabaseService db, PreventiveMaintenancePlan plan, bool update, CancellationToken token = default)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            string insert = @"INSERT INTO preventive_maintenance_plans (code, name, description, machine_id, component_id, frequency, checklist_file, next_due, status)
                             VALUES (@code,@name,@desc,@mid,@cid,@freq,@file,@due,@status)";
            string updateSql = @"UPDATE preventive_maintenance_plans SET code=@code, name=@name, description=@desc, machine_id=@mid, component_id=@cid, frequency=@freq, checklist_file=@file, next_due=@due, status=@status WHERE id=@id";

            var pars = new List<MySqlParameter>
            {
                new("@code", plan.Code ?? string.Empty),
                new("@name", plan.Name ?? string.Empty),
                new("@desc", plan.Description ?? string.Empty),
                new("@mid", (object?)plan.MachineId ?? DBNull.Value),
                new("@cid", (object?)plan.ComponentId ?? DBNull.Value),
                new("@freq", (object?)plan.Frequency ?? DBNull.Value),
                new("@file", (object?)plan.ChecklistFile ?? DBNull.Value),
                new("@due", (object?)plan.NextDue ?? DBNull.Value),
                new("@status", (object?)plan.Status ?? DBNull.Value)
            };
            if (update) pars.Add(new MySqlParameter("@id", plan.Id));

            try
            {
                if (!update)
                {
                    await db.ExecuteNonQueryAsync(insert, pars, token).ConfigureAwait(false);
                    var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
                    plan.Id = Convert.ToInt32(idObj);
                }
                else
                {
                    await db.ExecuteNonQueryAsync(updateSql, pars, token).ConfigureAwait(false);
                }
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                insert = insert.Replace("preventive_maintenance_plans", "ppm_plans");
                updateSql = updateSql.Replace("preventive_maintenance_plans", "ppm_plans");
                if (!update)
                {
                    await db.ExecuteNonQueryAsync(insert, pars, token).ConfigureAwait(false);
                    var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
                    plan.Id = Convert.ToInt32(idObj);
                }
                else
                {
                    await db.ExecuteNonQueryAsync(updateSql, pars, token).ConfigureAwait(false);
                }
            }

            return plan.Id;
        }

        public static async Task DeletePpmPlanAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            try
            {
                await db.ExecuteNonQueryAsync("DELETE FROM preventive_maintenance_plans WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                await db.ExecuteNonQueryAsync("DELETE FROM ppm_plans WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            }
        }

        private static List<PreventiveMaintenancePlan> MapList(DataTable dt)
        {
            var list = new List<PreventiveMaintenancePlan>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        private static PreventiveMaintenancePlan Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int? I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
            DateTime? D(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

            return new PreventiveMaintenancePlan
            {
                Id = r.Table.Columns.Contains("id") && r["id"] != DBNull.Value ? Convert.ToInt32(r["id"]) : 0,
                Code = S("code"),
                Name = S("name"),
                Description = S("description"),
                MachineId = I("machine_id"),
                ComponentId = I("component_id"),
                Frequency = S("frequency"),
                ChecklistFile = S("checklist_file"),
                NextDue = D("next_due"),
                Status = S("status")
            };
        }
    }
}
