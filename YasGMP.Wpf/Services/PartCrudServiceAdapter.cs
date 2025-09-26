using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Concrete adapter that routes CRUD requests from the WPF shell to the
    /// shared <see cref="PartService"/> and <see cref="DatabaseService"/>.
    /// </summary>
    public sealed class PartCrudServiceAdapter : IPartCrudService
    {
        private readonly PartService _partService;
        private readonly DatabaseService _database;
        private readonly AuditService _audit;

        public PartCrudServiceAdapter(PartService partService, DatabaseService database, AuditService auditService)
        {
            _partService = partService ?? throw new ArgumentNullException(nameof(partService));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _audit = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        public async Task<IReadOnlyList<Part>> GetAllAsync()
        {
            var parts = await _partService.GetAllAsync().ConfigureAwait(false);
            return parts.AsReadOnly();
        }

        public async Task<Part?> TryGetByIdAsync(int id)
        {
            try
            {
                return await _partService.GetByIdAsync(id).ConfigureAwait(false);
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        public async Task<int> CreateAsync(Part part, PartCrudContext context)
        {
            await _partService.CreateAsync(part, context.UserId).ConfigureAwait(false);
            await StampAsync(part, context).ConfigureAwait(false);
            return part.Id;
        }

        public async Task UpdateAsync(Part part, PartCrudContext context)
        {
            await _partService.UpdateAsync(part, context.UserId).ConfigureAwait(false);
            await StampAsync(part, context).ConfigureAwait(false);
        }

        public void Validate(Part part)
        {
            if (part is null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            if (string.IsNullOrWhiteSpace(part.Name))
            {
                throw new InvalidOperationException("Part name is required.");
            }

            if (string.IsNullOrWhiteSpace(part.Code))
            {
                throw new InvalidOperationException("Part code is required.");
            }

            if (!part.DefaultSupplierId.HasValue && string.IsNullOrWhiteSpace(part.DefaultSupplierName))
            {
                throw new InvalidOperationException("Default supplier is required.");
            }
        }

        public string NormalizeStatus(string? status)
            => string.IsNullOrWhiteSpace(status) ? "active" : status.Trim().ToLower(CultureInfo.InvariantCulture);

        private async Task StampAsync(Part part, PartCrudContext context)
        {
            const string sql = "UPDATE parts SET source_ip=@ip, last_modified_by_id=@user, session_id=@session WHERE id=@id";
            var parameters = new[]
            {
                new MySqlParameter("@ip", context.Ip),
                new MySqlParameter("@user", context.UserId),
                new MySqlParameter("@session", context.SessionId ?? string.Empty),
                new MySqlParameter("@id", part.Id)
            };
            await _database.ExecuteNonQueryAsync(sql, parameters).ConfigureAwait(false);
            await _audit.LogSystemEventAsync(
                context.UserId,
                "PART_STAMP",
                "parts",
                "PartCrud",
                part.Id,
                part.DigitalSignature,
                context.Ip,
                "wpf",
                context.DeviceInfo,
                context.SessionId).ConfigureAwait(false);
        }
    }
}
