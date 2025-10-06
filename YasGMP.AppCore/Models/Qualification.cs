using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Qualification</b> – Ultra-robust GMP/CMMS qualification record (IQ, OQ, PQ, Supplier, Component, Requalification, etc.)
    /// <para>
    /// ✅ Tracks initial and periodic qualifications, status, expiry, digital signatures, audit logs, escalation.<br/>
    /// ✅ Full regulatory compliance (21 CFR Part 11, Annex 11, HALMED, ISO 9001/13485).<br/>
    /// ✅ Forensically links to equipment, users, suppliers, documents, and change events.
    /// </para>
    /// </summary>
    public class Qualification
    {
        /// <summary>Unique qualification ID (Primary Key).</summary>
        [Key]
        [Display(Name = "ID kvalifikacije")]
        public int Id { get; set; }

        /// <summary>Qualification code (e.g., "IQ-2024-03").</summary>
        [Required]
        [StringLength(40)]
        [Display(Name = "Oznaka kvalifikacije")]
        public string Code { get; set; } = string.Empty;

        /// <summary>Type of qualification (IQ, OQ, PQ, Supplier, Component, Requalification, etc.).</summary>
        [Required]
        [StringLength(24)]
        [Display(Name = "Tip kvalifikacije")]
        public string Type { get; set; } = string.Empty;

        /// <summary>Description/summary of qualification.</summary>
        [StringLength(255)]
        [Display(Name = "Opis")]
        public string? Description { get; set; }

        /// <summary>Date of qualification execution (UTC preferred).</summary>
        [Display(Name = "Datum kvalifikacije")]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        /// <summary>Expiry/next review date (for periodic requalification).</summary>
        [Display(Name = "Datum isteka/revizije")]
        public DateTime? ExpiryDate { get; set; }

        /// <summary>Status (active, expired, scheduled, under_review, withdrawn, in_progress, rejected).</summary>
        [StringLength(32)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "active";

        /// <summary>Equipment/machine qualified (FK, optional).</summary>
        [Display(Name = "Stroj/oprema")]
        public int? MachineId { get; set; }

        [ForeignKey(nameof(MachineId))]
        public Machine? Machine { get; set; }

        /// <summary>Component qualified (FK, optional).</summary>
        [Display(Name = "Komponenta")]
        public int? ComponentId { get; set; }

        [ForeignKey(nameof(ComponentId))]
        public MachineComponent? Component { get; set; }

        /// <summary>Supplier qualified (FK, optional).</summary>
        [Display(Name = "Dobavljač")]
        public int? SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public Supplier? Supplier { get; set; }

        /// <summary>User who performed qualification (FK).</summary>
        [Display(Name = "Izvršio korisnik")]
        public int? QualifiedById { get; set; }

        [ForeignKey(nameof(QualifiedById))]
        public User? QualifiedBy { get; set; }

        /// <summary>Approval user (FK).</summary>
        [Display(Name = "Odobrio korisnik")]
        public int? ApprovedById { get; set; }

        [ForeignKey(nameof(ApprovedById))]
        public User? ApprovedBy { get; set; }

        /// <summary>Date/time of approval (UTC).</summary>
        [Display(Name = "Datum odobravanja")]
        public DateTime? ApprovedAt { get; set; }

        /// <summary>Digital signature for approval/qualification.</summary>
        [StringLength(255)]
        [Display(Name = "Digitalni potpis")]
        public string? DigitalSignature { get; set; }

        /// <summary>Optional certificate/record number (alias for UI filtering/compatibility).</summary>
        [StringLength(80)]
        [Display(Name = "Broj certifikata / zapisnik")]
        public string? CertificateNumber { get; set; }

        /// <summary>List of linked documents, SOPs, certificates.</summary>
        [Display(Name = "Povezani dokumenti")]
        public List<DocumentControl> Documents { get; set; } = new List<DocumentControl>();

        /// <summary>Full audit log of qualification process.</summary>
        [Display(Name = "Evidencija aktivnosti kvalifikacije")]
        public List<QualificationAuditLog> AuditLogs { get; set; } = new List<QualificationAuditLog>();

        /// <summary>Additional notes, escalation, regulatory info.</summary>
        [StringLength(255)]
        [Display(Name = "Napomene")]
        public string? Note { get; set; }

        // ===== UI/VW COMPATIBILITY ALIASES (to remove CS1061 in ViewModels) =====

        /// <summary>
        /// Alias for <see cref="Type"/> used by some ViewModels.
        /// </summary>
        [NotMapped]
        public string QualificationType
        {
            get => Type;
            set => Type = value;
        }

        /// <summary>
        /// Alias that returns the equipment/machine display name; used by filters/search.
        /// Always returns a non-null string.
        /// </summary>
        [NotMapped]
        public string EquipmentName => Machine?.Name ?? Component?.Name ?? Supplier?.Name ?? string.Empty;

        /// <summary>Human-readable summary.</summary>
        public override string ToString()
            => $"[{Code}] {Type} → {(string.IsNullOrWhiteSpace(EquipmentName) ? "N/A" : EquipmentName)} (Status: {Status}, Date: {Date:u}, Expires: {ExpiryDate:u})";

        /// <summary>Deep copy (for safe editing/rollback).</summary>
        public Qualification DeepCopy()
        {
            return new Qualification
            {
                Id = Id,
                Code = Code,
                Type = Type,
                Description = Description,
                Date = Date,
                ExpiryDate = ExpiryDate,
                Status = Status,
                MachineId = MachineId,
                Machine = Machine,
                ComponentId = ComponentId,
                Component = Component,
                SupplierId = SupplierId,
                Supplier = Supplier,
                QualifiedById = QualifiedById,
                QualifiedBy = QualifiedBy,
                ApprovedById = ApprovedById,
                ApprovedBy = ApprovedBy,
                ApprovedAt = ApprovedAt,
                DigitalSignature = DigitalSignature,
                CertificateNumber = CertificateNumber,
                Documents = new List<DocumentControl>(Documents ?? new()),
                AuditLogs = new List<QualificationAuditLog>(AuditLogs ?? new()),
                Note = Note
            };
        }
    }

    /// <summary>
    /// <b>QualificationAuditLog</b> – Single audit entry for qualification event (creation, approval, expiry, etc.).
    /// </summary>
    public class QualificationAuditLog
    {
        [Key] public int Id { get; set; }

        [Display(Name = "ID kvalifikacije")] public int QualificationId { get; set; }

        [ForeignKey(nameof(QualificationId))] public Qualification? Qualification { get; set; }

        [Display(Name = "Vrijeme događaja")] public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Display(Name = "Akcija")] public string? Action { get; set; }

        [Display(Name = "Korisnik")] public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))] public User? User { get; set; }

        [Display(Name = "Opis događaja")] public string? Description { get; set; }
    }
}

