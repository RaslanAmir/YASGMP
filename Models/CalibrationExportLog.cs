using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>CalibrationExportLog</b> – Records every calibration export event for full audit, GMP, and regulatory traceability.
    /// <para>
    /// ✅ Tracks: who, when, export format, filter criteria, file, and IP.
    /// ✅ Essential for 21 CFR Part 11, EU GMP, HALMED audits, and inspection readiness.
    /// </para>
    /// </summary>
    [Table("calibration_export_log")]
    public class CalibrationExportLog
    {
        /// <summary>
        /// Unique export log ID (Primary Key).
        /// </summary>
        [Key]
        [Column("id")]
        [Display(Name = "ID izvoza")]
        public int Id { get; set; }

        /// <summary>
        /// User who performed the export (FK, optional).
        /// </summary>
        [Column("user_id")]
        [Display(Name = "Korisnik")]
        public int? UserId { get; set; }

        /// <summary>
        /// Navigation to the user who performed the export (optional).
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        /// <summary>
        /// Date/time when the export occurred (UTC).
        /// </summary>
        [Required]
        [Column("export_time")]
        [Display(Name = "Vrijeme izvoza")]
        public DateTime ExportTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Export format (e.g., "excel", "pdf").
        /// </summary>
        [Required]
        [Column("export_format")]
        [StringLength(16)]
        [Display(Name = "Format")]
        public string ExportFormat { get; set; } = string.Empty;

        /// <summary>
        /// Filter: Component ID (nullable, for filtered export).
        /// </summary>
        [Column("filter_component_id")]
        [Display(Name = "Komponenta")]
        public int? FilterComponentId { get; set; }

        /// <summary>
        /// Date filter: From.
        /// </summary>
        [Column("filter_date_from")]
        [Display(Name = "Datum od")]
        public DateTime? FilterDateFrom { get; set; }

        /// <summary>
        /// Date filter: To.
        /// </summary>
        [Column("filter_date_to")]
        [Display(Name = "Datum do")]
        public DateTime? FilterDateTo { get; set; }

        /// <summary>
        /// File path or name of the exported file.
        /// </summary>
        [Column("file_path")]
        [StringLength(255)]
        [Display(Name = "Datoteka")]
        public string FilePath { get; set; } = string.Empty;
    }
}
