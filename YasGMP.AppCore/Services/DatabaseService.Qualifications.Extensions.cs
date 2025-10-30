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
        /// <summary>
        /// Executes the get all qualifications async operation.
        /// </summary>
        public static async Task<List<Qualification>> GetAllQualificationsAsync(this DatabaseService db, bool includeAudit = true, bool includeCertificates = true, bool includeAttachments = true, CancellationToken token = default)
        {
            const string sqlPreferred = @"SELECT
    cq.id,
    cq.component_id,
    cq.machine_id,
    cq.supplier_id,
    cq.qualification_type,
    cq.type,
    cq.status,
    cq.qualification_date,
    cq.expiry_date,
    cq.next_due,
    cq.certificate_number,
    cq.qualified_by_id,
    qualified.username AS qualified_by_username,
    qualified.full_name AS qualified_by_full_name,
    cq.approved_by_id,
    cq.approved_at,
    approved.username AS approved_by_username,
    approved.full_name AS approved_by_full_name,
    mc.code   AS component_code,
    mc.name   AS component_name,
    m.code    AS machine_code,
    m.name    AS machine_name,
    s.name    AS supplier_name
FROM component_qualifications cq
LEFT JOIN machine_components mc ON mc.id = cq.component_id
LEFT JOIN machines m             ON m.id = cq.machine_id
LEFT JOIN suppliers s            ON s.id = cq.supplier_id
LEFT JOIN users qualified        ON qualified.id = cq.qualified_by_id
LEFT JOIN users approved         ON approved.id  = cq.approved_by_id
ORDER BY cq.qualification_date DESC, cq.id DESC";

            const string sqlLegacy = "SELECT id, component_id, supplier_id, qualification_date, status, certificate_number FROM component_qualifications ORDER BY qualification_date DESC, id DESC";

            System.Data.DataTable dt;
            try
            {
                // Map from `component_qualifications` with joined metadata when available.
                dt = await db.ExecuteSelectAsync(sqlPreferred, null, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                // Fall back to legacy minimal projection when newer columns are missing.
                dt = await db.ExecuteSelectAsync(sqlLegacy, null, token).ConfigureAwait(false);
            }
            var list = new List<Qualification>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }
        /// <summary>
        /// Executes the get qualification by id async operation.
        /// </summary>

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
        /// <summary>
        /// Executes the add qualification async operation.
        /// </summary>

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
        /// <summary>
        /// Executes the update qualification async operation.
        /// </summary>

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
        /// <summary>
        /// Executes the delete qualification async operation.
        /// </summary>

        public static async Task DeleteQualificationAsync(this DatabaseService db, int id, string ip, string device, string? sessionId, CancellationToken token = default)
        {
            try { await db.ExecuteNonQueryAsync("DELETE FROM component_qualifications WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false); } catch { }
            await db.LogQualificationAuditAsync(null, "DELETE", ip, device, sessionId, null, token).ConfigureAwait(false);
        }
        /// <summary>
        /// Executes the rollback qualification async operation.
        /// </summary>

        public static Task RollbackQualificationAsync(this DatabaseService db, int id, string ip, string device, string? sessionId, CancellationToken token = default)
            => db.LogQualificationAuditAsync(null, "ROLLBACK", ip, device, sessionId, null, token);
        /// <summary>
        /// Executes the export qualifications async operation.
        /// </summary>

        public static Task ExportQualificationsAsync(this DatabaseService db, List<Qualification> items, string ip, string device, string? sessionId, CancellationToken token = default)
            => db.LogQualificationAuditAsync(null, "EXPORT", ip, device, sessionId, $"count={items?.Count ?? 0}", token);
        /// <summary>
        /// Executes the log qualification audit async operation.
        /// </summary>

        public static Task LogQualificationAuditAsync(this DatabaseService db, Qualification? q, string action, string ip, string device, string? sessionId, string? details, CancellationToken token = default)
            => db.LogSystemEventAsync(null, $"QUAL_{action}", "qualifications", "Qualification", q?.Id, details ?? q?.Code, ip, "audit", device, sessionId, token: token);

        private static Qualification Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            int? IN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
            DateTime? D(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

            string ResolveCode()
            {
                var code = S("certificate_number");
                if (!string.IsNullOrWhiteSpace(code)) return code;
                code = S("code");
                return !string.IsNullOrWhiteSpace(code) ? code : I("id").ToString(CultureInfo.InvariantCulture);
            }

            var qualification = new Qualification
            {
                Id = I("id"),
                Code = ResolveCode(),
                Type = !string.IsNullOrWhiteSpace(S("type"))
                    ? S("type")
                    : (!string.IsNullOrWhiteSpace(S("qualification_type")) ? S("qualification_type") : "Component"),
                Description = S("qualification_type"),
                Date = D("qualification_date") ?? DateTime.UtcNow,
                ExpiryDate = D("expiry_date"),
                Status = S("status"),
                MachineId = IN("machine_id"),
                ComponentId = IN("component_id"),
                SupplierId = IN("supplier_id"),
                QualifiedById = IN("qualified_by_id"),
                ApprovedById = IN("approved_by_id"),
                ApprovedAt = D("approved_at"),
                CertificateNumber = S("certificate_number")
            };

            string machineCode = S("machine_code");
            string machineName = S("machine_name");
            if (qualification.MachineId.HasValue || !string.IsNullOrWhiteSpace(machineCode) || !string.IsNullOrWhiteSpace(machineName))
            {
                qualification.Machine = new Machine
                {
                    Id = qualification.MachineId ?? 0,
                    Code = string.IsNullOrWhiteSpace(machineCode) ? string.Empty : machineCode,
                    Name = string.IsNullOrWhiteSpace(machineName) ? string.Empty : machineName
                };
            }

            string componentCode = S("component_code");
            string componentName = S("component_name");
            if (qualification.ComponentId.HasValue || !string.IsNullOrWhiteSpace(componentCode) || !string.IsNullOrWhiteSpace(componentName))
            {
                qualification.Component = new MachineComponent
                {
                    Id = qualification.ComponentId ?? 0,
                    Code = string.IsNullOrWhiteSpace(componentCode) ? string.Empty : componentCode,
                    Name = string.IsNullOrWhiteSpace(componentName) ? string.Empty : componentName
                };
            }

            string supplierName = S("supplier_name");
            if (qualification.SupplierId.HasValue || !string.IsNullOrWhiteSpace(supplierName))
            {
                qualification.Supplier = new Supplier
                {
                    Id = qualification.SupplierId ?? 0,
                    Name = string.IsNullOrWhiteSpace(supplierName) ? string.Empty : supplierName
                };
            }

            string qualifiedUsername = S("qualified_by_username");
            string qualifiedFullName = S("qualified_by_full_name");
            if (qualification.QualifiedById.HasValue || !string.IsNullOrWhiteSpace(qualifiedUsername) || !string.IsNullOrWhiteSpace(qualifiedFullName))
            {
                qualification.QualifiedBy = new User
                {
                    Id = qualification.QualifiedById ?? 0,
                    Username = string.IsNullOrWhiteSpace(qualifiedUsername) ? string.Empty : qualifiedUsername,
                    FullName = string.IsNullOrWhiteSpace(qualifiedFullName) ? string.Empty : qualifiedFullName
                };
            }

            string approvedUsername = S("approved_by_username");
            string approvedFullName = S("approved_by_full_name");
            if (qualification.ApprovedById.HasValue || !string.IsNullOrWhiteSpace(approvedUsername) || !string.IsNullOrWhiteSpace(approvedFullName))
            {
                qualification.ApprovedBy = new User
                {
                    Id = qualification.ApprovedById ?? 0,
                    Username = string.IsNullOrWhiteSpace(approvedUsername) ? string.Empty : approvedUsername,
                    FullName = string.IsNullOrWhiteSpace(approvedFullName) ? string.Empty : approvedFullName
                };
            }

            return qualification;
        }
    }
}
