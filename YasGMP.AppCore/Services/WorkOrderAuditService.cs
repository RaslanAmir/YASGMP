using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YasGMP.Models;
using YasGMP.Models.Enums;
using YasGMP.Services.Interfaces;
using YasGMP.Data;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>WorkOrderAuditService</b> – Ultra-robustan GMP-compliant servis za upravljanje audit zapisima radnih naloga.
    /// <para>
    /// ✅ EF Core pristup, digitalni potpis i integritetni hash (SHA256).<br/>
    /// ✅ Usklađeno s 21 CFR Part 11 / EU GMP Annex 11 / ISO 13485.
    /// </para>
    /// </summary>
    public class WorkOrderAuditService : IWorkOrderAuditService
    {
        private readonly YasGmpDbContext _context;

        /// <summary>Inicijalizira servis s instancom EF Core DbContext-a.</summary>
        public WorkOrderAuditService(YasGmpDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region === CREATE AUDIT ===

        /// <summary>
        /// Stvara novi audit zapis s forenzičkim metapodacima te kriptografskim potpisom i hashom integriteta.
        /// </summary>
        /// <param name="audit">Audit entitet spreman za pohranu.</param>
        public async Task CreateAsync(WorkOrderAudit audit)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));

            audit.ChangedAt = DateTime.UtcNow;
            audit.SourceIp ??= "system";
            audit.DeviceInfo ??= Environment.MachineName;
            audit.DigitalSignature = GenerateDigitalSignature(audit);
            audit.IntegrityHash = GenerateIntegrityHash(audit);

            await _context.WorkOrderAudits.AddAsync(audit).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        #endregion

        #region === UPDATE AUDIT ===

        /// <summary>Ažurira postojeći audit zapis i regenerira hash integriteta.</summary>
        public async Task UpdateAsync(WorkOrderAudit audit)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));

            audit.ChangedAt = DateTime.UtcNow;
            audit.IntegrityHash = GenerateIntegrityHash(audit);

            _context.WorkOrderAudits.Update(audit);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        #endregion

        #region === DELETE AUDIT ===

        /// <summary>Trajno uklanja audit zapis po ID-u (po potrebi se može premjestiti u arhivu).</summary>
        public async Task DeleteAsync(int id)
        {
            var entity = await _context.WorkOrderAudits.FindAsync(id).ConfigureAwait(false);
            if (entity != null)
            {
                _context.WorkOrderAudits.Remove(entity);
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        #endregion

        #region === GET AUDIT RECORDS ===

        /// <summary>Vraća audit zapis po ID-u. Baca iznimku ako zapis ne postoji (eliminira CS8603).</summary>
        public async Task<WorkOrderAudit> GetByIdAsync(int id)
        {
            var entity = await _context.WorkOrderAudits
                .Include(a => a.User)
                .Include(a => a.WorkOrder)
                .FirstOrDefaultAsync(a => a.Id == id)
                .ConfigureAwait(false);

            if (entity == null)
                throw new KeyNotFoundException($"WorkOrderAudit #{id} nije pronađen.");

            return entity;
        }

        /// <summary>Vraća audite za određeni radni nalog (najnoviji prvi).</summary>
        public async Task<IReadOnlyList<WorkOrderAudit>> GetByWorkOrderIdAsync(int workOrderId) =>
            await _context.WorkOrderAudits
                .Where(a => a.WorkOrderId == workOrderId)
                .Include(a => a.User)
                .OrderByDescending(a => a.ChangedAt)
                .ToListAsync()
                .ConfigureAwait(false);

        /// <summary>Vraća audite filtrirane po vrsti akcije.</summary>
        public async Task<IReadOnlyList<WorkOrderAudit>> GetByActionTypeAsync(WorkOrderActionType actionType) =>
            await _context.WorkOrderAudits
                .Where(a => a.Action == actionType)
                .Include(a => a.User)
                .OrderByDescending(a => a.ChangedAt)
                .ToListAsync()
                .ConfigureAwait(false);

        /// <summary>Vraća audite korisnika.</summary>
        public async Task<IReadOnlyList<WorkOrderAudit>> GetByUserIdAsync(int userId) =>
            await _context.WorkOrderAudits
                .Where(a => a.UserId == userId)
                .Include(a => a.WorkOrder)
                .OrderByDescending(a => a.ChangedAt)
                .ToListAsync()
                .ConfigureAwait(false);

        /// <summary>Vraća audite u zadanom vremenskom intervalu.</summary>
        public async Task<IReadOnlyList<WorkOrderAudit>> GetByDateRangeAsync(DateTime from, DateTime to) =>
            await _context.WorkOrderAudits
                .Where(a => a.ChangedAt >= from && a.ChangedAt <= to)
                .Include(a => a.User)
                .Include(a => a.WorkOrder)
                .OrderByDescending(a => a.ChangedAt)
                .ToListAsync()
                .ConfigureAwait(false);

        /// <summary>Vraća audite povezane s incidentom.</summary>
        public async Task<IReadOnlyList<WorkOrderAudit>> GetByIncidentIdAsync(int incidentId) =>
            await _context.WorkOrderAudits
                .Where(a => a.IncidentId == incidentId)
                .Include(a => a.User)
                .Include(a => a.WorkOrder)
                .OrderByDescending(a => a.ChangedAt)
                .ToListAsync()
                .ConfigureAwait(false);

        /// <summary>Vraća audite povezane s CAPA slučajem.</summary>
        public async Task<IReadOnlyList<WorkOrderAudit>> GetByCapaIdAsync(int capaId) =>
            await _context.WorkOrderAudits
                .Where(a => a.CapaId == capaId)
                .Include(a => a.User)
                .Include(a => a.WorkOrder)
                .OrderByDescending(a => a.ChangedAt)
                .ToListAsync()
                .ConfigureAwait(false);

        #endregion

        #region === INTEGRITY & SIGNATURES ===

        /// <summary>Generira digitalni potpis audit zapisa (SHA256 + salt).</summary>
        private static string GenerateDigitalSignature(WorkOrderAudit audit)
        {
            using var sha = SHA256.Create();
            string raw = $"{audit.UserId}|{audit.WorkOrderId}|{audit.Action}|{DateTime.UtcNow:O}|{Guid.NewGuid()}";
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        /// <summary>Generira hash integriteta audit zapisa.</summary>
        private static string GenerateIntegrityHash(WorkOrderAudit audit)
        {
            using var sha = SHA256.Create();
            string raw = $"{audit.UserId}|{audit.WorkOrderId}|{audit.Action}|{audit.ChangedAt:O}|{audit.Note}|{audit.DigitalSignature}";
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        /// <summary>Provjerava odgovara li izračunati hash spremljenom.</summary>
        public bool ValidateIntegrity(WorkOrderAudit audit) =>
            audit != null && GenerateIntegrityHash(audit) == audit.IntegrityHash;

        /// <summary>Minimalna provjera potpisa (demo); za produkciju koristiti PKI/certifikate.</summary>
        public bool VerifyDigitalSignature(WorkOrderAudit audit) =>
            !string.IsNullOrWhiteSpace(audit?.DigitalSignature);

        /// <summary>Vraća "snapshot" prethodnog stanja (pretpostavka: <see cref="WorkOrderAudit.OldValue"/> sadrži JSON).</summary>
        public async Task<string> GetPreviousStateSnapshotAsync(int auditId)
        {
            var audit = await _context.WorkOrderAudits.FindAsync(auditId).ConfigureAwait(false);
            return audit?.OldValue ?? string.Empty;
        }

        /// <summary>Brzo logiranje jednostavne akcije.</summary>
        public async Task LogQuickAsync(int workOrderId, int userId, WorkOrderActionType action, string note)
        {
            var draftForSignature = new WorkOrderAudit
            {
                WorkOrderId = workOrderId,
                UserId = userId,
                Action = action
            };

            var audit = new WorkOrderAudit
            {
                WorkOrderId = workOrderId,
                UserId = userId,
                Action = action,
                Note = note,
                ChangedAt = DateTime.UtcNow,
                DigitalSignature = GenerateDigitalSignature(draftForSignature)
            };
            audit.IntegrityHash = GenerateIntegrityHash(audit);

            await _context.WorkOrderAudits.AddAsync(audit).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        #endregion

        #region === FUTURE HOOKS ===

        /// <summary>Hook za buduću AI detekciju anomalija.</summary>
        public Task<bool> PredictAnomalyAsync(WorkOrderAudit audit) =>
            Task.FromResult(false);

        #endregion
    }
}

