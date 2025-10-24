// ============================================================================
// File: Services/DatabaseService.MachineLookups.Extensions.cs
// Purpose: Lookup helpers that are resilient to schema differences.
//          - Always WRITE into a plain column `name` (auto-add if missing).
//          - READ with COALESCE(name, <best existing text column).
// ============================================================================
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
// Model aliases
using MachineType        = YasGMP.Models.MachineType;
using Manufacturer       = YasGMP.Models.Manufacturer;
using LocationModel      = YasGMP.Models.Location;
using ResponsibleParty   = YasGMP.Models.ResponsibleParty;
using MachineStatusModel = YasGMP.Models.MachineStatus;

namespace YasGMP.Services
{
    /// <summary>
    /// DatabaseService extensions that expose lightweight machine reference lookups.
    /// </summary>
    public static class DatabaseServiceMachineLookupsExtensions
    {
        // Preferred order when looking for an existing display column
        private static readonly string[] NameCandidates = new[]
        {
            "name", "title", "label", "value", "type",
            "status", "status_name",
            "machine_type", "manufacturer",
            "location", "responsible_party", "display_name"
        };

        // ---------- tiny helpers ----------

        private static List<T> MapList<T>(DataTable dt, Func<DataRow, T> map)
        {
            var list = new List<T>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(map(r));
            return list;
        }

        private static Task EnsureLookupTableAsync(DatabaseService db, string table, string nameDef, CancellationToken token)
        {
            var sql = $@"CREATE TABLE IF NOT EXISTS `{table}` (
                           `id`   INT AUTO_INCREMENT PRIMARY KEY,
                           {nameDef}
                         ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            return db.ExecuteNonQueryAsync(sql, null, token);
        }

        private static async Task<bool> ColumnExistsAsync(DatabaseService db, string table, string column, CancellationToken token)
        {
            const string sql = @"
                SELECT 1
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME   = @t
                  AND COLUMN_NAME  = @c
                LIMIT 1;";
            var dt = await db.ExecuteSelectAsync(sql, new[]
            {
                new MySqlParameter("@t", table),
                new MySqlParameter("@c", column)
            }, token).ConfigureAwait(false);

            return dt.Rows.Count > 0;
        }

        private static async Task EnsureNameColumnAsync(DatabaseService db, string table, string nameDef, CancellationToken token)
        {
            // Add a plain, writable `name` column if it doesn't exist.
            if (!await ColumnExistsAsync(db, table, "name", token).ConfigureAwait(false))
            {
                var alter = $@"ALTER TABLE `{table}` ADD COLUMN {nameDef};";
                await db.ExecuteNonQueryAsync(alter, null, token).ConfigureAwait(false);
            }
        }

        private static async Task<string?> FindAlternateDisplayColumnAsync(DatabaseService db, string table, CancellationToken token)
        {
            // Find the best *existing* text column (other than `name`) to display
            const string sql = @"
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME   = @t
                  AND COLUMN_NAME <> 'name'
                  AND COLUMN_NAME IN ('name','title','label','value','type',
                                      'status','status_name','machine_type','manufacturer',
                                      'location','responsible_party','display_name')
                ORDER BY FIELD(COLUMN_NAME,
                               'title','label','value','type',
                               'status','status_name',
                               'machine_type','manufacturer','location',
                               'responsible_party','display_name')
                LIMIT 1;";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@t", table) }, token).ConfigureAwait(false);
            return dt.Rows.Count > 0 ? dt.Rows[0]["COLUMN_NAME"]?.ToString() : null;
        }

        private static async Task<DataTable> LookupSelectAsync(DatabaseService db, string table, string nameDef, CancellationToken token)
        {
            // 1) Make sure table exists; 2) Guarantee writable `name` exists;
            await EnsureLookupTableAsync(db, table, nameDef, token).ConfigureAwait(false);
            await EnsureNameColumnAsync   (db, table, nameDef, token).ConfigureAwait(false);

            // If some legacy column is present (e.g. display_name), show it when `name` is null.
            var alt = await FindAlternateDisplayColumnAsync(db, table, token).ConfigureAwait(false);
            string selectExpr = alt is { Length: > 0 }
                ? $"COALESCE(`name`,`{alt}`)"
                : "`name`";

            var sql = $"SELECT id, {selectExpr} AS name FROM `{table}` ORDER BY {selectExpr}, id";
            return await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
        }

        private static async Task<int> LookupInsertAsync(DatabaseService db, string table, string nameDef, string value, CancellationToken token)
        {
            // Always write into *our* `name` column (safe even if legacy generated columns exist)
            await EnsureLookupTableAsync(db, table, nameDef, token).ConfigureAwait(false);
            await EnsureNameColumnAsync   (db, table, nameDef, token).ConfigureAwait(false);

            var sql = $"INSERT INTO `{table}` (`name`) VALUES (@v)";
            await db.ExecuteNonQueryAsync(sql, new[] { new MySqlParameter("@v", value) }, token).ConfigureAwait(false);
            var id = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
            return Convert.ToInt32(id);
        }

        // ---------- machine_types ----------
        public static async Task<List<MachineType>> GetMachineTypesAsync(this DatabaseService db, CancellationToken token = default)
        {
            var dt = await LookupSelectAsync(db, "machine_types", "`name` VARCHAR(256) NOT NULL", token);
            return MapList(dt, r => new MachineType { Id = Convert.ToInt32(r["id"]), Name = r["name"]?.ToString() ?? string.Empty });
        }
        public static Task<int> AddMachineTypeAsync(this DatabaseService db, string name, CancellationToken token = default)
            => LookupInsertAsync(db, "machine_types", "`name` VARCHAR(256) NOT NULL", name, token);

        // ---------- manufacturers ----------
        public static async Task<List<Manufacturer>> GetManufacturersAsync(this DatabaseService db, CancellationToken token = default)
        {
            var dt = await LookupSelectAsync(db, "manufacturers", "`name` VARCHAR(256) NOT NULL", token);
            return MapList(dt, r => new Manufacturer { Id = Convert.ToInt32(r["id"]), Name = r["name"]?.ToString() ?? string.Empty });
        }
        public static Task<int> AddManufacturerAsync(this DatabaseService db, string name, CancellationToken token = default)
            => LookupInsertAsync(db, "manufacturers", "`name` VARCHAR(256) NOT NULL", name, token);

        // ---------- locations ----------
        public static async Task<List<LocationModel>> GetLocationsAsync(this DatabaseService db, CancellationToken token = default)
        {
            var dt = await LookupSelectAsync(db, "locations", "`name` VARCHAR(256) NOT NULL", token);
            return MapList(dt, r => new LocationModel { Id = Convert.ToInt32(r["id"]), Name = r["name"]?.ToString() ?? string.Empty });
        }
        public static Task<int> AddLocationAsync(this DatabaseService db, string name, CancellationToken token = default)
            => LookupInsertAsync(db, "locations", "`name` VARCHAR(256) NOT NULL", name, token);

        // ---------- responsible_parties ----------
        public static async Task<List<ResponsibleParty>> GetResponsiblePartiesAsync(this DatabaseService db, CancellationToken token = default)
        {
            var dt = await LookupSelectAsync(db, "responsible_parties", "`name` VARCHAR(256) NOT NULL", token);
            return MapList(dt, r => new ResponsibleParty { Id = Convert.ToInt32(r["id"]), Name = r["name"]?.ToString() ?? string.Empty });
        }
        public static Task<int> AddResponsiblePartyAsync(this DatabaseService db, string name, CancellationToken token = default)
            => LookupInsertAsync(db, "responsible_parties", "`name` VARCHAR(256) NOT NULL", name, token);

        // ---------- machine_statuses ----------
        public static async Task<List<MachineStatusModel>> GetMachineStatusesAsync(this DatabaseService db, CancellationToken token = default)
        {
            var dt = await LookupSelectAsync(db, "machine_statuses", "`name` VARCHAR(100) NOT NULL", token);
            return MapList(dt, r => new MachineStatusModel { Id = Convert.ToInt32(r["id"]), Name = r["name"]?.ToString() ?? string.Empty });
        }
        public static Task<int> AddMachineStatusAsync(this DatabaseService db, string name, CancellationToken token = default)
            => LookupInsertAsync(db, "machine_statuses", "`name` VARCHAR(100) NOT NULL", name, token);
    }
}

