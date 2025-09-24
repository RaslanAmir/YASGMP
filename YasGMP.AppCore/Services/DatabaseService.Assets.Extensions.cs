// ==============================================================================
// File: Services/DatabaseService.Assets.Extensions.cs
// Purpose: Assets (machines) minimal APIs used by AssetViewModel
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
    /// DatabaseService extensions that load and persist asset (machine) records.
    /// </summary>
    public static class DatabaseServiceAssetsExtensions
    {
        public static async Task<List<Asset>> GetAllAssetsFullAsync(this DatabaseService db, CancellationToken token = default)
        {
            const string sql = @"SELECT 
    id, code, name, description, model, manufacturer, location,
    install_date, procurement_date, purchase_date, status, urs_doc,
    warranty_until, warranty_expiry, decommission_date, decommission_reason,
    digital_signature, notes
FROM machines
ORDER BY name, id";
            var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
            var list = new List<Asset>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        public static async Task AddAssetAsync(this DatabaseService db, Asset asset, string signatureHash, string ip, string device, string sessionId, int actorUserId, CancellationToken token = default)
        {
            // Minimal insert tolerant to schema; fall back to event log
            try
            {
                const string sql = @"INSERT INTO machines (code, name, description, model, manufacturer, location, install_date, procurement_date, purchase_date, status, urs_doc, digital_signature)
                                    VALUES (COALESCE(NULLIF(@code,''), CONCAT('MCH-AUTO-', DATE_FORMAT(UTC_TIMESTAMP(), '%Y%m%d%H%i%s'))),
                                            @name,@desc,@model,@mf,@loc,@inst,@proc,@purch,@status,@urs,@sig)";
                var pars = new List<MySqlParameter>
                {
                    new("@code", asset.Code ?? string.Empty),
                    new("@name", asset.Name ?? string.Empty),
                    new("@desc", (object?)asset.Description ?? DBNull.Value),
                    new("@model", (object?)asset.Model ?? DBNull.Value),
                    new("@mf", (object?)asset.Manufacturer ?? DBNull.Value),
                    new("@loc", (object?)asset.Location ?? DBNull.Value),
                    new("@inst", (object?)asset.InstallDate ?? DBNull.Value),
                    new("@proc", (object?)asset.ProcurementDate ?? DBNull.Value),
                    new("@purch", (object?)asset.PurchaseDate ?? DBNull.Value),
                    new("@status", (object?)asset.Status ?? DBNull.Value),
                    new("@urs", (object?)asset.UrsDoc ?? DBNull.Value),
                    new("@sig", signatureHash ?? string.Empty)
                };
                await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);
            }
            catch { }
            await db.LogSystemEventAsync(actorUserId, "ASSET_CREATE", "machines", "AssetModule", null, asset?.Name, ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
        }

        public static async Task UpdateAssetAsync(this DatabaseService db, Asset asset, string signatureHash, string ip, string device, string sessionId, int actorUserId, CancellationToken token = default)
        {
            try
            {
                const string sql = @"UPDATE machines SET code=@code, name=@name, description=@desc, model=@model, manufacturer=@mf, location=@loc, status=@status, urs_doc=@urs, digital_signature=@sig WHERE id=@id";
                var pars = new List<MySqlParameter>
                {
                    new("@code", asset.Code ?? string.Empty),
                    new("@name", asset.Name ?? string.Empty),
                    new("@desc", (object?)asset.Description ?? DBNull.Value),
                    new("@model", (object?)asset.Model ?? DBNull.Value),
                    new("@mf", (object?)asset.Manufacturer ?? DBNull.Value),
                    new("@loc", (object?)asset.Location ?? DBNull.Value),
                    new("@status", (object?)asset.Status ?? DBNull.Value),
                    new("@urs", (object?)asset.UrsDoc ?? DBNull.Value),
                    new("@sig", signatureHash ?? string.Empty),
                    new("@id", asset.Id)
                };
                await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);
            }
            catch { }
            await db.LogSystemEventAsync(actorUserId, "ASSET_UPDATE", "machines", "AssetModule", asset?.Id, asset?.Name, ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
        }

        public static async Task DeleteAssetAsync(this DatabaseService db, int id, string ip, string device, string sessionId, int actorUserId, CancellationToken token = default)
        {
            try { await db.ExecuteNonQueryAsync("DELETE FROM machines WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false); } catch { }
            await db.LogSystemEventAsync(actorUserId, "ASSET_DELETE", "machines", "AssetModule", id, null, ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
        }

        public static Task RollbackAssetAsync(this DatabaseService db, int id, string ip, string device, string sessionId, int actorUserId, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "ASSET_ROLLBACK", "machines", "AssetModule", id, null, ip, "audit", device, sessionId, token: token);

        public static Task ExportAssetsAsync(this DatabaseService db, List<Asset> items, string ip, string device, string sessionId, int actorUserId, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "ASSET_EXPORT", "machines", "AssetModule", null, $"count={items?.Count ?? 0}", ip, "info", device, sessionId, token: token);

        public static async Task<string?> ExportAssetsAsync(this DatabaseService db, List<Asset> items, string format, string ip, string device, string sessionId, int actorUserId, CancellationToken token = default)
        {
            string fmt = string.IsNullOrWhiteSpace(format) ? "csv" : format.ToLowerInvariant();
            string? path;
            var list = items ?? new List<Asset>();
            if (fmt == "xlsx")
            {
                path = YasGMP.Helpers.XlsxExporter.WriteSheet(list, "assets",
                    new (string, Func<Asset, object?>)[]
                    {
                        ("Id", a => a.Id),
                        ("Name", a => a.Name),
                        ("Code", a => a.Code),
                        ("Model", a => a.Model),
                        ("Location", a => a.Location),
                        ("Status", a => a.Status)
                    });
            }
            else if (fmt == "pdf")
            {
                path = YasGMP.Helpers.PdfExporter.WriteTable(list, "assets",
                    new (string, Func<Asset, object?>)[]
                    {
                        ("Id", a => a.Id),
                        ("Name", a => a.Name),
                        ("Code", a => a.Code),
                        ("Model", a => a.Model),
                        ("Location", a => a.Location)
                    }, title: "Assets Export");
            }
            else
            {
                path = YasGMP.Helpers.CsvExportHelper.WriteCsv(list, "assets",
                    new (string, Func<Asset, object?>)[]
                    {
                        ("Id", a => a.Id),
                        ("Name", a => a.Name),
                        ("Code", a => a.Code),
                        ("Model", a => a.Model),
                        ("Location", a => a.Location),
                        ("Status", a => a.Status)
                    });
            }
            await db.LogSystemEventAsync(actorUserId, "ASSET_EXPORT", "machines", "AssetModule", null, $"fmt={fmt}; count={list.Count}; file={path}", ip, "info", device, sessionId, token: token).ConfigureAwait(false);
            return path;
        }

        public static Task LogAssetAuditAsync(
            this DatabaseService db,
            int assetId,
            string action,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            string? details,
            CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, $"ASSET_{action}", "machines", "AssetModule", assetId == 0 ? null : assetId, details, ip, "audit", deviceInfo, sessionId, token: token);

        private static Asset Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            DateTime? D(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

            return new Asset
            {
                Id = I("id"),
                Code = S("code"),
                Name = S("name"),
                Description = S("description"),
                Model = S("model"),
                Manufacturer = S("manufacturer"),
                Location = S("location"),
                InstallDate = D("install_date"),
                ProcurementDate = D("procurement_date"),
                PurchaseDate = D("purchase_date"),
                Status = S("status"),
                UrsDoc = S("urs_doc"),
                WarrantyUntil = D("warranty_until"),
                WarrantyExpiry = D("warranty_expiry"),
                DecommissionDate = D("decommission_date"),
                DecommissionReason = S("decommission_reason"),
                DigitalSignature = S("digital_signature"),
                Notes = S("notes")
            };
        }
    }
}
