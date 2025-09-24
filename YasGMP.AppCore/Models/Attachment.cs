using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Attachment</b> â€“ Ultra-robust digital record for any file (PDF, image, scan, doc, certificate, etc.)
    /// linked to GMP/CMMS entities (WorkOrder, Machine, Component, CAPA, Calibration, Inspection, etc).
    /// <para>
    /// âś… Fully auditable (upload, approval, expiry, rollback, versioning, chain-of-custody, e-sign).<br/>
    /// âś… Supports multi-entity links, digital signature, hash, OCR, session, device, IP, ML/AI.<br/>
    /// âś… Inspector, regulatory, and future-proof: ALL FIELDS for forensic, analytics, rollback, audit, and more.
    /// </para>
    /// </summary>
    [Table("attachments")]
    public partial class Attachment
    {
        /// <summary>
        /// Unique identifier of the attachment (Primary Key).
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// User-friendly display name or description of the file (not the file name).
        /// </summary>
        [Required, StringLength(255)]
        [Column("name")]
        [Display(Name = "Naziv/Opis")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Actual file name on disk, cloud, or original upload (used for download/display).
        /// </summary>
        [Required, StringLength(255)]
        [Column("file_name")]
        [Display(Name = "Naziv datoteke")]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// File storage path, URL, or blob identifier (location on disk/cloud/DB).
        /// </summary>
        [StringLength(512)]
        [Column("file_path")]
        [Display(Name = "Putanja/URL")]
        public string? FilePath { get; set; }

        /// <summary>
        /// File extension/type (pdf, jpg, docx, etc.).
        /// </summary>
        [StringLength(20)]
        [Column("file_type")]
        [Display(Name = "Tip/Dodatak")]
        public string? FileType { get; set; }

        /// <summary>
        /// File size in bytes (optional, for quotas/security checks).
        /// </summary>
        [Column("file_size")]
        [Display(Name = "VeliÄŤina (B)")]
        public long? FileSize { get; set; }

        /// <summary>
        /// Entity type this attachment is linked to (e.g., "WorkOrder", "Machine").
        /// </summary>
        [StringLength(50)]
        [Column("entity_type")]
        [Display(Name = "Tip entiteta")]
        public string? EntityType { get; set; }

        /// <summary>
        /// ID of the linked entity (e.g., WorkOrderId, MachineId, etc).
        /// </summary>
        [Column("entity_id")]
        [Display(Name = "ID entiteta")]
        public int? EntityId { get; set; }

        /// <summary>
        /// Optional field for full file content if stored in DB (BLOB).
        /// </summary>
        [Column("file_content", TypeName = "LONGBLOB")]
        [Display(Name = "SadrĹľaj datoteke (BLOB)")]
        public byte[]? FileContent { get; set; }

        /// <summary>
        /// OCR extracted text (from scans, PDFs, images) for compliance, search, and AI.
        /// </summary>
        [Column("ocr_text")]
        [Display(Name = "OCR tekst")]
        public string? OCRText { get; set; }

        /// <summary>
        /// SHA256/SHA512 or other cryptographic hash of file (compliance, e-sign).
        /// </summary>
        [StringLength(64)]
        [Column("sha256")]
        [Display(Name = "SHA-256 Hash")]
        public string? Sha256 { get; set; }

        /// <summary>
        /// Timestamp when the file was uploaded (audit/forensics).
        /// </summary>
        [Column("uploaded_at")]
        [Display(Name = "Datum uploada")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ID of the user who uploaded the file (FK).
        /// </summary>
        [Column("uploaded_by_id")]
        public int? UploadedById { get; set; }

        /// <summary>
        /// Navigation property for the user who uploaded the file.
        /// </summary>
        [ForeignKey(nameof(UploadedById))]
        public virtual User? UploadedBy { get; set; }

        /// <summary>
        /// Optional tenant that owns the attachment when multi-tenancy is enabled.
        /// </summary>
        [Column("tenant_id")]
        public int? TenantId { get; set; }

        /// <summary>
        /// Navigation property to the owning tenant.
        /// </summary>
        [ForeignKey(nameof(TenantId))]
        public virtual Tenant? Tenant { get; set; }

        /// <summary>
        /// Approval status (GMP-controlled documents).
        /// </summary>
        [Column("is_approved")]
        [Display(Name = "Odobreno")]
        public bool IsApproved { get; set; }

        /// <summary>
        /// ID of the user who approved the document (optional, FK).
        /// </summary>
        [Column("approved_by_id")]
        public int? ApprovedById { get; set; }

        /// <summary>
        /// Navigation property for the user who approved the file.
        /// </summary>
        [ForeignKey(nameof(ApprovedById))]
        public virtual User? ApprovedBy { get; set; }

        /// <summary>
        /// Date/time of approval (if applicable).
        /// </summary>
        [Column("approved_at")]
        [Display(Name = "Datum odobrenja")]
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// Expiry date (for certificates or time-limited documents).
        /// </summary>
        [Column("expiry_date")]
        [Display(Name = "Rok trajanja")]
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Cryptographic digital signature of the file (for GMP compliance).
        /// </summary>
        [StringLength(256)]
        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// IP address of uploader (forensics, audit, regulatory logs).
        /// </summary>
        [StringLength(64)]
        [Column("ip_address")]
        [Display(Name = "IP adresa")]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Device info of uploader (browser, OS, workstation, etc; forensics).
        /// </summary>
        [StringLength(256)]
        [Column("device_info")]
        [Display(Name = "UreÄ‘aj / Device info")]
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// Session ID in which file was uploaded (chain-of-custody, security, forensics).
        /// </summary>
        [StringLength(128)]
        [Column("session_id")]
        [Display(Name = "Session ID")]
        public string? SessionId { get; set; }

        /// <summary>
        /// Current status of the file (uploaded, approved, expired, deleted, replaced, signed, etc).
        /// </summary>
        [StringLength(64)]
        [Column("status")]
        [Display(Name = "Status")]
        public string? Status { get; set; }

        /// <summary>
        /// Indicates whether the attachment payload is encrypted at rest.
        /// </summary>
        [Column("encrypted")]
        [Display(Name = "Encrypted")]
        public bool Encrypted { get; set; }

        /// <summary>
        /// Optional serialized encryption metadata (algorithm, key id, chunks, etc.).
        /// </summary>
        [Column("encryption_metadata")]
        public string? EncryptionMetadata { get; set; }

        /// <summary>
        /// Optional comments or notes about the attachment (legacy compatibility).
        /// </summary>
        [StringLength(255)]
        [Column("note")]
        [Display(Name = "Napomena")]
        public string? Note { get; set; }

        /// <summary>
        /// Optional rich notes/comments field for advanced audit modules (your code expects "Notes").
        /// </summary>
        [StringLength(2048)]
        [Column("notes")]
        [Display(Name = "Napomene")]
        public string? Notes { get; set; }

        /// <summary>
        /// Version of the document (for rollback and full audit trail).
        /// </summary>
        [Column("change_version")]
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Is this record soft-deleted (GDPR/archive, not physically removed).
        /// </summary>
        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Timestamp when the record was soft deleted by retention policies.
        /// </summary>
        [Column("soft_deleted_at")]
        public DateTime? SoftDeletedAt { get; set; }

        // ==================== BONUS/ADVANCED EXTENSIBILITY FIELDS ====================

        /// <summary>
        /// For AI/ML: anomaly detection score, OCR/scan fraud flag, etc.
        /// </summary>
        [Column("ai_score")]
        public double? AiScore { get; set; }

        /// <summary>
        /// For linking audit/capa/cert chains.
        /// </summary>
        [Column("chain_id")]
        public string? ChainId { get; set; }

        /// <summary>
        /// File version UID (for blockchain, e-sign, advanced rollback).
        /// </summary>
        [StringLength(128)]
        [Column("version_uid")]
        public string? VersionUid { get; set; }

        /// <summary>
        /// Creates a deep copy of the attachment for rollback/inspection scenarios.
        /// </summary>
        /// <returns>A deep-copied <see cref="Attachment"/> object.</returns>
        public Attachment DeepCopy()
        {
            return new Attachment
            {
                Id = this.Id,
                Name = this.Name,
                FileName = this.FileName,
                FilePath = this.FilePath,
                FileType = this.FileType,
                FileSize = this.FileSize,
                EntityType = this.EntityType,
                EntityId = this.EntityId,
                FileContent = this.FileContent != null ? (byte[])this.FileContent.Clone() : null,
                OCRText = this.OCRText,
                Sha256 = this.Sha256,
                UploadedAt = this.UploadedAt,
                UploadedById = this.UploadedById,
                UploadedBy = this.UploadedBy,
                TenantId = this.TenantId,
                Tenant = this.Tenant,
                IsApproved = this.IsApproved,
                ApprovedById = this.ApprovedById,
                ApprovedBy = this.ApprovedBy,
                ApprovedAt = this.ApprovedAt,
                ExpiryDate = this.ExpiryDate,
                DigitalSignature = this.DigitalSignature,
                IpAddress = this.IpAddress,
                DeviceInfo = this.DeviceInfo,
                SessionId = this.SessionId,
                Status = this.Status,
                Encrypted = this.Encrypted,
                EncryptionMetadata = this.EncryptionMetadata,
                Note = this.Note,
                Notes = this.Notes,
                ChangeVersion = this.ChangeVersion,
                IsDeleted = this.IsDeleted,
                SoftDeletedAt = this.SoftDeletedAt,
                AiScore = this.AiScore,
                ChainId = this.ChainId,
                VersionUid = this.VersionUid
            };
        }

        /// <summary>
        /// Checks if the attachment is expired based on <see cref="ExpiryDate"/>.
        /// </summary>
        [NotMapped]
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;

        /// <summary>
        /// Returns a human-readable string for debugging or logging.
        /// </summary>
        /// <returns>A string representing the attachment details.</returns>
        public virtual ICollection<AttachmentLink> Links { get; set; } = new List<AttachmentLink>();

        public virtual ICollection<RetentionPolicy> RetentionPolicies { get; set; } = new List<RetentionPolicy>();

        public override string ToString()
        {
            return $"{(string.IsNullOrWhiteSpace(FileName) ? Name : FileName)} ({FileType}) - Linked to: {EntityType}#{EntityId} [{Status}]";
        }
    }
}
