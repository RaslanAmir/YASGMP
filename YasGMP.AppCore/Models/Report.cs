using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Report</b> — Ultra robust, all-purpose audit/analytics/compliance report model.
    /// GMP/CSV/21 CFR Part 11/ISO/AI ready: includes forensic chain, versioning, filter params, signature, attachments, ML, regulatory links.
    /// Base for specialized report types (inherit or extend as needed).
    /// </summary>
    public class Report
    {
        /// <summary>
        /// Jedinstveni ID izvještaja (PK).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Naziv izvještaja (npr. "Mjesečni pregled PPM", "Kvarovi Q2/2025").
        /// </summary>
        [Required]
        [MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Opis/svrha izvještaja (može sadržavati filtere, parametre, KPI, ciljeve...).
        /// </summary>
        [MaxLength(300)]
        public string? Description { get; set; }

        /// <summary>
        /// Datum/vrijeme generiranja izvještaja (UTC, za audit chain).
        /// </summary>
        public DateTime GeneratedOn { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// FK – korisnik koji je generirao izvještaj (za forensic audit).
        /// </summary>
        public int GeneratedById { get; set; }
        public User? GeneratedBy { get; set; }

        /// <summary>
        /// Putanja do izvještaja (PDF/Excel/CSV/HTML/JSON). Puna forenzička sljedivost.
        /// </summary>
        [MaxLength(255)]
        public string? FilePath { get; set; }

        /// <summary>
        /// Digitalni potpis (hash, ime, e-signature, device info, Part 11 compliance).
        /// </summary>
        [MaxLength(128)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Tip izvještaja ("pdf", "excel", "dashboard", "csv", "html", "json", "xml", ...).
        /// </summary>
        [MaxLength(30)]
        public string? ReportType { get; set; }

        /// <summary>
        /// Verzija izvještaja (za audit/export sljedivost).
        /// </summary>
        public int VersionNo { get; set; }

        /// <summary>
        /// Snapshot parametara filtera (JSON string: svi input parametri za audit chain).
        /// </summary>
        public string? Parameters { get; set; }

        /// <summary>
        /// Prilozi izvještaja (dokumentacija, slike, dokazi, zip, csv, pdf, …).
        /// </summary>
        public List<string> Attachments { get; set; } = new();

        /// <summary>
        /// Status izvještaja (kreiran, exportiran, validiran, arhiviran, poništen, error, ...).
        /// </summary>
        [MaxLength(30)]
        public string? Status { get; set; }

        /// <summary>
        /// Sljedivost: povezana inspekcija, CAPA, incident, radni nalog...
        /// </summary>
        public int? LinkedEntityId { get; set; }

        [MaxLength(40)]
        public string? LinkedEntityType { get; set; }

        /// <summary>
        /// Regulatorno tijelo (HALMED, EMA, FDA, ISO, …), ako je vezano za compliance.
        /// </summary>
        [MaxLength(40)]
        public string? Regulator { get; set; }

        /// <summary>
        /// AI/ML analitika (risk score, outlier/anomaly, comment).
        /// </summary>
        public double? AnomalyScore { get; set; }

        [MaxLength(2048)]
        public string? AnalyticsJson { get; set; }

        /// <summary>
        /// Vrijeme zadnje izmjene (audit, forensic).
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ID korisnika koji je zadnji mijenjao izvještaj (forensic/audit).
        /// </summary>
        public int LastModifiedById { get; set; }
        public User? LastModifiedBy { get; set; }

        /// <summary>
        /// IP adresa/uređaj koji je generirao ili izmijenio izvještaj (forensic chain).
        /// </summary>
        [MaxLength(64)]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Session ID, user agent, device fingerprint, geo (bonus: forensics).
        /// </summary>
        [MaxLength(100)]
        public string? SessionId { get; set; }

        [MaxLength(128)]
        public string? DeviceInfo { get; set; }

        [MaxLength(128)]
        public string? GeoLocation { get; set; }

        /// <summary>
        /// Free text note (auditor comment, inspector remark, error info, …).
        /// </summary>
        [MaxLength(1000)]
        public string? Note { get; set; }

        /// <summary>
        /// Human-friendly summary for log/debug.
        /// </summary>
        public override string ToString()
        {
            return $"Report: {Title} [Type: {ReportType}, Status: {Status}] (By: {GeneratedById}, On: {GeneratedOn:u})";
        }
    }
}
