using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>ExternalServicerService</b> – Manages all external calibration service providers.
    /// Includes: CRUD for suppliers, integrations, contact management, and audit logging.
    /// </summary>
    public class ExternalServicerService
    {
        private readonly DatabaseService _db;

        public ExternalServicerService(DatabaseService db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>Returns every external servicer ordered by name.</summary>
        public async Task<IReadOnlyList<ExternalServicer>> GetAllAsync(CancellationToken token = default)
        {
            var contractors = await _db.GetAllExternalServicersAsync(token).ConfigureAwait(false);
            return contractors
                .Select(ToServicer)
                .OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        /// <summary>Returns the external servicer by ID or <c>null</c> when not found.</summary>
        public async Task<ExternalServicer?> TryGetByIdAsync(int id, CancellationToken token = default)
        {
            const string sql = @"SELECT
    id, name, code, registration_number, contact_person, email, phone, address,
    type, status, cooperation_start, cooperation_end, comment, digital_signature, note
FROM external_contractors WHERE id=@id LIMIT 1";

            var table = await _db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@id", id) }, token)
                .ConfigureAwait(false);
            if (table.Rows.Count == 0)
            {
                return null;
            }

            return MapToServicer(table.Rows[0]);
        }

        public async Task<int> CreateAsync(ExternalServicer servicer, int actorUserId, CancellationToken token = default)
        {
            if (servicer is null)
            {
                throw new ArgumentNullException(nameof(servicer));
            }

            await _db.InsertOrUpdateExternalServicerAsync(servicer, update: false, token).ConfigureAwait(false);
            await LogAsync(servicer.Id, "CREATE", actorUserId, servicer.Status, servicer.Type, token).ConfigureAwait(false);
            return servicer.Id;
        }

        public async Task UpdateAsync(ExternalServicer servicer, int actorUserId, CancellationToken token = default)
        {
            if (servicer is null)
            {
                throw new ArgumentNullException(nameof(servicer));
            }

            await _db.InsertOrUpdateExternalServicerAsync(servicer, update: true, token).ConfigureAwait(false);
            await LogAsync(servicer.Id, "UPDATE", actorUserId, servicer.Status, servicer.Type, token).ConfigureAwait(false);
        }

        public async Task DeleteAsync(int id, int actorUserId, CancellationToken token = default)
        {
            await _db.DeleteExternalServicerAsync(id, token).ConfigureAwait(false);
            await LogAsync(id, "DELETE", actorUserId, null, null, token).ConfigureAwait(false);
        }

        private Task LogAsync(int id, string action, int actorUserId, string? status, string? type, CancellationToken token)
        {
            var details = string.Format(
                CultureInfo.InvariantCulture,
                "status={0}; type={1}",
                status ?? "?",
                type ?? "?");
            return _db.LogSupplierAuditAsync(id, action, actorUserId, details, "system", "external-servicer", null, token);
        }

        private static ExternalServicer ToServicer(ExternalContractor contractor)
            => new()
            {
                Id = contractor.Id,
                Name = contractor.Name ?? string.Empty,
                Code = contractor.Code,
                VatOrId = contractor.RegistrationNumber,
                ContactPerson = contractor.ContactPerson,
                Email = contractor.Email,
                Phone = contractor.Phone,
                Address = contractor.Address,
                Type = contractor.Type,
                Status = contractor.Status,
                Comment = contractor.Comment,
                DigitalSignature = contractor.DigitalSignature,
                ExtraNotes = contractor.Note
            };

        private static ExternalServicer MapToServicer(DataRow row)
        {
            string S(string column) => row.Table.Columns.Contains(column) ? row[column]?.ToString() ?? string.Empty : string.Empty;
            DateTime? D(string column) => row.Table.Columns.Contains(column) && row[column] != DBNull.Value
                ? Convert.ToDateTime(row[column], CultureInfo.InvariantCulture)
                : (DateTime?)null;

            return new ExternalServicer
            {
                Id = row.Table.Columns.Contains("id") && row["id"] != DBNull.Value ? Convert.ToInt32(row["id"], CultureInfo.InvariantCulture) : 0,
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
    }
}
