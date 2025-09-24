// ==============================================================================
// File: Services/DatabaseService.SpareParts.Extensions.cs
// Purpose: Minimal Parts/SpareParts CRUD + audit/export shims used by SparePartViewModel
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
    /// DatabaseService extensions for spare part catalog, pricing, and stock.
    /// </summary>
    public static class DatabaseServiceSparePartsExtensions
    {
        public static async Task<List<Part>> GetAllPartsAsync(this DatabaseService db, CancellationToken token = default)
            => await db.GetAllSparePartsFullAsync(token).ConfigureAwait(false);

        public static async Task<Part?> GetPartByIdAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync("SELECT id, code, name, description, category, barcode, rfid, serial_or_lot, default_supplier_id, price, stock, min_stock_alert, location, image, status, blocked, regulatory_certificates, digital_signature, last_modified, last_modified_by_id, source_ip FROM parts WHERE id=@id LIMIT 1", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            return dt.Rows.Count == 1 ? Map(dt.Rows[0]) : null;
        }

        public static async Task<int> InsertOrUpdatePartAsync(this DatabaseService db, Part part, bool update, CancellationToken token = default)
        {
            if (!update) return await db.AddSparePartAsync(part, actorUserId: 0, ip: string.Empty, deviceInfo: string.Empty, token).ConfigureAwait(false);
            await db.UpdateSparePartAsync(part, actorUserId: 0, ip: string.Empty, deviceInfo: string.Empty, token).ConfigureAwait(false);
            return part.Id;
        }

        public static Task DeletePartAsync(this DatabaseService db, int id, CancellationToken token = default)
            => db.DeleteSparePartAsync(id, actorUserId: 0, ip: string.Empty, token);
        public static async Task<List<Part>> GetAllSparePartsFullAsync(this DatabaseService db, CancellationToken token = default)
        {
            const string sql = @"SELECT p.id, p.code, p.name, p.description, p.category, p.barcode, p.rfid, p.serial_or_lot,
       p.default_supplier_id, p.price, p.stock, p.min_stock_alert, p.location, p.image, p.status, p.blocked,
       p.regulatory_certificates, p.digital_signature, p.last_modified, p.last_modified_by_id, p.source_ip,
       (
         SELECT SUM(CASE WHEN sl.quantity < COALESCE(sl.min_threshold, 2147483647) THEN 1 ELSE 0 END)
         FROM stock_levels sl WHERE sl.part_id = p.id
       ) AS low_wh_count,
       (
         SELECT GROUP_CONCAT(CONCAT(COALESCE(w.name, CONCAT('WH-', sl.warehouse_id)), ':', sl.quantity) ORDER BY w.name SEPARATOR ', ')
         FROM stock_levels sl LEFT JOIN warehouses w ON w.id = sl.warehouse_id WHERE sl.part_id = p.id
       ) AS wh_summary
FROM parts p
ORDER BY p.name, p.id";
            var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
            var list = new List<Part>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        public static async Task<int> AddSparePartAsync(this DatabaseService db, Part part, int actorUserId, string ip, string deviceInfo, CancellationToken token = default)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            string insert = @"INSERT INTO parts (code, name, description, category, barcode, rfid, serial_or_lot, default_supplier_id, price, stock, min_stock_alert, location, image, status, blocked, regulatory_certificates, digital_signature, last_modified, last_modified_by_id, source_ip)
                             VALUES (@code,@name,@desc,@cat,@bar,@rfid,@serial,@supp,@price,@stock,@min,@loc,@img,@status,@blocked,@reg,@sig,NOW(),@lmb,@ip)";

            var pars = BuildParams(part, actorUserId, ip);
            await db.ExecuteNonQueryAsync(insert, pars, token).ConfigureAwait(false);
            var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
            part.Id = Convert.ToInt32(idObj);
            await db.LogSparePartAuditAsync(part.Id, "CREATE", actorUserId, part.DigitalSignature, ip, deviceInfo, null, token).ConfigureAwait(false);
            return part.Id;
        }

        public static async Task UpdateSparePartAsync(this DatabaseService db, Part part, int actorUserId, string ip, string deviceInfo, CancellationToken token = default)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            string update = @"UPDATE parts SET code=@code, name=@name, description=@desc, category=@cat, barcode=@bar, rfid=@rfid, serial_or_lot=@serial, default_supplier_id=@supp, price=@price, stock=@stock, min_stock_alert=@min, location=@loc, image=@img, status=@status, blocked=@blocked, regulatory_certificates=@reg, digital_signature=@sig, last_modified=NOW(), last_modified_by_id=@lmb, source_ip=@ip WHERE id=@id";
            var pars = BuildParams(part, actorUserId, ip);
            pars.Add(new MySqlParameter("@id", part.Id));
            await db.ExecuteNonQueryAsync(update, pars, token).ConfigureAwait(false);
            await db.LogSparePartAuditAsync(part.Id, "UPDATE", actorUserId, part.DigitalSignature, ip, deviceInfo, null, token).ConfigureAwait(false);
        }

        public static Task UpdatePartMinStockAlertAsync(this DatabaseService db, int partId, int? minStock, CancellationToken token = default)
            => db.ExecuteNonQueryAsync("UPDATE parts SET min_stock_alert=@m WHERE id=@id", new[] { new MySqlParameter("@m", (object?)minStock ?? DBNull.Value), new MySqlParameter("@id", partId) }, token);

        public static async Task DeleteSparePartAsync(this DatabaseService db, int id, int actorUserId, string ip, CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync("DELETE FROM parts WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            await db.LogSparePartAuditAsync(id, "DELETE", actorUserId, null, ip, null, null, token).ConfigureAwait(false);
        }

        public static Task RollbackSparePartAsync(this DatabaseService db, int id, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.LogSparePartAuditAsync(id, "ROLLBACK", actorUserId, null, ip, deviceInfo, sessionId, token);

        public static Task ExportSparePartsAsync(this DatabaseService db, List<Part> items, string format, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
        {
            string? path = null;
            if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
            {
                path = YasGMP.Helpers.XlsxExporter.WriteSheet(items ?? new List<Part>(), "parts",
                    new (string, Func<Part, object?>)[]
                    {
                        ("Id", p => p.Id),
                        ("Code", p => p.Code),
                        ("Name", p => p.Name),
                        ("Category", p => p.Category),
                        ("Stock", p => p.Stock),
                        ("Price", p => p.Price)
                    });
            }
            else if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
            {
                path = YasGMP.Helpers.PdfExporter.WriteTable(items ?? new List<Part>(), "parts",
                    new (string, Func<Part, object?>)[]
                    {
                        ("Id", p => p.Id),
                        ("Code", p => p.Code),
                        ("Name", p => p.Name),
                        ("Category", p => p.Category),
                        ("Stock", p => p.Stock)
                    }, title: "Parts Export");
            }
            else
            {
                path = YasGMP.Helpers.CsvExportHelper.WriteCsv(items ?? new List<Part>(), "parts",
                    new (string, Func<Part, object?>)[]
                    {
                        ("Id", p => p.Id),
                        ("Code", p => p.Code),
                        ("Name", p => p.Name),
                        ("Category", p => p.Category),
                        ("Stock", p => p.Stock),
                        ("Price", p => p.Price)
                    });
            }
            return db.LogSparePartAuditAsync(0, "EXPORT", actorUserId, $"fmt={format}; file={path}", ip, deviceInfo, sessionId, token);
        }

        public static Task LogSparePartAuditAsync(this DatabaseService db, int partId, string action, int actorUserId, string? signature, string? ip, string? deviceInfo, string? sessionId, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, $"PART_{action}", "parts", "PartModule", partId == 0 ? null : partId, signature, ip, "audit", deviceInfo, sessionId, token: token);

        public static Task AddPartAsync(this DatabaseService db, Part part, int actorUserId, string ip, string deviceInfo, CancellationToken token = default)
            => db.AddSparePartAsync(part, actorUserId, ip, deviceInfo, token);

        // Back-compat wrappers for PartViewModel naming
        public static Task UpdatePartAsync(this DatabaseService db, Part part, int actorUserId, string ip, string deviceInfo, CancellationToken token = default)
            => db.UpdateSparePartAsync(part, actorUserId, ip, deviceInfo, token);

        public static Task DeletePartAsync(this DatabaseService db, int id, int actorUserId, string ip, string deviceInfo, CancellationToken token = default)
            => db.DeleteSparePartAsync(id, actorUserId, ip, token);

        public static Task RollbackPartAsync(this DatabaseService db, int id, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.RollbackSparePartAsync(id, actorUserId, ip, deviceInfo, sessionId, token);

        public static Task ExportPartsAsync(this DatabaseService db, List<Part> items, string format, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.ExportSparePartsAsync(items, format, actorUserId, ip, deviceInfo, sessionId, token);

        private static List<MySqlParameter> BuildParams(Part p, int actorUserId, string ip)
        {
            object? NV(object? v) => v ?? DBNull.Value;
            return new()
            {
                new("@code", p.Code ?? string.Empty),
                new("@name", p.Name ?? string.Empty),
                new("@desc", NV(p.Description)),
                new("@cat", NV(p.Category)),
                new("@bar", NV(p.Barcode)),
                new("@rfid", NV(p.RFID)),
                new("@serial", NV(p.SerialOrLot)),
                new("@supp", NV(p.DefaultSupplierId)),
                new("@price", NV(p.Price)),
                new("@stock", p.Stock),
                new("@min", NV(p.MinStockAlert)),
                new("@loc", NV(p.Location)),
                new("@img", NV(p.Image)),
                new("@status", NV(p.Status)),
                new("@blocked", p.Blocked),
                new("@reg", NV(p.RegulatoryCertificates)),
                new("@sig", NV(p.DigitalSignature)),
                new("@lmb", actorUserId),
                new("@ip", NV(ip))
            };
        }

        private static Part Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            int? IN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
            decimal? DEC(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDecimal(r[c]) : (decimal?)null;
            bool B(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value && Convert.ToBoolean(r[c]);

            var part = new Part
            {
                Id = I("id"),
                Code = S("code"),
                Name = S("name"),
                Description = S("description"),
                Category = S("category"),
                Barcode = S("barcode"),
                RFID = S("rfid"),
                SerialOrLot = S("serial_or_lot"),
                DefaultSupplierId = IN("default_supplier_id"),
                Price = DEC("price"),
                Stock = I("stock"),
                MinStockAlert = IN("min_stock_alert"),
                Location = S("location"),
                Image = S("image"),
                Status = S("status"),
                Blocked = B("blocked"),
                RegulatoryCertificates = S("regulatory_certificates"),
                DigitalSignature = S("digital_signature"),
                LastModified = DateTime.UtcNow,
                LastModifiedById = IN("last_modified_by_id") ?? 0,
                SourceIp = S("source_ip")
            };
            if (r.Table.Columns.Contains("low_wh_count") && r["low_wh_count"] != DBNull.Value)
            {
                part.LowWarehouseCount = Convert.ToInt32(r["low_wh_count"]);
                part.IsWarehouseStockCritical = part.LowWarehouseCount > 0;
            }
            if (r.Table.Columns.Contains("wh_summary"))
            {
                part.WarehouseSummary = r["wh_summary"]?.ToString() ?? string.Empty;
            }
            return part;
        }
    }
}

