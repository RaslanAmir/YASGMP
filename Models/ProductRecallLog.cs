using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>ProductRecallLog</b> – Ultra robust, full-featured GMP/CSV/ISO audit log for all product/part recalls.
    /// Tracks every regulatory, root-cause, traceability, and closure step. Digital signature, attachments, chain-of-custody, inspector audit, AI/ML ready.
    /// </summary>
    public class ProductRecallLog
    {
        /// <summary>Jedinstveni ID opoziva (Primary Key).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Tip povezanog entiteta (part, component, machine, batch, lot, order, other).</summary>
        [MaxLength(40)]
        public string? RelatedType { get; set; }

        /// <summary>ID povezanog entiteta (Part/Component/Machine/Batch/Lot... FK).</summary>
        public int RelatedId { get; set; }

        /// <summary>Datum iniciranja opoziva (UTC).</summary>
        public DateTime RecallDate { get; set; }

        /// <summary>Razlog opoziva (kvar, nonconformity, quality, regulatory…).</summary>
        [MaxLength(1000)]
        public string? Reason { get; set; }

        /// <summary>ID korisnika koji je pokrenuo opoziv (FK).</summary>
        public int RecalledById { get; set; }
        public User? RecalledBy { get; set; }

        /// <summary>Opis poduzetih akcija (trace, steps, digital, chain-of-custody, crosslink IDs, external communication).</summary>
        [MaxLength(1500)]
        public string? RecallAction { get; set; }

        /// <summary>Rezultat opoziva (successful, partial, failed, inspector reviewed…).</summary>
        [MaxLength(200)]
        public string? RecallResult { get; set; }

        /// <summary>Datum zatvaranja opoziva (if closed/resolved).</summary>
        public DateTime? ClosedAt { get; set; }

        /// <summary>ID korisnika koji je zatvorio opoziv (FK, inspector, QA, admin).</summary>
        public int? ClosedById { get; set; }
        public User? ClosedBy { get; set; }

        /// <summary>Status opoziva (open, under review, closed, escalated, regulator notified, external, inspector audit).</summary>
        [MaxLength(30)]
        public string? Status { get; set; }

        /// <summary>Paths to relevant recall documentation (PDF, report, photo, certificate, inspector notes).</summary>
        public List<string> Attachments { get; set; } = new();

        /// <summary>Digital signature/hash for full chain-of-custody and Part 11 compliance.</summary>
        [MaxLength(128)]
        public string? DigitalSignature { get; set; }

        /// <summary>Forensic: IP address/device info (initiation/closure) for full audit/traceability.</summary>
        [MaxLength(64)]
        public string? SourceIp { get; set; }

        /// <summary>Session ID, browser/device fingerprint for full trace chain.</summary>
        [MaxLength(100)]
        public string? SessionId { get; set; }

        /// <summary>AI/ML anomaly or risk score (future use, smart recall monitoring, compliance analytics).</summary>
        public double? AnomalyScore { get; set; }

        /// <summary>Regulator (if recall triggered by, or notified to, a regulatory body: HALMED, EMA, FDA…)</summary>
        [MaxLength(40)]
        public string? Regulator { get; set; }

        /// <summary>ID povezanog incidenta/CAPA/inspection ako postoji (traceability chain).</summary>
        public int? LinkedIncidentId { get; set; }
        public Incident? LinkedIncident { get; set; }

        public int? LinkedCapaId { get; set; }
        public CapaCase? LinkedCapa { get; set; }

        public int? LinkedInspectionId { get; set; }
        public Inspection? LinkedInspection { get; set; }

        /// <summary>Vrijeme zadnje izmjene (audit).</summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>ID korisnika koji je zadnji izmijenio opoziv (audit FK).</summary>
        public int? LastModifiedById { get; set; }
        public User? LastModifiedBy { get; set; }

        /// <summary>Free text note (inspector, auditor, system event, compliance note, explanation).</summary>
        [MaxLength(1000)]
        public string? Note { get; set; }

        /// <summary>
        /// Returns a human-readable summary of the recall for log/debugging.
        /// </summary>
        public override string ToString()
        {
            return $"[Recall #{Id}] {RelatedType ?? "entity"}#{RelatedId} – {Status} (Started: {RecallDate:u}, Closed: {ClosedAt?.ToString("u") ?? "open"})";
        }
    }
}
