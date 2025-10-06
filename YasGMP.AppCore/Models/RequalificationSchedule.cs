using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>RequalificationSchedule</b> — Ultra robust requalification/revalidation plan for a GMP component.
    /// Covers all regulatory, forensics, AI/analytics, digital traceability, audit, e-signature, and extensibility requirements.
    /// </summary>
    public class RequalificationSchedule
    {
        /// <summary>
        /// Jedinstveni ID plana revalidacije (PK).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// FK – Komponenta na koju se odnosi revalidacija.
        /// </summary>
        [Required]
        public int ComponentId { get; set; }
        public Component? Component { get; set; }

        /// <summary>
        /// Datum zadnje provedene kvalifikacije (for traceability).
        /// </summary>
        [Required]
        public DateTime LastQualified { get; set; }

        /// <summary>
        /// Datum kada je sljedeća revalidacija planirana (compliance deadline).
        /// </summary>
        [Required]
        public DateTime NextDue { get; set; }

        /// <summary>
        /// Metoda/protokol (naziv SOP-a, validacijski dokument, standard, procedure ref).
        /// </summary>
        [MaxLength(200)]
        public string? Method { get; set; }

        /// <summary>
        /// FK – ID odgovorne osobe (User).
        /// </summary>
        public int? ResponsibleUserId { get; set; }
        public User? ResponsibleUser { get; set; }

        /// <summary>
        /// (Opcionalno) Putanja do validacijskog protokola/dokaza (file, url, scan).
        /// </summary>
        [MaxLength(255)]
        public string? ProtocolFile { get; set; }

        /// <summary>
        /// (Opcionalno) Status: planirano, provedeno, odgođeno, odbijeno, zatvoreno, na čekanju, zahtijeva reviziju, error, ...
        /// </summary>
        [MaxLength(30)]
        public string? Status { get; set; }

        /// <summary>
        /// Komentar, napomena, rezultati, audit/inspekcija findings, deviation reference.
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Digitalni potpis odobravanja (user, hash, e-signature, device info).
        /// </summary>
        [MaxLength(128)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Vrijeme zadnje izmjene (forensic/audit chain).
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ID korisnika koji je zadnji mijenjao podatke (audit).
        /// </summary>
        public int LastModifiedById { get; set; }
        public User? LastModifiedBy { get; set; }

        /// <summary>
        /// IP adresa/uređaj s kojeg je mijenjano (forensic chain-of-custody).
        /// </summary>
        [MaxLength(64)]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Session ID or device session token (for forensic audit).
        /// </summary>
        [MaxLength(100)]
        public string? SessionId { get; set; }

        /// <summary>
        /// Geolocation (if available, for inspector).
        /// </summary>
        [MaxLength(128)]
        public string? GeoLocation { get; set; }

        /// <summary>
        /// Attached evidence files (docs, images, certificates, reports, findings, etc.).
        /// </summary>
        public string? AttachmentsJson { get; set; }

        /// <summary>
        /// Regulatorno tijelo (HALMED, EMA, FDA, ISO, ...), ako je vezano za compliance.
        /// </summary>
        [MaxLength(40)]
        public string? Regulator { get; set; }

        /// <summary>
        /// AI/ML analytics (anomaly, risk score, compliance risk, recommendation).
        /// </summary>
        public double? AnomalyScore { get; set; }

        [MaxLength(2048)]
        public string? AnalyticsJson { get; set; }

        /// <summary>
        /// Povezana CAPA/deviation/incident (full GMP/CSV traceability).
        /// </summary>
        public int? RelatedCaseId { get; set; }

        [MaxLength(40)]
        public string? RelatedCaseType { get; set; }

        /// <summary>
        /// Human-friendly ToString for inspector/log/debug.
        /// </summary>
        public override string ToString()
        {
            return $"Requalification: Component#{ComponentId} NextDue: {NextDue:d} Status: {Status}";
        }
    }
}

