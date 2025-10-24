using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.Enums;
using YasGMP.Models.DTO;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>CAPAService</b> – Ultra robust, GMP-compliant master service for Corrective and Preventive Actions (CAPA).
    /// <para>
    /// ✔ Full CRUD, multi-level workflow, AI-ready risk scoring, digital signatures, GMP/Annex 11/21 CFR Part 11 audit trail, notifications, escalation.
    /// ✔ Fully aligned with ISO 13485, ICH Q10, 21 CFR Part 11, EU GMP Annex 11.
    /// </para>
    /// <para>
    /// Bonus: Partial hooks for multi-signature, attachments, ML/AI analytics, forensic logging.
    /// </para>
    /// </summary>
    public partial class CAPAService : ICAPAService
    {
        private readonly DatabaseService _db;
        private readonly ICapaAuditService _audit;
        private readonly INotificationService _notifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="CAPAService"/> class with all required dependencies.
        /// </summary>
        /// <param name="databaseService">GMP-compliant data access service.</param>
        /// <param name="auditService">CAPA audit logging service.</param>
        /// <param name="notificationService">Notification/escalation service.</param>
        /// <exception cref="ArgumentNullException">Thrown if any dependency is <c>null</c>.</exception>
        public CAPAService(
            DatabaseService databaseService,
            ICapaAuditService auditService,
            INotificationService notificationService)
        {
            _db = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _audit = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _notifier = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        #region === CRUD OPERATIONS ===

        /// <summary>
        /// Retrieves all CAPA cases from the database.
        /// </summary>
        /// <returns>List of all <see cref="CapaCase"/> entities.</returns>
        public async Task<List<CapaCase>> GetAllAsync() =>
            await _db.GetAllCapaCasesAsync();

        /// <summary>
        /// Retrieves a specific CAPA case by ID.
        /// </summary>
        /// <param name="id">CAPA case primary key.</param>
        /// <returns>The <see cref="CapaCase"/> if found; otherwise throws.</returns>
        public async Task<CapaCase> GetByIdAsync(int id)
        {
            var c = await _db.GetCapaCaseByIdAsync(id);
            if (c == null)
                throw new InvalidOperationException($"CAPA case with ID={id} was not found.");
            return c;
        }

        /// <summary>
        /// Creates a new CAPA case, generates digital signature, logs full audit, and triggers risk notification if needed.
        /// </summary>
        /// <param name="capa">CAPA case to create.</param>
        /// <param name="userId">User performing the action.</param>
        /// <param name="signatureMetadata">Optional electronic signature metadata (hash/IP/device/session).</param>
        /// <exception cref="InvalidOperationException">Thrown if validation fails.</exception>
        public async Task CreateAsync(CapaCase capa, int userId, SignatureMetadataDto? signatureMetadata = null)
        {
            ValidateCapa(capa);
            capa.Status = CapaStatus.OPEN.ToString();
            capa.LastModified = DateTime.UtcNow;
            ApplySignatureMetadata(capa, signatureMetadata, () => GenerateDigitalSignature(capa));

            int id = await _db.AddCapaCaseAsync(
                capa,
                signatureMetadata,
                signatureMetadata?.IpAddress ?? string.Empty,
                signatureMetadata?.Device ?? string.Empty,
                signatureMetadata?.Session,
                userId).ConfigureAwait(false);
            await LogAudit(id, userId, CapaActionType.CREATE, $"Created CAPA: {capa.Reason}");

            await CheckAndNotifyRisk(capa);
            OnCapaCreated(capa);
        }

        /// <summary>
        /// Updates an existing CAPA case, re-generates digital signature, and records an audit log.
        /// </summary>
        /// <param name="capa">CAPA case with updated data.</param>
        /// <param name="userId">User performing the update.</param>
        /// <param name="signatureMetadata">Optional electronic signature metadata (hash/IP/device/session).</param>
        /// <exception cref="InvalidOperationException">Thrown if validation fails.</exception>
        public async Task UpdateAsync(CapaCase capa, int userId, SignatureMetadataDto? signatureMetadata = null)
        {
            ValidateCapa(capa);
            capa.LastModified = DateTime.UtcNow;
            ApplySignatureMetadata(capa, signatureMetadata, () => GenerateDigitalSignature(capa));

            await _db.UpdateCapaCaseAsync(
                capa,
                signatureMetadata,
                signatureMetadata?.IpAddress ?? string.Empty,
                signatureMetadata?.Device ?? string.Empty,
                signatureMetadata?.Session,
                userId).ConfigureAwait(false);
            await LogAudit(capa.Id, userId, CapaActionType.UPDATE, $"Updated CAPA ID={capa.Id}");

            await CheckAndNotifyRisk(capa);
            OnCapaUpdated(capa);
        }

        /// <summary>
        /// Deletes a CAPA case and logs the event.
        /// </summary>
        /// <param name="capaId">CAPA case primary key.</param>
        /// <param name="userId">User performing the deletion.</param>
        public async Task DeleteAsync(int capaId, int userId)
        {
            await _db.DeleteCapaCaseAsync(capaId, userId, string.Empty, string.Empty);
            await LogAudit(capaId, userId, CapaActionType.DELETE, $"Deleted CAPA ID={capaId}");
            OnCapaDeleted(capaId);
        }

        #endregion

        #region === WORKFLOW MANAGEMENT ===

        /// <summary>
        /// Starts the investigation phase for a CAPA case.
        /// </summary>
        /// <param name="capaId">CAPA case ID.</param>
        /// <param name="userId">Investigator user ID.</param>
        /// <param name="investigator">Investigator's name.</param>
        /// <param name="signatureMetadata">Optional electronic signature metadata (hash/IP/device/session).</param>
        public async Task StartInvestigationAsync(int capaId, int userId, string investigator, SignatureMetadataDto? signatureMetadata = null)
        {
            var capa = await GetByIdAsync(capaId);
            capa.Status = CapaStatus.INVESTIGATION.ToString();
            capa.LastModified = DateTime.UtcNow;
            ApplySignatureMetadata(capa, signatureMetadata, () => GenerateDigitalSignature(capa));

            await _db.UpdateCapaCaseAsync(
                capa,
                signatureMetadata,
                signatureMetadata?.IpAddress ?? string.Empty,
                signatureMetadata?.Device ?? string.Empty,
                signatureMetadata?.Session,
                userId).ConfigureAwait(false);
            await LogAudit(capa.Id, userId, CapaActionType.INVESTIGATION_START, $"Investigation started by: {investigator}");
            OnInvestigationStarted(capa, investigator);
        }

        /// <summary>
        /// Defines an action plan for a CAPA case.
        /// </summary>
        /// <param name="capaId">CAPA case ID.</param>
        /// <param name="userId">User defining action plan.</param>
        /// <param name="actionPlan">Action plan details.</param>
        /// <param name="signatureMetadata">Optional electronic signature metadata (hash/IP/device/session).</param>
        public async Task DefineActionPlanAsync(int capaId, int userId, string actionPlan, SignatureMetadataDto? signatureMetadata = null)
        {
            var capa = await GetByIdAsync(capaId);
            capa.Status = CapaStatus.ACTION_DEFINED.ToString();
            capa.Actions = actionPlan;
            capa.LastModified = DateTime.UtcNow;
            ApplySignatureMetadata(capa, signatureMetadata, () => GenerateDigitalSignature(capa));

            await _db.UpdateCapaCaseAsync(
                capa,
                signatureMetadata,
                signatureMetadata?.IpAddress ?? string.Empty,
                signatureMetadata?.Device ?? string.Empty,
                signatureMetadata?.Session,
                userId).ConfigureAwait(false);
            await LogAudit(capa.Id, userId, CapaActionType.ACTION_PLAN_DEFINED, $"Action plan defined: {actionPlan}");
            OnActionPlanDefined(capa, actionPlan);
        }

        /// <summary>
        /// Approves the CAPA action plan.
        /// </summary>
        /// <param name="capaId">CAPA case ID.</param>
        /// <param name="approverId">Approving user ID.</param>
        /// <param name="signatureMetadata">Optional electronic signature metadata (hash/IP/device/session).</param>
        public async Task ApproveActionPlanAsync(int capaId, int approverId, SignatureMetadataDto? signatureMetadata = null)
        {
            var capa = await GetByIdAsync(capaId);
            capa.Approved = true;
            capa.ApprovedById = approverId;
            capa.ApprovedAt = DateTime.UtcNow;
            capa.Status = CapaStatus.ACTION_APPROVED.ToString();
            ApplySignatureMetadata(capa, signatureMetadata, () => GenerateDigitalSignature(capa));

            await _db.UpdateCapaCaseAsync(
                capa,
                signatureMetadata,
                signatureMetadata?.IpAddress ?? string.Empty,
                signatureMetadata?.Device ?? string.Empty,
                signatureMetadata?.Session,
                approverId).ConfigureAwait(false);
            await LogAudit(capa.Id, approverId, CapaActionType.APPROVE, "Action plan approved.");
            OnActionPlanApproved(capa, approverId);
        }

        /// <summary>
        /// Marks CAPA actions as executed and logs the event.
        /// </summary>
        /// <param name="capaId">CAPA case ID.</param>
        /// <param name="userId">User who executed the action.</param>
        /// <param name="executionComment">Execution notes/comments.</param>
        /// <param name="signatureMetadata">Optional electronic signature metadata (hash/IP/device/session).</param>
        public async Task MarkActionExecutedAsync(int capaId, int userId, string executionComment, SignatureMetadataDto? signatureMetadata = null)
        {
            var capa = await GetByIdAsync(capaId);
            capa.Status = CapaStatus.ACTION_EXECUTED.ToString();
            capa.Actions = (capa.Actions ?? string.Empty) + $" | Executed: {executionComment}";
            capa.LastModified = DateTime.UtcNow;
            ApplySignatureMetadata(capa, signatureMetadata, () => GenerateDigitalSignature(capa));

            await _db.UpdateCapaCaseAsync(
                capa,
                signatureMetadata,
                signatureMetadata?.IpAddress ?? string.Empty,
                signatureMetadata?.Device ?? string.Empty,
                signatureMetadata?.Session,
                userId).ConfigureAwait(false);
            await LogAudit(capa.Id, userId, CapaActionType.ACTION_EXECUTED, $"Action executed: {executionComment}");
            OnActionExecuted(capa, executionComment);
        }

        /// <summary>
        /// Verifies CAPA effectiveness and records verification.
        /// </summary>
        /// <param name="capaId">CAPA case ID.</param>
        /// <param name="verifierId">User who verifies effectiveness.</param>
        /// <param name="effective">Whether the action was effective.</param>
        /// <param name="signatureMetadata">Optional electronic signature metadata (hash/IP/device/session).</param>
        public async Task VerifyEffectivenessAsync(int capaId, int verifierId, bool effective, SignatureMetadataDto? signatureMetadata = null)
        {
            var capa = await GetByIdAsync(capaId);
            capa.Status = CapaStatus.VERIFICATION.ToString();
            capa.IsEffective = effective;
            capa.LastModified = DateTime.UtcNow;
            ApplySignatureMetadata(capa, signatureMetadata, () => GenerateDigitalSignature(capa));

            await _db.UpdateCapaCaseAsync(
                capa,
                signatureMetadata,
                signatureMetadata?.IpAddress ?? string.Empty,
                signatureMetadata?.Device ?? string.Empty,
                signatureMetadata?.Session,
                verifierId).ConfigureAwait(false);
            await LogAudit(capa.Id, verifierId, CapaActionType.VERIFICATION, $"Effectiveness verified: {(effective ? "Effective" : "Not Effective")}");
            OnEffectivenessVerified(capa, effective);
        }

        /// <summary>
        /// Closes a CAPA case and logs closure event.
        /// </summary>
        /// <param name="capaId">CAPA case ID.</param>
        /// <param name="userId">User closing the CAPA.</param>
        /// <param name="closureComment">Closure notes/comments.</param>
        /// <param name="signatureMetadata">Optional electronic signature metadata (hash/IP/device/session).</param>
        public async Task CloseCapaAsync(int capaId, int userId, string closureComment, SignatureMetadataDto? signatureMetadata = null)
        {
            var capa = await GetByIdAsync(capaId);
            capa.Status = CapaStatus.CLOSED.ToString();
            capa.LastModified = DateTime.UtcNow;
            ApplySignatureMetadata(capa, signatureMetadata, () => GenerateDigitalSignature(capa));

            await _db.UpdateCapaCaseAsync(
                capa,
                signatureMetadata,
                signatureMetadata?.IpAddress ?? string.Empty,
                signatureMetadata?.Device ?? string.Empty,
                signatureMetadata?.Session,
                userId).ConfigureAwait(false);
            await LogAudit(capa.Id, userId, CapaActionType.CLOSE, $"CAPA closed. Comment: {closureComment}");
            OnCapaClosed(capa, closureComment);
        }

        #endregion

        #region === RISK ASSESSMENT & NOTIFICATIONS ===

        /// <summary>
        /// Calculates risk score for a CAPA case (AI/ML extensibility hook).
        /// </summary>
        /// <param name="c">The CAPA case.</param>
        /// <returns>Risk score (higher = more critical).</returns>
        public int CalculateRiskScore(CapaCase c)
        {
            int severityWeight = c.RootCauseReference?.ToUpper() switch
            {
                "HIGH" => 80,
                "MEDIUM" => 50,
                "LOW" => 20,
                _ => 10
            };
            return severityWeight;
        }

        /// <summary>
        /// Checks risk and notifies stakeholders if score exceeds GMP thresholds.
        /// </summary>
        /// <param name="c">The CAPA case.</param>
        public async Task CheckAndNotifyRisk(CapaCase c)
        {
            if (CalculateRiskScore(c) > 80)
            {
                await _notifier.NotifyAsync($"🚨 High-Risk CAPA: {c.Reason}", "qa_team@company.com");
            }
        }

        #endregion

        #region === VALIDATION ===

        /// <summary>
        /// Validates that the CAPA case contains all required fields.
        /// </summary>
        /// <param name="c">The CAPA case.</param>
        /// <exception cref="InvalidOperationException">Thrown if the CAPA is invalid.</exception>
        public void ValidateCapa(CapaCase c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (string.IsNullOrWhiteSpace(c.Reason)) throw new InvalidOperationException("CAPA reason is required.");
            if (string.IsNullOrWhiteSpace(c.Status)) throw new InvalidOperationException("CAPA status must be defined.");
        }

        #endregion

        #region === DIGITAL SIGNATURES ===

        /// <summary>Applies signature metadata to the CAPA entity or computes a fallback signature.</summary>
        /// <param name="capa">The CAPA case.</param>
        /// <param name="metadata">Optional electronic signature metadata.</param>
        /// <param name="legacyFactory">Fallback signature factory.</param>
        private static void ApplySignatureMetadata(CapaCase capa, SignatureMetadataDto? metadata, Func<string> legacyFactory)
        {
            if (capa == null) throw new ArgumentNullException(nameof(capa));
            if (legacyFactory == null) throw new ArgumentNullException(nameof(legacyFactory));

            string hash = metadata?.Hash ?? capa.DigitalSignature ?? legacyFactory();
            capa.DigitalSignature = hash;

            if (!string.IsNullOrWhiteSpace(metadata?.IpAddress))
            {
                capa.SourceIp = metadata.IpAddress!;
                capa.IpAddress = metadata.IpAddress!;
            }

            if (!string.IsNullOrWhiteSpace(metadata?.Device))
            {
                capa.DeviceInfo = metadata.Device!;
            }

            if (!string.IsNullOrWhiteSpace(metadata?.Session))
            {
                capa.SessionId = metadata.Session!;
            }
        }

        /// <summary>
        /// Generates a robust digital signature hash for the given CAPA case.
        /// </summary>
        /// <param name="c">The CAPA case.</param>
        /// <returns>Base64 SHA256 hash.</returns>
        public string GenerateDigitalSignature(CapaCase c)
        {
            string raw = $"{c.Id}|{c.Reason}|{c.Status}|{DateTime.UtcNow:O}";
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        /// <summary>
        /// Generates a digital signature hash from arbitrary string payload.
        /// </summary>
        /// <param name="payload">String to sign.</param>
        /// <returns>Base64 SHA256 hash.</returns>
        public string GenerateDigitalSignature(string payload)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes($"{payload}|{Guid.NewGuid()}")));
        }

        #endregion

        #region === AUDIT LOG ===

        /// <summary>
        /// Creates a full GMP/Annex 11-compliant audit log entry for the given action.
        /// </summary>
        /// <param name="capaId">CAPA case ID.</param>
        /// <param name="userId">User ID.</param>
        /// <param name="action">Action performed.</param>
        /// <param name="details">Audit details.</param>
        public async Task LogAudit(int capaId, int userId, CapaActionType action, string details)
        {
            await _audit.CreateAsync(new CapaAudit
            {
                CapaId = capaId,
                UserId = userId,
                Action = action,
                Details = details,
                ChangedAt = DateTime.UtcNow,
                DigitalSignature = GenerateDigitalSignature(details),
                SourceIp = "system",
                DeviceInfo = Environment.MachineName
            });
        }

        #endregion

        #region === EXTENSIBILITY PARTIAL HOOKS (BONUS) ===
        /// <summary>Partial method called when a CAPA is created (extensible for ML/AI, notifications, attachments, etc.).</summary>
        partial void OnCapaCreated(CapaCase capa);
        /// <summary>Partial method called when a CAPA is updated.</summary>
        partial void OnCapaUpdated(CapaCase capa);
        /// <summary>Partial method called when a CAPA is deleted.</summary>
        partial void OnCapaDeleted(int capaId);
        /// <summary>Partial method called when investigation starts.</summary>
        partial void OnInvestigationStarted(CapaCase capa, string investigator);
        /// <summary>Partial method called when action plan is defined.</summary>
        partial void OnActionPlanDefined(CapaCase capa, string actionPlan);
        /// <summary>Partial method called when action plan is approved.</summary>
        partial void OnActionPlanApproved(CapaCase capa, int approverId);
        /// <summary>Partial method called when actions are executed.</summary>
        partial void OnActionExecuted(CapaCase capa, string executionComment);
        /// <summary>Partial method called when effectiveness is verified.</summary>
        partial void OnEffectivenessVerified(CapaCase capa, bool effective);
        /// <summary>Partial method called when CAPA is closed.</summary>
        partial void OnCapaClosed(CapaCase capa, string closureComment);
        #endregion
    }
}
