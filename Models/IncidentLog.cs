using System;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>IncidentLog</b> – Forenzička evidencija svih incidenata, nepravilnosti i GMP prijava.
    /// Prati sve što inspektor može tražiti: detekciju, izvješćivanje, CAPA, status, korisnik, audit, rollback, signature!
    /// <para>
    /// ✅ Complete regulatory, forensic, and audit logging<br/>
    /// ✅ Every field needed for inspection, rollback, legal defense<br/>
    /// ✅ Digital signatures, user linkage, CAPA actions, and device/IP forensics
    /// </para>
    /// </summary>
    public class IncidentLog
    {
        /// <summary>Jedinstveni identifikator incidenta (Primary Key).</summary>
        public int Id { get; set; }

        /// <summary>Datum/vrijeme detekcije incidenta.</summary>
        public DateTime DetectedAt { get; set; }

        /// <summary>ID korisnika koji je prijavio incident (FK).</summary>
        public int? ReportedById { get; set; }
        public User? ReportedBy { get; set; }

        /// <summary>Kategorizacija/snaznost incidenta ("low", "medium", "high", "critical", "gmp", "compliance").</summary>
        public string? Severity { get; set; }

        /// <summary>Kratak naslov incidenta (za dashboard).</summary>
        public string? Title { get; set; }

        /// <summary>Detaljni opis incidenta ili GMP nepravilnosti.</summary>
        public string? Description { get; set; }

        /// <summary>Da li je incident riješen.</summary>
        public bool Resolved { get; set; } = false;

        /// <summary>Datum/vrijeme rješavanja (može biti null).</summary>
        public DateTime? ResolvedAt { get; set; }

        /// <summary>ID korisnika koji je riješio incident (FK).</summary>
        public int? ResolvedById { get; set; }
        public User? ResolvedBy { get; set; }

        /// <summary>Akcije koje su poduzete radi rješavanja (CAPA, ispravci, rollback…).</summary>
        public string? ActionsTaken { get; set; }

        /// <summary>Follow-up plan, dodatne radnje, CAPA mjere.</summary>
        public string? FollowUp { get; set; }

        /// <summary>Slobodan komentar (forenzika, rollback, notifikacija…).</summary>
        public string? Note { get; set; }

        /// <summary>IP adresa/uređaj (forenzička evidencija).</summary>
        public string? SourceIp { get; set; }

        /// <summary>Digitalni potpis prijave/rješenja incidenta.</summary>
        public string? DigitalSignature { get; set; }

        // ==================== ✅ BONUS FIELDS/EXTENSIONS ====================

        /// <summary>Root cause/failure classification (for deep investigation and traceability).</summary>
        public string? RootCause { get; set; }

        /// <summary>Linked CAPA case or reference (traceability to corrective/preventive action).</summary>
        public int? CapaCaseId { get; set; }
        public CapaCase? CapaCase { get; set; }

        /// <summary>Attachments or evidence (JSON/CSV/Blob/IDs for photos, PDFs, reports).</summary>
        public string? Attachments { get; set; }

        /// <summary>Last modification timestamp (for forensic timeline).</summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>User ID who last modified (for audit trail).</summary>
        public int? LastModifiedById { get; set; }
        public User? LastModifiedBy { get; set; }
    }
}
