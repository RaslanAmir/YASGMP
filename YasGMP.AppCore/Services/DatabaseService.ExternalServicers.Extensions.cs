// ==============================================================================
// File: Services/DatabaseService.ExternalServicers.Extensions.cs
// Purpose: External servicers (external_contractors) minimal CRUD for UI
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
    /// DatabaseService extensions for managing external service providers.
    /// </summary>
    public static class DatabaseServiceExternalServicersExtensions
    {
        // UI prefers ExternalContractor. Keep DB mapping tolerant and expose both shapes when needed.
        public static async Task<List<ExternalContractor>> GetAllExternalServicersAsync(this DatabaseService db, CancellationToken token = default)
        {
            const string sql = @"SELECT
    id, name, code, registration_number, contact_person, email, phone, address,
    type, status, cooperation_start, cooperation_end, comment, digital_signature, note
FROM external_contractors ORDER BY name, id";
            var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
            var list = new List<ExternalContractor>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(MapToContractor(r));
            return list;
        }

        public static async Task<ExternalServicer?> GetExternalServicerByIdAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            const string sql = @"SELECT
    id, name, code, registration_number, contact_person, email, phone, address,
    type, status, cooperation_start, cooperation_end, comment, digital_signature, note
FROM external_contractors WHERE id=@id LIMIT 1";

            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            if (dt.Rows.Count == 0)
            {
                return null;
            }

            return MapToServicer(dt.Rows[0]);
        }

        public static async Task<ExternalContractor?> GetExternalContractorByIdAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            var servicer = await db.GetExternalServicerByIdAsync(id, token).ConfigureAwait(false);
            return servicer != null ? ToContractor(servicer) : null;
        }

        public static async Task<int> InsertOrUpdateExternalServicerAsync(this DatabaseService db, ExternalServicer ext, bool update, CancellationToken token = default)
        {
            if (ext == null) throw new ArgumentNullException(nameof(ext));
            string insert = @"INSERT INTO external_contractors (name, code, registration_number, contact_person, email, phone, address, type, status, cooperation_start, cooperation_end, comment, digital_signature, note)
                             VALUES (@name,@code,@reg,@contact,@em,@ph,@addr,@type,@status,@start,@end,@comm,@sig,@note)";
            string updateSql = @"UPDATE external_contractors SET name=@name, code=@code, registration_number=@reg, contact_person=@contact, email=@em, phone=@ph, address=@addr, type=@type, status=@status, cooperation_start=@start, cooperation_end=@end, comment=@comm, digital_signature=@sig, note=@note WHERE id=@id";

            var pars = new List<MySqlParameter>
            {
                new("@name", ext.Name ?? string.Empty),
                new("@code", (object?)ext.Code ?? DBNull.Value),
                new("@reg", (object?)ext.VatOrId ?? DBNull.Value),
                new("@contact", (object?)ext.ContactPerson ?? DBNull.Value),
                new("@em", (object?)ext.Email ?? DBNull.Value),
                new("@ph", (object?)ext.Phone ?? DBNull.Value),
                new("@addr", (object?)ext.Address ?? DBNull.Value),
                new("@type", (object?)ext.Type ?? DBNull.Value),
                new("@status", (object?)ext.Status ?? DBNull.Value),
                new("@start", (object?)ext.CooperationStart ?? DBNull.Value),
                new("@end", (object?)ext.CooperationEnd ?? DBNull.Value),
                new("@comm", (object?)ext.Comment ?? DBNull.Value),
                new("@sig", (object?)ext.DigitalSignature ?? DBNull.Value),
                new("@note", (object?)ext.ExtraNotes ?? DBNull.Value)
            };
            if (update) pars.Add(new MySqlParameter("@id", ext.Id));

            if (!update)
            {
                await db.ExecuteNonQueryAsync(insert, pars, token).ConfigureAwait(false);
                var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
                ext.Id = Convert.ToInt32(idObj);
            }
            else
            {
                await db.ExecuteNonQueryAsync(updateSql, pars, token).ConfigureAwait(false);
            }

            return ext.Id;
        }

        // Overload for UI alias type ExternalContractor
        public static Task<int> InsertOrUpdateExternalServicerAsync(this DatabaseService db, ExternalContractor ext, bool update, CancellationToken token = default)
            => db.InsertOrUpdateExternalServicerAsync(ToServicer(ext), update, token);

        // Back-compat wrappers for ExternalContractorViewModel naming
        public static Task<List<ExternalContractor>> GetAllExternalContractorsAsync(this DatabaseService db, CancellationToken token = default)
            => db.GetAllExternalServicersAsync(token);

        public static Task<int> AddExternalContractorAsync(this DatabaseService db, ExternalContractor contractor, CancellationToken token = default)
            => db.InsertOrUpdateExternalServicerAsync(contractor, update: false, token);

        public static Task<int> UpdateExternalContractorAsync(this DatabaseService db, ExternalContractor contractor, CancellationToken token = default)
            => db.InsertOrUpdateExternalServicerAsync(contractor, update: true, token);

        public static Task DeleteExternalContractorAsync(this DatabaseService db, int id, CancellationToken token = default)
            => db.DeleteExternalServicerAsync(id, token);

        public static Task DeleteExternalServicerAsync(this DatabaseService db, int id, CancellationToken token = default)
            => db.ExecuteNonQueryAsync("DELETE FROM external_contractors WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token);

        private static ExternalServicer MapToServicer(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            DateTime? D(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

            return new ExternalServicer
            {
                Id = I("id"),
                Name = S("name"),
                Code = S("code"),
                VatOrId = S("registration_number"),
                ContactPerson = S("contact_person"),
                Email = S("email"),
                Phone = S("phone"),
                Address = S("address"),
                Type = S("type"),
                Status = S("status"),
                CooperationStart = D("cooperation_start"),
                CooperationEnd = D("cooperation_end"),
                Comment = S("comment"),
                DigitalSignature = S("digital_signature"),
                ExtraNotes = S("note")
            };
        }

        private static ExternalContractor MapToContractor(DataRow r)
        {
            var s = MapToServicer(r);
            return ToContractor(s);
        }

        private static ExternalContractor ToContractor(ExternalServicer s)
            => new ExternalContractor
            {
                Id = s.Id,
                Name = s.Name,
                ContractorCode = s.Code,
                RegistrationNumber = s.VatOrId,
                ContactPerson = s.ContactPerson,
                Email = s.Email,
                Phone = s.Phone,
                Address = s.Address,
                Type = s.Type,
                Status = s.Status, // UI-only on model; tolerated
                DigitalSignature = s.DigitalSignature,
                Note = s.ExtraNotes ?? s.Comment
            };

        private static ExternalServicer ToServicer(ExternalContractor c)
            => new ExternalServicer
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.ContractorCode,
                VatOrId = c.RegistrationNumber,
                ContactPerson = c.ContactPerson,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                Type = c.Type,
                Status = c.Status,
                Comment = c.CommentRaw ?? c.Note,
                DigitalSignature = c.DigitalSignature,
                ExtraNotes = c.Note
            };
    }
}

