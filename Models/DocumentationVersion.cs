using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>DocumentationVersion</b> – Tracks all versions of documents (SOP, manuals, protocols, reports, etc.) for full GMP/CSV/21 CFR Part 11 compliance.
    /// <para>
    /// ⭐ SUPER-MEGA-ULTRA ROBUST: Versioning, audit, digital signature, file hash, workflow/approval, user navigation, rollback, forensics, soft delete, and unlimited extensibility for inspections and legal defensibility.
    /// </para>
    /// </summary>
    public class DocumentationVersion
    {
        /// <summary>
        /// Unique ID for the document version (Primary Key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Type of document: sop, manual, protocol, report, other.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        /// <summary>
        /// ID of the related entity (WorkOrder, Machine, Component, etc).
        /// </summary>
        [Required]
        public int RelatedId { get; set; }

        /// <summary>
        /// Version number.
        /// </summary>
        [Required]
        public int VersionNo { get; set; }

        /// <summary>
        /// File name.
        /// </summary>
        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Path to the file.
        /// </summary>
        [Required]
        [StringLength(512)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// User ID of the uploader.
        /// </summary>
        [Required]
        public int UploadedById { get; set; }

        /// <summary>
        /// Navigation to uploader user.
        /// </summary>
        [ForeignKey(nameof(UploadedById))]
        public virtual User UploadedBy { get; set; } = null!;

        /// <summary>
        /// Upload date and time.
        /// </summary>
        [Required]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional comment or note.
        /// </summary>
        [StringLength(2000)]
        public string? Note { get; set; }

        /// <summary>
        /// File hash/digital signature (21 CFR Part 11, GMP trust).
        /// </summary>
        [StringLength(256)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Forensic: IP address/device from which document was uploaded.
        /// </summary>
        [StringLength(128)]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Document approval status (draft, under review, approved, obsolete).
        /// </summary>
        [StringLength(32)]
        public string? ApprovalStatus { get; set; }

        /// <summary>
        /// ID of user who approved this version.
        /// </summary>
        public int? ApprovedById { get; set; }

        /// <summary>
        /// Navigation to the approver.
        /// </summary>
        [ForeignKey(nameof(ApprovedById))]
        public virtual User? ApprovedBy { get; set; }

        /// <summary>
        /// Date/time when document version was approved.
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// Chain/version for rollback, traceability, and event sourcing.
        /// </summary>
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Is this version archived/soft deleted (GDPR, not physically deleted).
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }
}
