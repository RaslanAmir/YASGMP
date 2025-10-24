// ==============================================================================
// File: Services/DatabaseService.Inventory.Extensions.cs
// Purpose: Transactional stock ledger operations (receive, issue, adjust),
//          resilient to schema differences (auto-create tables if missing).
// ==============================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using MySqlConnector;

namespace YasGMP.Services
{
    /// <summary>
    /// DatabaseService extensions supporting inventory locations, stock, and movements.
    /// </summary>
    public static class DatabaseServiceInventoryExtensions
    {
        private static Task EnsureWarehouseSchemaAsync(this DatabaseService db, CancellationToken token)
        {
            const string wh = @"CREATE TABLE IF NOT EXISTS warehouses (
  id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  location VARCHAR(255) NULL
)";
            return db.ExecuteNonQueryAsync(wh, null, token);
        }

        private static Task EnsureStockSchemaAsync(this DatabaseService db, CancellationToken token)
        {
            var sqls = new[]
            {
                @"CREATE TABLE IF NOT EXISTS stock_levels (
  id INT AUTO_INCREMENT PRIMARY KEY,
  part_id INT NOT NULL,
  warehouse_id INT NOT NULL,
  quantity INT NOT NULL DEFAULT 0,
  min_threshold INT NULL,
  max_threshold INT NULL,
  UNIQUE KEY ux_part_wh (part_id, warehouse_id)
)",
                @"CREATE TABLE IF NOT EXISTS inventory_transactions (
  id INT AUTO_INCREMENT PRIMARY KEY,
  part_id INT NOT NULL,
  warehouse_id INT NOT NULL,
  transaction_type VARCHAR(32) NOT NULL,
  quantity INT NOT NULL,
  transaction_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  performed_by_id INT NULL,
  related_document VARCHAR(255) NULL,
  note VARCHAR(255) NULL,
  INDEX ix_it_part_dt (part_id, transaction_date)
)"
            };
            return db.ExecuteNonQueryAsync(string.Join(';', sqls), null, token);
        }

        private static async Task EnsureInventorySchemaAsync(this DatabaseService db, CancellationToken token)
        {
            await db.EnsureWarehouseSchemaAsync(token).ConfigureAwait(false);
            await db.EnsureStockSchemaAsync(token).ConfigureAwait(false);
        }

        // ---------- Core operations ----------

        public static async Task ReceiveStockAsync(
            this DatabaseService db,
            int partId,
            int warehouseId,
            int quantity,
            int? performedById,
            string? doc,
            string? note,
            string ip,
            string device,
            string? sessionId,
            CancellationToken token = default)
        {
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            await db.EnsureInventorySchemaAsync(token).ConfigureAwait(false);
            await db.WithTransactionAsync(async (conn, tx) =>
            {
                // Upsert stock level
                const string upsert = @"INSERT INTO stock_levels (part_id, warehouse_id, quantity)
VALUES (@p,@w,@q)
ON DUPLICATE KEY UPDATE quantity = quantity + VALUES(quantity)";
                var parsUp = new[]
                {
                    new MySqlParameter("@p", partId),
                    new MySqlParameter("@w", warehouseId),
                    new MySqlParameter("@q", quantity)
                };
                var cmdUp = new MySqlCommand(upsert, conn, tx);
                cmdUp.Parameters.AddRange(parsUp);
                await cmdUp.ExecuteNonQueryAsync(token).ConfigureAwait(false);

                // Insert transaction
                const string ins = @"INSERT INTO inventory_transactions (part_id, warehouse_id, transaction_type, quantity, performed_by_id, related_document, note)
VALUES (@p,@w,'in',@q,@u,@doc,@note)";
                var cmdIns = new MySqlCommand(ins, conn, tx);
                cmdIns.Parameters.AddRange(new[]
                {
                    new MySqlParameter("@p", partId),
                    new MySqlParameter("@w", warehouseId),
                    new MySqlParameter("@q", quantity),
                    new MySqlParameter("@u", (object?)performedById ?? DBNull.Value),
                    new MySqlParameter("@doc", (object?)doc ?? DBNull.Value),
                    new MySqlParameter("@note", (object?)note ?? DBNull.Value)
                });
                await cmdIns.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await db.LogSystemEventAsync(performedById, "STOCK_IN", "stock_levels", "Inventory", partId, $"w={warehouseId}; qty={quantity}; doc={doc}", ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
        }

        public static async Task IssueStockAsync(
            this DatabaseService db,
            int partId,
            int warehouseId,
            int quantity,
            int? performedById,
            string? doc,
            string? note,
            string ip,
            string device,
            string? sessionId,
            CancellationToken token = default)
        {
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            await db.EnsureInventorySchemaAsync(token).ConfigureAwait(false);
            await db.WithTransactionAsync(async (conn, tx) =>
            {
                const string update = @"UPDATE stock_levels SET quantity = quantity - @q WHERE part_id=@p AND warehouse_id=@w";
                var cmd = new MySqlCommand(update, conn, tx);
                cmd.Parameters.AddRange(new[]
                {
                    new MySqlParameter("@q", quantity),
                    new MySqlParameter("@p", partId),
                    new MySqlParameter("@w", warehouseId)
                });
                await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);

                const string ins = @"INSERT INTO inventory_transactions (part_id, warehouse_id, transaction_type, quantity, performed_by_id, related_document, note)
VALUES (@p,@w,'out',@q,@u,@doc,@note)";
                var cmdIns = new MySqlCommand(ins, conn, tx);
                cmdIns.Parameters.AddRange(new[]
                {
                    new MySqlParameter("@p", partId),
                    new MySqlParameter("@w", warehouseId),
                    new MySqlParameter("@q", quantity),
                    new MySqlParameter("@u", (object?)performedById ?? DBNull.Value),
                    new MySqlParameter("@doc", (object?)doc ?? DBNull.Value),
                    new MySqlParameter("@note", (object?)note ?? DBNull.Value)
                });
                await cmdIns.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await db.LogSystemEventAsync(performedById, "STOCK_OUT", "stock_levels", "Inventory", partId, $"w={warehouseId}; qty={quantity}; doc={doc}", ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
        }

        public static async Task AdjustStockAsync(
            this DatabaseService db,
            int partId,
            int warehouseId,
            int delta,
            string reason,
            int? performedById,
            string ip,
            string device,
            string? sessionId,
            CancellationToken token = default)
        {
            if (delta == 0) return;
            await db.EnsureInventorySchemaAsync(token).ConfigureAwait(false);
            await db.WithTransactionAsync(async (conn, tx) =>
            {
                const string update = @"INSERT INTO stock_levels (part_id, warehouse_id, quantity)
VALUES (@p,@w,0)
ON DUPLICATE KEY UPDATE quantity = quantity + @d";
                var cmd = new MySqlCommand(update, conn, tx);
                cmd.Parameters.AddRange(new[]
                {
                    new MySqlParameter("@p", partId),
                    new MySqlParameter("@w", warehouseId),
                    new MySqlParameter("@d", delta)
                });
                await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);

                const string ins = @"INSERT INTO inventory_transactions (part_id, warehouse_id, transaction_type, quantity, performed_by_id, related_document, note)
VALUES (@p,@w,'adjust',@q,@u,NULL,@note)";
                var cmdIns = new MySqlCommand(ins, conn, tx);
                cmdIns.Parameters.AddRange(new[]
                {
                    new MySqlParameter("@p", partId),
                    new MySqlParameter("@w", warehouseId),
                    new MySqlParameter("@q", Math.Abs(delta)),
                    new MySqlParameter("@u", (object?)performedById ?? DBNull.Value),
                    new MySqlParameter("@note", reason ?? string.Empty)
                });
                await cmdIns.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await db.LogSystemEventAsync(performedById, "STOCK_ADJUST", "stock_levels", "Inventory", partId, $"w={warehouseId}; delta={delta}; reason={reason}", ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
        }

        public static async Task<int> GetStockAsync(this DatabaseService db, int partId, int warehouseId, CancellationToken token = default)
        {
            await db.EnsureInventorySchemaAsync(token).ConfigureAwait(false);
            const string sql = "SELECT quantity FROM stock_levels WHERE part_id=@p AND warehouse_id=@w";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@p", partId), new MySqlParameter("@w", warehouseId) }, token).ConfigureAwait(false);
            if (dt.Rows.Count == 0) return 0;
            return Convert.ToInt32(dt.Rows[0]["quantity"]);
        }

        public static async Task<Dictionary<int,int>> GetStockByWarehouseAsync(this DatabaseService db, int partId, CancellationToken token = default)
        {
            await db.EnsureInventorySchemaAsync(token).ConfigureAwait(false);
            const string sql = "SELECT warehouse_id, quantity FROM stock_levels WHERE part_id=@p";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@p", partId) }, token).ConfigureAwait(false);
            var dict = new Dictionary<int, int>(dt.Rows.Count);
            foreach (System.Data.DataRow r in dt.Rows)
                dict[Convert.ToInt32(r["warehouse_id"])] = Convert.ToInt32(r["quantity"]);
            return dict;
        }

        public static async Task<List<(int warehouseId, string warehouseName, int quantity, int? min, int? max)>> GetStockLevelsForPartAsync(
            this DatabaseService db,
            int partId,
            CancellationToken token = default)
        {
            await db.EnsureInventorySchemaAsync(token).ConfigureAwait(false);
            const string sql = @"SELECT sl.warehouse_id, COALESCE(w.name, CONCAT('WH-', sl.warehouse_id)) AS wname, sl.quantity, sl.min_threshold, sl.max_threshold
FROM stock_levels sl
LEFT JOIN warehouses w ON w.id = sl.warehouse_id
WHERE sl.part_id=@p
ORDER BY wname, sl.warehouse_id";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@p", partId) }, token).ConfigureAwait(false);
            var list = new List<(int,string,int,int?,int?)>(dt.Rows.Count);
            foreach (System.Data.DataRow r in dt.Rows)
            {
                int wid = Convert.ToInt32(r["warehouse_id"]);
                string name = r["wname"]?.ToString() ?? $"WH-{wid}";
                int qty = r.Table.Columns.Contains("quantity") && r["quantity"] != DBNull.Value ? Convert.ToInt32(r["quantity"]) : 0;
                int? min = r.Table.Columns.Contains("min_threshold") && r["min_threshold"] != DBNull.Value ? Convert.ToInt32(r["min_threshold"]) : (int?)null;
                int? max = r.Table.Columns.Contains("max_threshold") && r["max_threshold"] != DBNull.Value ? Convert.ToInt32(r["max_threshold"]) : (int?)null;
                list.Add((wid, name, qty, min, max));
            }
            return list;
        }

        public static async Task<System.Data.DataTable> GetInventoryMovementPreviewAsync(
            this DatabaseService db,
            int? warehouseId,
            int? partId,
            int take = 20,
            CancellationToken token = default)
        {
            await db.EnsureInventorySchemaAsync(token).ConfigureAwait(false);

            if (take <= 0)
            {
                take = 20;
            }
            else if (take > 500)
            {
                take = 500;
            }

            var sql = new StringBuilder();
            sql.AppendLine("SELECT");
            sql.AppendLine("    it.transaction_date,");
            sql.AppendLine("    it.transaction_type,");
            sql.AppendLine("    it.quantity,");
            sql.AppendLine("    it.related_document,");
            sql.AppendLine("    it.note,");
            sql.AppendLine("    it.performed_by_id");
            sql.AppendLine("FROM inventory_transactions it");

            var filters = new List<string>();
            var parameters = new List<MySqlParameter>
            {
                new("@take", take)
            };

            if (warehouseId.HasValue && warehouseId.Value > 0)
            {
                filters.Add("it.warehouse_id = @warehouseId");
                parameters.Add(new MySqlParameter("@warehouseId", warehouseId.Value));
            }

            if (partId.HasValue && partId.Value > 0)
            {
                filters.Add("it.part_id = @partId");
                parameters.Add(new MySqlParameter("@partId", partId.Value));
            }

            if (filters.Count > 0)
            {
                sql.AppendLine("WHERE " + string.Join(" AND ", filters));
            }

            sql.AppendLine("ORDER BY it.transaction_date DESC, it.id DESC");
            sql.AppendLine("LIMIT @take");

            return await db.ExecuteSelectAsync(sql.ToString(), parameters, token).ConfigureAwait(false);
        }

        public static Task UpdateStockThresholdsAsync(
            this DatabaseService db,
            int partId,
            int warehouseId,
            int? min,
            int? max,
            CancellationToken token = default)
        {
            return db.ExecuteNonQueryAsync(
                "UPDATE stock_levels SET min_threshold=@min, max_threshold=@max WHERE part_id=@p AND warehouse_id=@w",
                new[]
                {
                    new MySqlParameter("@min", (object?)min ?? DBNull.Value),
                    new MySqlParameter("@max", (object?)max ?? DBNull.Value),
                    new MySqlParameter("@p", partId),
                    new MySqlParameter("@w", warehouseId)
                }, token);
        }

        public static async Task<System.Data.DataTable> GetInventoryTransactionsForPartAsync(
            this DatabaseService db,
            int partId,
            int take = 200,
            CancellationToken token = default)
        {
            await db.EnsureInventorySchemaAsync(token).ConfigureAwait(false);
            string sql = @"SELECT id, transaction_date, transaction_type, quantity, warehouse_id, performed_by_id, related_document, note
FROM inventory_transactions
WHERE part_id=@p
ORDER BY transaction_date DESC, id DESC
LIMIT @take";
            return await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@p", partId), new MySqlParameter("@take", take) }, token).ConfigureAwait(false);
        }
    }
}

