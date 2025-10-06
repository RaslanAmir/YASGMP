#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.Enums;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// Service for managing Deviations / Non-Conformances (GMP-compliant).
    /// Handles CRUD, workflow, and writes unified audit log entries.
    /// </summary>
    public class DeviationService
    {
        private readonly DatabaseService _db;
        private readonly IDeviationAuditService _audit;

        /// <summary>DI constructor.</summary>
        public DeviationService(DatabaseService databaseService, IDeviationAuditService auditService)
        {
            _db    = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _audit = auditService    ?? throw new ArgumentNullException(nameof(auditService));
        }

        // ============================================================================
        // CRUD
        // ============================================================================

        /// <summary>Gets all deviations (sorted newest first).</summary>
        public Task<List<Deviation>> GetAllAsync() => _db.GetAllDeviationsAsync();

        /// <summary>Gets a single deviation by id (or null).</summary>
        public Task<Deviation?> GetByIdAsync(int id) => _db.GetDeviationByIdAsync(id);

        /// <summary>
        /// Creates a new deviation and logs an audit entry.
        /// </summary>
        /// <param name="dev">Deviation model to create.</param>
        /// <param name="userId">Actor user id.</param>
        /// <param name="ip">Source IP.</param>
        /// <param name="deviceInfo">Device information.</param>
        /// <param name="sessionId">Session id.</param>
        public async Task<int> CreateAsync(Deviation dev, int userId, string ip = "system", string deviceInfo = "server", string? sessionId = null)
        {
            if (dev is null) throw new ArgumentNullException(nameof(dev));

            if (string.IsNullOrWhiteSpace(dev.Status))
                dev.Status = DeviationStatus.OPEN.ToString();

            dev.DigitalSignature = GenerateDigitalSignature(dev);

            var id = await _db.InsertOrUpdateDeviationAsync(dev, update: false, actorUserId: userId, ip: ip, device: deviceInfo, sessionId: sessionId)
                              .ConfigureAwait(false);

            await LogAudit(id, userId, DeviationActionType.CREATE, $"Created deviation #{id}").ConfigureAwait(false);
            return id;
        }

        /// <summary>Updates an existing deviation and logs an audit entry.</summary>
        public async Task UpdateAsync(Deviation dev, int userId, string ip = "system", string deviceInfo = "server", string? sessionId = null)
        {
            if (dev is null) throw new ArgumentNullException(nameof(dev));
            if (dev.Id <= 0) throw new ArgumentException("Deviation Id must be set for update.", nameof(dev));

            dev.DigitalSignature = GenerateDigitalSignature(dev);

            await _db.InsertOrUpdateDeviationAsync(dev, update: true, actorUserId: userId, ip: ip, device: deviceInfo, sessionId: sessionId)
                     .ConfigureAwait(false);

            await LogAudit(dev.Id, userId, DeviationActionType.UPDATE, $"Updated deviation #{dev.Id}").ConfigureAwait(false);
        }

        /// <summary>Deletes the deviation and logs an audit entry.</summary>
        public async Task DeleteAsync(int deviationId, int userId, string ip = "system", string deviceInfo = "server", string? sessionId = null)
        {
            await _db.DeleteDeviationAsync(deviationId, actorUserId: userId, ip: ip, device: deviceInfo, sessionId: sessionId)
                     .ConfigureAwait(false);

            await LogAudit(deviationId, userId, DeviationActionType.DELETE, $"Deleted deviation #{deviationId}").ConfigureAwait(false);
        }

        // ============================================================================
        // WORKFLOW
        // ============================================================================

        /// <summary>
        /// Starts investigation: sets status to <see cref="DeviationStatus.INVESTIGATION"/>
        /// and records investigator information for traceability.
        /// </summary>
        public async Task StartInvestigationAsync(int deviationId, int userId, string investigator)
        {
            var dev = await _db.GetDeviationByIdAsync(deviationId)
                      ?? throw new InvalidOperationException("Deviation not found.");

            dev.Status = DeviationStatus.INVESTIGATION.ToString();

            // Your model has AssignedInvestigatorId (int?) and AssignedInvestigatorName (string),
            // not AssignedToUserId. We store the name here to avoid User navigation assignment.
            dev.AssignedInvestigatorName = investigator;

            dev.DigitalSignature = GenerateDigitalSignature(dev);

            await _db.InsertOrUpdateDeviationAsync(dev, update: true, actorUserId: userId).ConfigureAwait(false);
            await LogAudit(dev.Id, userId, DeviationActionType.INVESTIGATION_START,
                $"Investigation started (Investigator: {investigator})").ConfigureAwait(false);
        }

        /// <summary>Defines the root cause and sets status to <see cref="DeviationStatus.ROOT_CAUSE"/>.</summary>
        public async Task DefineRootCauseAsync(int deviationId, int userId, string rootCause)
        {
            var dev = await _db.GetDeviationByIdAsync(deviationId)
                      ?? throw new InvalidOperationException("Deviation not found.");

            dev.RootCause = rootCause;
            dev.Status = DeviationStatus.ROOT_CAUSE.ToString();
            dev.DigitalSignature = GenerateDigitalSignature(dev);

            await _db.InsertOrUpdateDeviationAsync(dev, update: true, actorUserId: userId).ConfigureAwait(false);
            await LogAudit(dev.Id, userId, DeviationActionType.ROOT_CAUSE_DEFINED,
                $"Root cause defined: {rootCause}").ConfigureAwait(false);
        }

        /// <summary>Links this deviation to a CAPA case and sets status to <see cref="DeviationStatus.CAPA_LINKED"/>.</summary>
        public async Task LinkToCapaAsync(int deviationId, int capaId, int userId)
        {
            var dev = await _db.GetDeviationByIdAsync(deviationId)
                      ?? throw new InvalidOperationException("Deviation not found.");

            dev.LinkedCapaId = capaId;
            dev.Status = DeviationStatus.CAPA_LINKED.ToString();
            dev.DigitalSignature = GenerateDigitalSignature(dev);

            await _db.InsertOrUpdateDeviationAsync(dev, update: true, actorUserId: userId).ConfigureAwait(false);
            await LogAudit(dev.Id, userId, DeviationActionType.CAPA_LINKED,
                $"Linked to CAPA #{capaId}").ConfigureAwait(false);
        }

        /// <summary>Closes the deviation and records a closure comment.</summary>
        public async Task CloseDeviationAsync(int deviationId, int userId, string closureComment)
        {
            var dev = await _db.GetDeviationByIdAsync(deviationId)
                      ?? throw new InvalidOperationException("Deviation not found.");

            dev.Status = DeviationStatus.CLOSED.ToString();
            dev.ClosureComment = closureComment;
            dev.ClosedAt = DateTime.UtcNow;
            dev.DigitalSignature = GenerateDigitalSignature(dev);

            await _db.InsertOrUpdateDeviationAsync(dev, update: true, actorUserId: userId).ConfigureAwait(false);
            await LogAudit(dev.Id, userId, DeviationActionType.CLOSE,
                $"Deviation closed. Comment: {closureComment}").ConfigureAwait(false);
        }

        /// <summary>
        /// Assigns the deviation to a user (by id) and logs an audit entry.
        /// NOTE: Your model exposes <c>AssignedInvestigatorId</c>, not <c>AssignedToUserId</c>.
        /// </summary>
        public async Task AssignAsync(int deviationId, int assigneeUserId, int actorUserId)
        {
            var dev = await _db.GetDeviationByIdAsync(deviationId)
                      ?? throw new InvalidOperationException("Deviation not found.");

            dev.AssignedInvestigatorId = assigneeUserId; // FIX: match your model
            dev.DigitalSignature = GenerateDigitalSignature(dev);

            await _db.InsertOrUpdateDeviationAsync(dev, update: true, actorUserId: actorUserId).ConfigureAwait(false);
            await LogAudit(dev.Id, actorUserId, DeviationActionType.UPDATE,
                $"Assigned to user #{assigneeUserId}").ConfigureAwait(false);
        }

        /// <summary>Escalates the deviation (status unchanged; audit entry is recorded).</summary>
        public async Task EscalateAsync(int deviationId, int actorUserId, string reason)
        {
            var dev = await _db.GetDeviationByIdAsync(deviationId)
                      ?? throw new InvalidOperationException("Deviation not found.");

            dev.DigitalSignature = GenerateDigitalSignature(dev);

            await _db.InsertOrUpdateDeviationAsync(dev, update: true, actorUserId: actorUserId).ConfigureAwait(false);
            await LogAudit(dev.Id, actorUserId, DeviationActionType.UPDATE,
                $"Escalated: {reason}").ConfigureAwait(false);
        }

        // ============================================================================
        // EXPORT (simple in-memory CSV)
        // ============================================================================

        /// <summary>
        /// Builds a CSV string for the provided deviations.
        /// </summary>
        public Task<string> BuildCsvAsync(IEnumerable<Deviation> rows)
        {
            var list = (rows ?? Enumerable.Empty<Deviation>()).ToList();
            var sb = new StringBuilder();
            sb.AppendLine("Id,Title,Severity,Status,ReportedById,ReportedAt,AssignedInvestigatorId,LinkedCapaId,ClosedAt");

            foreach (var d in list)
            {
                string Esc(string? v) => string.IsNullOrEmpty(v) ? "" : "\"" + v.Replace("\"", "\"\"") + "\"";

                sb.Append(d.Id).Append(',')
                  .Append(Esc(d.Title)).Append(',')
                  .Append(Esc(d.Severity)).Append(',')
                  .Append(Esc(d.Status)).Append(',')
                  .Append(d.ReportedById).Append(',')
                  .Append(d.ReportedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "").Append(',')
                  .Append(d.AssignedInvestigatorId?.ToString() ?? "").Append(',')
                  .Append(d.LinkedCapaId?.ToString() ?? "").Append(',')
                  .Append(d.ClosedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "")
                  .AppendLine();
            }

            return Task.FromResult(sb.ToString());
        }

        // ============================================================================
        // AUDIT
        // ============================================================================

        /// <summary>
        /// Writes a unified deviation audit entry through <see cref="IDeviationAuditService"/>.
        /// </summary>
        private async Task LogAudit(int deviationId, int userId, DeviationActionType action, string details)
        {
            await _audit.CreateAsync(new DeviationAudit
            {
                DeviationId      = deviationId,
                UserId           = userId,
                Action           = action.ToString(), // DB column is string
                Details          = details,
                ChangedAt        = DateTime.UtcNow,
                DigitalSignature = GenerateDigitalSignature(details)
            }).ConfigureAwait(false);
        }

        // ============================================================================
        // SIGNATURE
        // ============================================================================

        /// <summary>Creates a deterministic SHA-256 signature string for audit purposes.</summary>
        private static string GenerateDigitalSignature(object? obj)
        {
            var payload = obj switch
            {
                null     => string.Empty,
                string s => s,
                _        => System.Text.Json.JsonSerializer.Serialize(obj)
            };

            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}

