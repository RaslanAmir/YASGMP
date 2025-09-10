// ============================================================================
// File: Services/DatabaseService.MachineLookups.Extensions.cs
// Purpose: Helper methods for basic machine lookup tables (type, manufacturer,
//          location, responsible entity, status).
// ============================================================================
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using MachineType = YasGMP.Models.MachineType;
using Manufacturer = YasGMP.Models.Manufacturer;
using LocationModel = YasGMP.Models.Location;
using ResponsibleEntity = YasGMP.Models.ResponsibleEntity;
using MachineStatusModel = YasGMP.Models.MachineStatus;

namespace YasGMP.Services
{
    public static class DatabaseServiceMachineLookupsExtensions
    {
        private static List<T> MapList<T>(DataTable dt, Func<DataRow, T> map)
        {
            var list = new List<T>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows)
                list.Add(map(r));
            return list;
        }

        public static async Task<List<MachineType>> GetMachineTypesAsync(this DatabaseService db, CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync("SELECT id, name FROM machine_types ORDER BY name, id", null, token).ConfigureAwait(false);
            return MapList(dt, r => new MachineType { Id = Convert.ToInt32(r["id"]), Name = r["name"]?.ToString() ?? string.Empty });
        }

        public static async Task<int> AddMachineTypeAsync(this DatabaseService db, string name, CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync("INSERT INTO machine_types (name) VALUES (@n)", new[] { new MySqlParameter("@n", name) }, token).ConfigureAwait(false);
            var id = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
            return Convert.ToInt32(id);
        }

        public static async Task<List<Manufacturer>> GetManufacturersAsync(this DatabaseService db, CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync("SELECT id, name FROM manufacturers ORDER BY name, id", null, token).ConfigureAwait(false);
            return MapList(dt, r => new Manufacturer { Id = Convert.ToInt32(r["id"]), Name = r["name"]?.ToString() ?? string.Empty });
        }

        public static async Task<int> AddManufacturerAsync(this DatabaseService db, string name, CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync("INSERT INTO manufacturers (name) VALUES (@n)", new[] { new MySqlParameter("@n", name) }, token).ConfigureAwait(false);
            var id = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
            return Convert.ToInt32(id);
        }

        public static async Task<List<LocationModel>> GetLocationsAsync(this DatabaseService db, CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync("SELECT id, name FROM locations ORDER BY name, id", null, token).ConfigureAwait(false);
            return MapList(dt, r => new LocationModel { Id = Convert.ToInt32(r["id"]), Name = r["name"]?.ToString() ?? string.Empty });
        }

        public static async Task<int> AddLocationAsync(this DatabaseService db, string name, CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync("INSERT INTO locations (name) VALUES (@n)", new[] { new MySqlParameter("@n", name) }, token).ConfigureAwait(false);
            var id = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
            return Convert.ToInt32(id);
        }

        public static async Task<List<ResponsibleEntity>> GetResponsibleEntitiesAsync(this DatabaseService db, CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync("SELECT id, name FROM responsible_entities ORDER BY name, id", null, token).ConfigureAwait(false);
            return MapList(dt, r => new ResponsibleEntity { Id = Convert.ToInt32(r["id"]), Name = r["name"]?.ToString() ?? string.Empty });
        }

        public static async Task<int> AddResponsibleEntityAsync(this DatabaseService db, string name, CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync("INSERT INTO responsible_entities (name) VALUES (@n)", new[] { new MySqlParameter("@n", name) }, token).ConfigureAwait(false);
            var id = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
            return Convert.ToInt32(id);
        }

        public static async Task<List<MachineStatusModel>> GetMachineStatusesAsync(this DatabaseService db, CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync("SELECT id, name FROM machine_statuses ORDER BY name, id", null, token).ConfigureAwait(false);
            return MapList(dt, r => new MachineStatusModel { Id = Convert.ToInt32(r["id"]), Name = r["name"]?.ToString() ?? string.Empty });
        }

        public static async Task<int> AddMachineStatusAsync(this DatabaseService db, string name, CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync("INSERT INTO machine_statuses (name) VALUES (@n)", new[] { new MySqlParameter("@n", name) }, token).ConfigureAwait(false);
            var id = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
            return Convert.ToInt32(id);
        }
    }
}
