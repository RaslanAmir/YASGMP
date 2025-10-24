using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>SopDocument</b> – Ultra-robust model for Standard Operating Procedure (SOP) documentation.
    /// Covers every detail for GMP/CSV/21 CFR Part 11: PDF, metadata, versioning, lifecycle, signatures, blockchain, e-sign, workflow, attachments, and AI/ML readiness.
    /// </summary>
    public class SopDocument
    {
        /// <summary>Unique SOP document ID (PK).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Document code or identifier (for global tracking/traceability, QR/barcode/UUID).</summary>
        [MaxLength(80)]
        public string Code { get; set; } = string.Empty;

        /// <summary>Name/title of the SOP.</summary>
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Short description or summary (purpose, process, scope).</summary>
        [MaxLength(400)]
        public string Description { get; set; } = string.Empty;

        /// <summary>Main process or module (CAPA, cleaning, validation, equipment, etc).</summary>
        [MaxLength(80)]
        public string Process { get; set; } = string.Empty;

        /// <summary>Language (multi-lingual support for global compliance).</summary>
        [MaxLength(10)]
        public string Language { get; set; } = "hr";

        /// <summary>Original issue date (validation/protocol date).</summary>
        [Required]
        public DateTime DateIssued { get; set; }

        /// <summary>Expiration date (null if perpetual, for review/archival alerts).</summary>
        public DateTime? DateExpiry { get; set; }

        /// <summary>Revision/review due date (future compliance triggers).</summary>
        public DateTime? NextReviewDate { get; set; }

        /// <summary>Path/URL to PDF (can be file, SharePoint, GDrive, Azure Blob, etc).</summary>
        [Required, MaxLength(512)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>List of attachment paths (source files, checklists, templates, etc).</summary>
        public List<string> Attachments { get; set; } = new();

        /// <summary>FK to responsible user/owner.</summary>
        public int ResponsibleUserId { get; set; }

        /// <summary>Navigacija na odgovornu osobu.</summary>
        public User ResponsibleUser { get; set; } = null!;

        /// <summary>FK to creator/author of the document (audit trace).</summary>
        public int? CreatedById { get; set; }

        /// <summary>Navigacija na kreatora.</summary>
        public User CreatedBy { get; set; } = null!;

        /// <summary>Document version number (for audit/version traceability).</summary>
        public int VersionNo { get; set; }

        /// <summary>PDF file checksum (SHA-256 or better, for content integrity).</summary>
        [MaxLength(128)]
        public string FileHash { get; set; } = string.Empty;

        /// <summary>Status (active, expired, draft, inactive, superseded, under review, pending approval, archived, etc).</summary>
        [MaxLength(30)]
        public string Status { get; set; } = "active";

        /// <summary>Digital signature (user, hash, timestamp, cert – for GMP/CSV/21 CFR Part 11 compliance).</summary>
        [MaxLength(128)]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Blockchain/hash chain for ultimate forensic traceability (future proof).</summary>
        [MaxLength(128)]
        public string ChainHash { get; set; } = string.Empty;

        /// <summary>List of user IDs who approved (multi-stage approval, e-sign).</summary>
        public List<int> ApproverIds { get; set; } = new();

        /// <summary>List of User objects (navigation property for multi-approver workflow).</summary>
        public List<User> Approvers { get; set; } = new();

        /// <summary>Approval/validation timestamps (multi-stage).</summary>
        public List<DateTime> ApprovalTimestamps { get; set; } = new();

        /// <summary>Reviewer or auditor notes/comments (for review cycle/audit).</summary>
        [MaxLength(1000)]
        public string ReviewNotes { get; set; } = string.Empty;

        /// <summary>PDF metadata (author, company, tags, extracted for AI/ML/search).</summary>
        [MaxLength(1024)]
        public string PdfMetadata { get; set; } = string.Empty;

        /// <summary>Linked entity type (machine, process, department, CAPA, validation, etc).</summary>
        [MaxLength(40)]
        public string RelatedType { get; set; } = string.Empty;

        /// <summary>Linked entity ID.</summary>
        public int? RelatedId { get; set; }

        /// <summary>Free comment or additional note.</summary>
        [MaxLength(400)]
        public string Comment { get; set; } = string.Empty;

        /// <summary>GMP/CSV/Part 11 audit: last modified time.</summary>
        public DateTime LastModified { get; set; }

        /// <summary>User ID of last modifier (for full audit chain).</summary>
        public int LastModifiedById { get; set; }

        /// <summary>Navigacija na zadnjeg urednika.</summary>
        public User LastModifiedBy { get; set; } = null!;

        /// <summary>IP/device info of last modification (forensics).</summary>
        [MaxLength(64)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>AI/ML tags, topics, or keywords for future analytics/search.</summary>
        [MaxLength(512)]
        public string AiTags { get; set; } = string.Empty;

        /// <summary>DeepCopy — for rollback/clone scenarios (audit/forensics).</summary>
        public SopDocument DeepCopy()
        {
            return new SopDocument
            {
                Id = this.Id,
                Code = this.Code,
                Name = this.Name,
                Description = this.Description,
                Process = this.Process,
                Language = this.Language,
                DateIssued = this.DateIssued,
                DateExpiry = this.DateExpiry,
                NextReviewDate = this.NextReviewDate,
                FilePath = this.FilePath,
                Attachments = new List<string>(this.Attachments),
                ResponsibleUserId = this.ResponsibleUserId,
                ResponsibleUser = this.ResponsibleUser,
                CreatedById = this.CreatedById,
                CreatedBy = this.CreatedBy,
                VersionNo = this.VersionNo,
                FileHash = this.FileHash,
                Status = this.Status,
                DigitalSignature = this.DigitalSignature,
                ChainHash = this.ChainHash,
                ApproverIds = new List<int>(this.ApproverIds),
                Approvers = new List<User>(this.Approvers),
                ApprovalTimestamps = new List<DateTime>(this.ApprovalTimestamps),
                ReviewNotes = this.ReviewNotes,
                PdfMetadata = this.PdfMetadata,
                RelatedType = this.RelatedType,
                RelatedId = this.RelatedId,
                Comment = this.Comment,
                LastModified = this.LastModified,
                LastModifiedById = this.LastModifiedById,
                LastModifiedBy = this.LastModifiedBy,
                SourceIp = this.SourceIp,
                AiTags = this.AiTags
            };
        }

        /// <summary>Human-readable ToString for dashboards/logging.</summary>
        public override string ToString()
        {
            return $"SOP#{Id}: {Name} (v{VersionNo}, Status: {Status}) — File: {FilePath}";
        }
    }
}

