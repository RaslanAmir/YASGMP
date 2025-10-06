// ==============================================================================
// File: Services/DatabaseService.Suppliers.Extensions.cs
// Purpose: Suppliers CRUD/read helpers expected by services/viewmodels
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.Models.DTO;

namespace YasGMP.Services
{
    /// <summary>
    /// DatabaseService extensions interacting with supplier master data and audits.
    /// </summary>
    public static class DatabaseServiceSuppliersExtensions
    {
        public static async Task<List<Supplier>> GetAllSuppliersAsync(this DatabaseService db, CancellationToken token = default)
        {
            // Some dumps use table name 'suppliers', others 'supplier' (singular). Try plural first.
            try
            {
                var dt = await db.ExecuteSelectAsync("SELECT * FROM suppliers ORDER BY name, id", null, token).ConfigureAwait(false);
                return MapList(dt);
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                var dt = await db.ExecuteSelectAsync("SELECT * FROM supplier /* ANALYZER_IGNORE: legacy table */ ORDER BY name, id", null, token).ConfigureAwait(false);
                return MapList(dt);
            }
        }

        public static async Task<Supplier?> GetSupplierByIdAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            try
            {
                var dt = await db.ExecuteSelectAsync("SELECT * FROM suppliers WHERE id=@id LIMIT 1", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
                return dt.Rows.Count == 1 ? Map(dt.Rows[0]) : null;
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                var dt = await db.ExecuteSelectAsync("SELECT * FROM supplier /* ANALYZER_IGNORE: legacy table */ WHERE id=@id LIMIT 1", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
                return dt.Rows.Count == 1 ? Map(dt.Rows[0]) : null;
            }
        }

        public static async Task<int> InsertOrUpdateSupplierAsync(this DatabaseService db, Supplier s, bool update, SignatureMetadataDto? signatureMetadata = null, int actorUserId = 0, string ip = "", string device = "", string? sessionId = null, CancellationToken token = default)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));

            if (!string.IsNullOrWhiteSpace(signatureMetadata?.Hash))
            {
                s.DigitalSignature = signatureMetadata!.Hash!;
            }

            if (signatureMetadata?.Id.HasValue == true)
            {
                s.DigitalSignatureId = signatureMetadata.Id;
            }

            int? initialSignatureId = s.DigitalSignatureId;

            string insert = @"INSERT INTO suppliers (name, vat_number, address, city, country, email, phone, website, supplier_type, notes, contract_file, status, digital_signature, digital_signature_id)
                             VALUES (@name,@vat,@addr,@city,@country,@em,@ph,@web,@type,@notes,@contract,@status,@sig,@sig_id)";
            string insertLegacy = @"INSERT INTO suppliers (name, vat_number, address, city, country, email, phone, website, supplier_type, notes, contract_file, status, digital_signature)
                             VALUES (@name,@vat,@addr,@city,@country,@em,@ph,@web,@type,@notes,@contract,@status,@sig)";
            string updateSql = @"UPDATE suppliers SET name=@name, vat_number=@vat, address=@addr, city=@city, country=@country,email=@em, phone=@ph, website=@web, supplier_type=@type, notes=@notes, contract_file=@contract, status=@status, digital_signature=@sig, digital_signature_id=@sig_id WHERE id=@id";
            string updateLegacy = @"UPDATE suppliers SET name=@name, vat_number=@vat, address=@addr, city=@city, country=@country,email=@em, phone=@ph, website=@web, supplier_type=@type, notes=@notes, contract_file=@contract, status=@status, digital_signature=@sig WHERE id=@id";

            var pars = new List<MySqlParameter>
            {
                new("@name", s.Name ?? string.Empty),
                new("@vat", s.VatNumber ?? string.Empty),
                new("@addr", s.Address ?? string.Empty),
                new("@city", s.City ?? string.Empty),
                new("@country", s.Country ?? string.Empty),
                new("@em", s.Email ?? string.Empty),
                new("@ph", s.Phone ?? string.Empty),
                new("@web", s.Website ?? string.Empty),
                new("@type", s.SupplierType ?? string.Empty),
                new("@notes", s.Notes ?? string.Empty),
                new("@contract", s.ContractFile ?? string.Empty),
                new("@status", s.Status ?? string.Empty),
                new("@sig", (object?)s.DigitalSignature ?? DBNull.Value),
                new("@sig_id", (object?)s.DigitalSignatureId ?? DBNull.Value)
            };
            if (update) pars.Add(new MySqlParameter("@id", s.Id));

            async Task ExecuteWithSignatureFallbackAsync(string preferredSql, string legacySql, List<MySqlParameter> parameters)
            {
                try
                {
                    await db.ExecuteNonQueryAsync(preferredSql, parameters, token).ConfigureAwait(false);
                }
                catch (MySqlException ex) when (ex.Number == 1054)
                {
                    var legacyPars = new List<MySqlParameter>(parameters);
                    legacyPars.RemoveAll(p => p.ParameterName.Equals("@sig_id", StringComparison.OrdinalIgnoreCase));
                    await db.ExecuteNonQueryAsync(legacySql, legacyPars, token).ConfigureAwait(false);
                }
            }

            async Task ExecuteInsertAsync()
            {
                await ExecuteWithSignatureFallbackAsync(insert, insertLegacy, pars);
                var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
                s.Id = Convert.ToInt32(idObj);
            }

            async Task ExecuteUpdateAsync()
            {
                await ExecuteWithSignatureFallbackAsync(updateSql, updateLegacy, pars);
            }

            string effectiveIp = !string.IsNullOrWhiteSpace(signatureMetadata?.IpAddress)
                ? signatureMetadata!.IpAddress!
                : ip ?? string.Empty;
            string effectiveDevice = signatureMetadata?.Device ?? device ?? string.Empty;
            string? effectiveSession = signatureMetadata?.Session ?? sessionId;

            if (!string.IsNullOrWhiteSpace(effectiveIp))
            {
                s.SourceIp = effectiveIp;
            }

            try
            {
                if (!update)
                {
                    await ExecuteInsertAsync();
                }
                else
                {
                    await ExecuteUpdateAsync();
                }
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                // Singular table fallback
                insert = insert.Replace("suppliers", "supplier /* ANALYZER_IGNORE: legacy table */");
                insertLegacy = insertLegacy.Replace("suppliers", "supplier /* ANALYZER_IGNORE: legacy table */");
                updateSql = updateSql.Replace("suppliers", "supplier /* ANALYZER_IGNORE: legacy table */");
                updateLegacy = updateLegacy.Replace("suppliers", "supplier /* ANALYZER_IGNORE: legacy table */");
                if (!update)
                {
                    await ExecuteInsertAsync();
                }
                else
                {
                    await ExecuteUpdateAsync();
                }
            }

            if (signatureMetadata != null)
            {
                var signatureRecord = new DigitalSignature
                {
                    Id = signatureMetadata.Id ?? 0,
                    TableName = "suppliers",
                    RecordId = s.Id,
                    UserId = actorUserId,
                    SignatureHash = signatureMetadata.Hash ?? s.DigitalSignature,
                    Method = signatureMetadata.Method,
                    Status = signatureMetadata.Status,
                    Note = signatureMetadata.Note,
                    SignedAt = DateTime.UtcNow,
                    DeviceInfo = signatureMetadata.Device ?? effectiveDevice,
                    IpAddress = effectiveIp,
                    SessionId = signatureMetadata.Session ?? effectiveSession
                };

                var persistedId = await db.InsertDigitalSignatureAsync(signatureRecord, token).ConfigureAwait(false);
                if (persistedId > 0)
                {
                    signatureMetadata.Id = persistedId;
                    if (s.DigitalSignatureId != persistedId)
                    {
                        s.DigitalSignatureId = persistedId;
                        if (persistedId != initialSignatureId)
                        {
                            await db.TryUpdateEntitySignatureIdAsync("suppliers", "id", s.Id, "digital_signature_id", persistedId, token).ConfigureAwait(false);
                        }
                    }
                }
            }

            return s.Id;
        }

        public static async Task DeleteSupplierAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            try
            {
                await db.ExecuteNonQueryAsync("DELETE FROM suppliers WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                await db.ExecuteNonQueryAsync("DELETE FROM supplier WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            }
        }

        public static Task RollbackSupplierAsync(this DatabaseService db, int id, int actorUserId, string ip, string device, string? sessionId, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "SUPPLIER_ROLLBACK", "suppliers", "SupplierModule", id, null, ip, "audit", device, sessionId, token: token);

        public static Task ExportSuppliersAsync(this DatabaseService db, List<Supplier> items, string format, int actorUserId, string ip, string device, string? sessionId, CancellationToken token = default)
        {
            string? path = null;
            if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
            {
                path = YasGMP.Helpers.XlsxExporter.WriteSheet(items ?? new List<Supplier>(), "suppliers",
                    new (string, Func<Supplier, object?>)[]
                    {
                        ("Id", s => s.Id),
                        ("Name", s => s.Name),
                        ("Type", s => s.SupplierType),
                        ("Status", s => s.Status),
                        ("Email", s => s.Email),
                        ("Phone", s => s.Phone)
                    });
            }
            else if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
            {
                path = YasGMP.Helpers.PdfExporter.WriteTable(items ?? new List<Supplier>(), "suppliers",
                    new (string, Func<Supplier, object?>)[]
                    {
                        ("Id", s => s.Id),
                        ("Name", s => s.Name),
                        ("Type", s => s.SupplierType),
                        ("Status", s => s.Status),
                        ("Email", s => s.Email)
                    }, title: "Suppliers Export");
            }
            else
            {
                path = YasGMP.Helpers.CsvExportHelper.WriteCsv(items ?? new List<Supplier>(), "suppliers",
                    new (string, Func<Supplier, object?>)[]
                    {
                        ("Id", s => s.Id),
                        ("Name", s => s.Name),
                        ("Type", s => s.SupplierType),
                        ("Status", s => s.Status),
                        ("Email", s => s.Email),
                        ("Phone", s => s.Phone)
                    });
            }
            return db.LogSystemEventAsync(actorUserId, "SUPPLIER_EXPORT", "suppliers", "SupplierModule", null, $"format={format}; count={items?.Count ?? 0}; file={path}", ip, "info", device, sessionId, token: token);
        }

        public static Task LogSupplierAuditAsync(this DatabaseService db, int supplierId, string action, int actorUserId, string? details, string ip, string device, string? sessionId, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, $"SUPPLIER_{action}", "suppliers", "SupplierModule", supplierId, details, ip, "audit", device, sessionId, token: token);

        // ViewModel-friendly helpers
        public static async Task<int> AddSupplierAsync(this DatabaseService db, Supplier supplier, int actorUserId, string ip, string device, CancellationToken token = default)
        {
            var id = await db.InsertOrUpdateSupplierAsync(supplier, update: false, signatureMetadata: null, actorUserId: actorUserId, ip: ip, device: device, sessionId: null, token: token).ConfigureAwait(false);
            await db.LogSupplierAuditAsync(id, "CREATE", actorUserId, null, ip, device, sessionId: null, token: token).ConfigureAwait(false);
            return id;
        }

        public static async Task UpdateSupplierAsync(this DatabaseService db, Supplier supplier, int actorUserId, string ip, string device, CancellationToken token = default)
        {
            await db.InsertOrUpdateSupplierAsync(supplier, update: true, signatureMetadata: null, actorUserId: actorUserId, ip: ip, device: device, sessionId: null, token: token).ConfigureAwait(false);
            await db.LogSupplierAuditAsync(supplier.Id, "UPDATE", actorUserId, null, ip, device, sessionId: null, token: token).ConfigureAwait(false);
        }

        public static async Task DeleteSupplierAsync(this DatabaseService db, int id, int actorUserId, string ip, string device, CancellationToken token = default)
        {
            await db.DeleteSupplierAsync(id, token).ConfigureAwait(false);
            await db.LogSupplierAuditAsync(id, "DELETE", actorUserId, null, ip, device, sessionId: null, token: token).ConfigureAwait(false);
        }

        public static Task<List<Supplier>> GetAllSuppliersFullAsync(this DatabaseService db, CancellationToken token = default)
            => db.GetAllSuppliersAsync(token);

        private static List<Supplier> MapList(DataTable dt)
        {
            var list = new List<Supplier>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        private static Supplier Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int GetInt(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;

            return new Supplier
            {
                Id = GetInt("id"),
                Name = S("name"),
                VatNumber = S("vat_number"),
                Address = S("address"),
                City = S("city"),
                Country = S("country"),
                Email = S("email"),
                Phone = S("phone"),
                Website = S("website"),
                SupplierType = S("supplier_type"),
                Notes = S("notes"),
                ContractFile = S("contract_file"),
                Status = S("status"),
                LastModified = DateTime.UtcNow,
                LastModifiedById = GetInt("last_modified_by_id"),
                SourceIp = S("source_ip"),
                RegisteredAuthorities = S("registered_authorities"),
                RiskLevel = S("risk_level"),
                EntryHash = S("entry_hash"),
                DigitalSignature = S("digital_signature"),
                DigitalSignatureId = r.Table.Columns.Contains("digital_signature_id") && r["digital_signature_id"] != DBNull.Value ? Convert.ToInt32(r["digital_signature_id"]) : (int?)null
            };
        }
    }
}


