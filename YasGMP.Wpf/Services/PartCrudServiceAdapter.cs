using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Services;
using YasGMP.Services.Interfaces;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Bridges the Parts module view models in the WPF shell to the shared MAUI
    /// <see cref="YasGMP.Services.PartService"/> pipeline and related infrastructure.
    /// </summary>
    /// <remarks>
    /// Requests originate from module view models (for example the docked Parts editor),
    /// travel through this adapter, and end at the shared <see cref="YasGMP.Services.PartService"/>
    /// and <see cref="YasGMP.Services.DatabaseService"/> just as they do in the MAUI shell.
    /// Callers should await the asynchronous operations off the UI thread and marshal UI updates
    /// back to the dispatcher via <see cref="WpfUiDispatcher"/>. The <see cref="CrudSaveResult"/>
    /// returned by create/update operations includes the persisted identifier together with status,
    /// signature, and session metadata that must be localized with <see cref="LocalizationServiceExtensions"/>
    /// (or an <see cref="ILocalizationService"/>) before presenting any status or note text.
    /// Audit trails are recorded through the shared <see cref="YasGMP.Services.AuditService"/>, ensuring
    /// WPF operations remain visible to the cross-platform MAUI auditing experiences.
    /// </remarks>
    public sealed class PartCrudServiceAdapter : IPartCrudService
    {
        private readonly PartService _partService;
        private readonly DatabaseService _database;
        private readonly AuditService _audit;
        /// <summary>
        /// Initializes a new instance of the PartCrudServiceAdapter class.
        /// </summary>

        public PartCrudServiceAdapter(PartService partService, DatabaseService database, AuditService auditService)
        {
            _partService = partService ?? throw new ArgumentNullException(nameof(partService));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _audit = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }
        /// <summary>
        /// Executes the get all async operation.
        /// </summary>

        public async Task<IReadOnlyList<Part>> GetAllAsync()
        {
            var parts = await _partService.GetAllAsync().ConfigureAwait(false);
            return parts.AsReadOnly();
        }
        /// <summary>
        /// Executes the try get by id async operation.
        /// </summary>

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
        /// <summary>
        /// Executes the create async operation.
        /// </summary>

        public async Task<CrudSaveResult> CreateAsync(Part part, PartCrudContext context)
        {
            var signature = ApplyContext(part, context);
            var metadata = CreateMetadata(context, signature);

            await _partService.CreateAsync(part, context.UserId, metadata).ConfigureAwait(false);

            part.DigitalSignature = signature;
            await StampAsync(part, context, signature).ConfigureAwait(false);
            return new CrudSaveResult(part.Id, metadata);
        }
        /// <summary>
        /// Executes the update async operation.
        /// </summary>

        public async Task<CrudSaveResult> UpdateAsync(Part part, PartCrudContext context)
        {
            var signature = ApplyContext(part, context);
            var metadata = CreateMetadata(context, signature);

            await _partService.UpdateAsync(part, context.UserId, metadata).ConfigureAwait(false);

            part.DigitalSignature = signature;
            await StampAsync(part, context, signature).ConfigureAwait(false);
            return new CrudSaveResult(part.Id, metadata);
        }
        /// <summary>
        /// Executes the validate operation.
        /// </summary>

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
        /// <summary>
        /// Executes the normalize status operation.
        /// </summary>

        public string NormalizeStatus(string? status)
            => string.IsNullOrWhiteSpace(status) ? "active" : status.Trim().ToLower(CultureInfo.InvariantCulture);

        private async Task StampAsync(Part part, PartCrudContext context, string signature)
        {
            const string sql = "UPDATE parts SET source_ip=@ip, last_modified_by_id=@user, session_id=@session, digital_signature=@signature WHERE id=@id";
            var parameters = new[]
            {
                new MySqlParameter("@ip", context.Ip),
                new MySqlParameter("@user", context.UserId),
                new MySqlParameter("@session", context.SessionId ?? string.Empty),
                new MySqlParameter("@signature", signature ?? string.Empty),
                new MySqlParameter("@id", part.Id)
            };
            try
            {
                await _database.ExecuteNonQueryAsync(sql, parameters).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                const string legacySql = "UPDATE parts SET source_ip=@ip, last_modified_by_id=@user, session_id=@session WHERE id=@id";
                var legacyParameters = new[]
                {
                    new MySqlParameter("@ip", context.Ip),
                    new MySqlParameter("@user", context.UserId),
                    new MySqlParameter("@session", context.SessionId ?? string.Empty),
                    new MySqlParameter("@id", part.Id)
                };
                await _database.ExecuteNonQueryAsync(legacySql, legacyParameters).ConfigureAwait(false);
            }
            var details = string.Format(
                CultureInfo.InvariantCulture,
                "sigId={0}; sigHash={1}; sigMethod={2}; sigStatus={3}; sigNote={4}",
                context.SignatureId?.ToString(CultureInfo.InvariantCulture) ?? "-",
                string.IsNullOrWhiteSpace(signature) ? part.DigitalSignature ?? string.Empty : signature,
                context.SignatureMethod ?? "-",
                context.SignatureStatus ?? "-",
                string.IsNullOrWhiteSpace(context.SignatureNote) ? "-" : context.SignatureNote);
            await _audit.LogSystemEventAsync(
                context.UserId,
                "PART_STAMP",
                "parts",
                "PartCrud",
                part.Id,
                details,
                context.Ip,
                "wpf",
                context.DeviceInfo,
                context.SessionId).ConfigureAwait(false);
        }

        private static string ApplyContext(Part part, PartCrudContext context)
        {
            var signature = context.SignatureHash ?? part.DigitalSignature ?? string.Empty;
            part.DigitalSignature = signature;

            if (context.UserId > 0)
            {
                part.LastModifiedById = context.UserId;
            }

            if (!string.IsNullOrWhiteSpace(context.Ip))
            {
                part.SourceIp = context.Ip;
            }

            return signature;
        }

        private static SignatureMetadataDto CreateMetadata(PartCrudContext context, string signature)
            => new()
            {
                Id = context.SignatureId,
                Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
                Method = context.SignatureMethod,
                Status = context.SignatureStatus,
                Note = context.SignatureNote,
                Session = context.SessionId,
                Device = context.DeviceInfo,
                IpAddress = context.Ip
            };
}
}
