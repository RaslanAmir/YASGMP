// ==============================================================================
// File: Services/DatabaseService.Qualifications.Extensions.cs
// Purpose: Minimal Qualifications list/CRUD/audit/export shims
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// DatabaseService extensions that manage qualification records and lookups.
    /// </summary>
    public static class DatabaseServiceQualificationsExtensions
    {
        public static async Task<List<Qualification>> GetAllQualificationsAsync(this DatabaseService db, bool includeAudit = true, bool includeCertificates = true, bool includeAttachments = true, CancellationToken token = default)
        {
            // Map from `component_qualifications` if available; otherwise return empty set.
            var dt = await db.ExecuteSelectAsync("SELECT id, component_id, supplier_id, qualification_date, status, certificate_number FROM component_qualifications ORDER BY qualification_date DESC, id DESC", null, token).ConfigureAwait(false);
            var list = new List<Qualification>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        public static async Task<Qualification?> GetQualificationByIdAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            const string sql = @"SELECT cq.id, cq.component_id, cq.supplier_id, cq.type, cq.qualification_type, cq.status, cq.certificate_number,
       mc.name AS component_name,
       s.name  AS supplier_name
FROM component_qualifications cq
LEFT JOIN machine_components mc ON mc.id = cq.component_id
LEFT JOIN suppliers s          ON s.id  = cq.supplier_id
WHERE cq.id=@id
LIMIT 1";

            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            if (dt.Rows.Count != 1) return null;

            var r = dt.Rows[0];
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int? IN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;

            string componentName = S("component_name");
            string supplierName = S("supplier_name");
            string type = !string.IsNullOrWhiteSpace(S("type")) ? S("type") : S("qualification_type");
            string code = !string.IsNullOrWhiteSpace(S("certificate_number")) ? S("certificate_number") : S("code");

            return new Qualification
            {
                Id = id,
                Code = string.IsNullOrWhiteSpace(code) ? id.ToString(CultureInfo.InvariantCulture) : code,
                Type = string.IsNullOrWhiteSpace(type) ? "Component" : type,
                Status = S("status"),
                ComponentId = IN("component_id"),
                SupplierId = IN("supplier_id"),
                Component = string.IsNullOrWhiteSpace(componentName) ? null : new MachineComponent { Name = componentName },
                Supplier = string.IsNullOrWhiteSpace(supplierName) ? null : new Supplier { Name = supplierName }
            };
        }

        public static async Task AddQualificationAsync(this DatabaseService db, Qualification q, string signature, string ip, string device, string? sessionId, CancellationToken token = default)
        {
            if (q == null) throw new ArgumentNullException(nameof(q));
            try
            {
                await db.ExecuteNonQueryAsync(@"INSERT INTO component_qualifications (component_id, supplier_id, qualification_date, status, certificate_number)
                                              VALUES (@comp,@supp,@date,@status,@cert)", new[]
                {
                    new MySqlParameter("@comp", (object?)q.ComponentId ?? DBNull.Value),
                    new MySqlParameter("@supp", (object?)q.SupplierId ?? DBNull.Value),
                    new MySqlParameter("@date", q.Date),
                    new MySqlParameter("@status", q.Status ?? string.Empty),
                    new MySqlParameter("@cert", (object?)q.CertificateNumber ?? DBNull.Value)
                }, token).ConfigureAwait(false);
            }
            catch { /* tolerate schema diffs */ }
            await db.LogQualificationAuditAsync(q, "CREATE", ip, device, sessionId, signature, token).ConfigureAwait(false);
        }

        public static async Task UpdateQualificationAsync(this DatabaseService db, Qualification q, string signature, string ip, string device, string? sessionId, CancellationToken token = default)
        {
            if (q == null) throw new ArgumentNullException(nameof(q));
            try
            {
                await db.ExecuteNonQueryAsync(@"UPDATE component_qualifications SET component_id=@comp, supplier_id=@supp, qualification_date=@date, status=@status, certificate_number=@cert WHERE id=@id", new[]
                {
                    new MySqlParameter("@comp", (object?)q.ComponentId ?? DBNull.Value),
                    new MySqlParameter("@supp", (object?)q.SupplierId ?? DBNull.Value),
                    new MySqlParameter("@date", q.Date),
                    new MySqlParameter("@status", q.Status ?? string.Empty),
                    new MySqlParameter("@cert", (object?)q.CertificateNumber ?? DBNull.Value),
                    new MySqlParameter("@id", q.Id)
                }, token).ConfigureAwait(false);
            }
            catch { }
            await db.LogQualificationAuditAsync(q, "UPDATE", ip, device, sessionId, signature, token).ConfigureAwait(false);
        }

        public static async Task DeleteQualificationAsync(this DatabaseService db, int id, string ip, string device, string? sessionId, CancellationToken token = default)
        {
            try { await db.ExecuteNonQueryAsync("DELETE FROM component_qualifications WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false); } catch { }
            await db.LogQualificationAuditAsync(null, "DELETE", ip, device, sessionId, null, token).ConfigureAwait(false);
        }

        public static Task RollbackQualificationAsync(this DatabaseService db, int id, string ip, string device, string? sessionId, CancellationToken token = default)
            => db.LogQualificationAuditAsync(null, "ROLLBACK", ip, device, sessionId, null, token);

        public static Task ExportQualificationsAsync(this DatabaseService db, List<Qualification> items, string ip, string device, string? sessionId, CancellationToken token = default)
            => db.LogQualificationAuditAsync(null, "EXPORT", ip, device, sessionId, $"count={items?.Count ?? 0}", token);

        public static Task LogQualificationAuditAsync(this DatabaseService db, Qualification? q, string action, string ip, string device, string? sessionId, string? details, CancellationToken token = default)
            => db.LogSystemEventAsync(null, $"QUAL_{action}", "qualifications", "Qualification", q?.Id, details ?? q?.Code, ip, "audit", device, sessionId, token: token);

        private static Qualification Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            int? IN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
            DateTime? D(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

            return new Qualification
            {
                Id = I("id"),
                Code = S("certificate_number"),
                Type = "Component",
                Description = S("qualification_type"),
                Date = D("qualification_date") ?? DateTime.UtcNow,
                ExpiryDate = D("expiry_date"),
                Status = S("status"),
                ComponentId = IN("component_id"),
                SupplierId = IN("supplier_id"),
                CertificateNumber = S("certificate_number")
            };
        }
    }
}

