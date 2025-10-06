using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.Enums;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>IncidentService</b> – GMP compliant incident workflow + audit integration.
    /// </summary>
    public class IncidentService
    {
        private readonly DatabaseService _db;
        private readonly IIncidentAuditService _audit;

        public IncidentService(DatabaseService databaseService, IIncidentAuditService auditService)
        {
            _db = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _audit = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        #region CRUD

        public Task<List<Incident>> GetAllAsync() => _db.GetAllIncidentsAsync();

        /// <summary>Returns incident by ID (throws if not found to satisfy non-nullable result and fix CS8619).</summary>
        public async Task<Incident> GetByIdAsync(int id)
        {
            var inc = await _db.GetIncidentByIdAsync(id);
            if (inc == null) throw new KeyNotFoundException($"Incident #{id} not found.");
            return inc;
        }

        /// <summary>Create new incident (REPORTED).</summary>
        public async Task CreateAsync(Incident incident, int userId)
        {
            ValidateIncident(incident);
            incident.Status = IncidentStatus.REPORTED.ToString();
            incident.RiskLevel = CalculateRiskLevel(incident);
            incident.ReportedAt = DateTime.UtcNow;
            incident.DigitalSignature = GenerateDigitalSignature(incident);

            await _db.InsertOrUpdateIncidentAsync(incident, update: false, actorUserId: userId);
            await LogAudit(incident.Id, userId, IncidentActionType.CREATE, $"Prijavljen incident: {incident.Title}");
        }

        public async Task UpdateAsync(Incident incident, int userId)
        {
            ValidateIncident(incident);
            incident.DigitalSignature = GenerateDigitalSignature(incident);

            await _db.InsertOrUpdateIncidentAsync(incident, update: true, actorUserId: userId);
            await LogAudit(incident.Id, userId, IncidentActionType.UPDATE, $"Ažuriran incident ID={incident.Id}");
        }

        public async Task DeleteAsync(int incidentId, int userId)
        {
            await _db.DeleteIncidentAsync(incidentId, actorUserId: userId);
            await LogAudit(incidentId, userId, IncidentActionType.DELETE, $"Obrisan incident ID={incidentId}");
        }

        #endregion

        #region Workflow

        public async Task StartInvestigationAsync(int incidentId, int userId, string investigator)
        {
            var inc = await _db.GetIncidentByIdAsync(incidentId) ?? throw new InvalidOperationException("Incident nije pronađen.");
            inc.Status = IncidentStatus.INVESTIGATION.ToString();
            inc.AssignedInvestigator = investigator;
            inc.DigitalSignature = GenerateDigitalSignature(inc);

            await _db.InsertOrUpdateIncidentAsync(inc, update: true, actorUserId: userId);
            await LogAudit(inc.Id, userId, IncidentActionType.INVESTIGATION_START, $"Pokrenuta istraga (Istražitelj: {investigator})");
        }

        public async Task ClassifyAsync(int incidentId, int userId, string classification)
        {
            var inc = await _db.GetIncidentByIdAsync(incidentId) ?? throw new InvalidOperationException("Incident nije pronađen.");
            inc.Classification = classification;
            inc.Status = IncidentStatus.CLASSIFIED.ToString();
            inc.DigitalSignature = GenerateDigitalSignature(inc);

            await _db.InsertOrUpdateIncidentAsync(inc, update: true, actorUserId: userId);
            await LogAudit(inc.Id, userId, IncidentActionType.CLASSIFY, $"Klasificirano kao {classification}");
        }

        public async Task LinkDeviationAsync(int incidentId, int deviationId, int userId)
        {
            var inc = await _db.GetIncidentByIdAsync(incidentId) ?? throw new InvalidOperationException("Incident nije pronađen.");
            inc.LinkedDeviationId = deviationId;
            inc.Status = IncidentStatus.DEVIATION_LINKED.ToString();
            inc.DigitalSignature = GenerateDigitalSignature(inc);

            await _db.InsertOrUpdateIncidentAsync(inc, update: true, actorUserId: userId);
            await LogAudit(inc.Id, userId, IncidentActionType.DEVIATION_LINKED, $"Povezan s Deviation ID={deviationId}");
        }

        public async Task LinkCapaAsync(int incidentId, int capaId, int userId)
        {
            var inc = await _db.GetIncidentByIdAsync(incidentId) ?? throw new InvalidOperationException("Incident nije pronađen.");
            inc.LinkedCapaId = capaId;
            inc.Status = IncidentStatus.CAPA_LINKED.ToString();
            inc.DigitalSignature = GenerateDigitalSignature(inc);

            await _db.InsertOrUpdateIncidentAsync(inc, update: true, actorUserId: userId);
            await LogAudit(inc.Id, userId, IncidentActionType.CAPA_LINKED, $"Povezan s CAPA ID={capaId}");
        }

        public async Task CloseIncidentAsync(int incidentId, int userId, string closureComment)
        {
            var inc = await _db.GetIncidentByIdAsync(incidentId) ?? throw new InvalidOperationException("Incident nije pronađen.");
            inc.Status = IncidentStatus.CLOSED.ToString();
            inc.ClosureComment = closureComment;
            inc.ClosedAt = DateTime.UtcNow;
            inc.DigitalSignature = GenerateDigitalSignature(inc);

            await _db.InsertOrUpdateIncidentAsync(inc, update: true, actorUserId: userId);
            await LogAudit(inc.Id, userId, IncidentActionType.CLOSE, $"Zatvoren. Komentar: {closureComment}");
        }

        #endregion

        #region Risk

        private int CalculateRiskLevel(Incident inc)
        {
            int baseScore = inc.Classification?.ToUpperInvariant() switch
            {
                "CRITICAL" => 100,
                "MAJOR"    => 70,
                "MINOR"    => 40,
                _          => 20
            };
            return inc.IsCritical ? baseScore + 20 : baseScore;
        }

        #endregion

        #region Validation & Signatures

        private static void ValidateIncident(Incident inc)
        {
            if (string.IsNullOrWhiteSpace(inc.Title))
                throw new InvalidOperationException("Naslov incidenta je obavezan.");
            if (string.IsNullOrWhiteSpace(inc.Description))
                throw new InvalidOperationException("Opis incidenta je obavezan.");
        }

        private static string GenerateDigitalSignature(Incident inc)
        {
            string raw = $"{inc.Id}|{inc.Title}|{inc.Status}|{DateTime.UtcNow:O}";
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        private static string GenerateDigitalSignature(string payload)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes($"{payload}|{Guid.NewGuid()}")));
        }

        #endregion

        #region Audit

        private async Task LogAudit(int incidentId, int userId, IncidentActionType action, string details)
        {
            await _audit.CreateAsync(new IncidentAudit
            {
                IncidentId = incidentId,
                UserId = userId,
                Action = action,
                Note = details,                 // use Note
                ActionAt = DateTime.UtcNow,     // use ActionAt
                DigitalSignature = GenerateDigitalSignature(details)
            });
        }

        #endregion
    }
}

