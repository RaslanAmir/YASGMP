// ============================================================================
// File: Services/DatabaseService.Warehouse.Extensions.cs
// Purpose: Minimal warehouse lookup + add, schema tolerant.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;

namespace YasGMP.Services
{
    public static class DatabaseServiceWarehouseExtensions
    {
        public static async Task<List<Warehouse>> GetWarehousesAsync(this DatabaseService db, CancellationToken token = default)
        {
            const string sql = @"CREATE TABLE IF NOT EXISTS warehouses (
  id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  location VARCHAR(255) NULL
);";
            await db.ExecuteNonQueryAsync(sql, null, token).ConfigureAwait(false);
            var dt = await db.ExecuteSelectAsync("SELECT id, name, COALESCE(location,'') AS location FROM warehouses ORDER BY name, id", null, token).ConfigureAwait(false);
            var list = new List<Warehouse>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new Warehouse { Id = Convert.ToInt32(r["id"]), Name = r["name"]?.ToString() ?? string.Empty, Location = r["location"]?.ToString() ?? string.Empty });
            }
            return list;
        }

        public static async Task<int> AddWarehouseAsync(this DatabaseService db, string name, string? location, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Required", nameof(name));
            const string ensure = @"CREATE TABLE IF NOT EXISTS warehouses (
  id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  location VARCHAR(255) NULL
);";
            await db.ExecuteNonQueryAsync(ensure, null, token).ConfigureAwait(false);
            const string ins = "INSERT INTO warehouses (name, location) VALUES (@n,@l)";
            await db.ExecuteNonQueryAsync(ins, new[] { new MySqlParameter("@n", name), new MySqlParameter("@l", (object?)location ?? DBNull.Value) }, token).ConfigureAwait(false);
            var id = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
            return Convert.ToInt32(id);
        }
    }
}

