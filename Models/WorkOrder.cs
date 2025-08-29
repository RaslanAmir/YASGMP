using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>WorkOrder</b> – Ultra-robust GMP-compliant maintenance, calibration, and inspection entity.
    /// <para>
    /// ✅ All core and bonus fields for maximum compatibility and futureproofing<br/>
    /// ✅ Full forensic audit, rollback, AI/ML support, signatures, attachments, CAPA, incidents<br/>
    /// ✅ Compatible with all past/future UIs, reporting, and data migration needs
    /// </para>
    /// </summary>
    [Table("work_orders")]
    [Serializable]
    public class WorkOrder
    {
        /// <summary>Unique identifier for the WorkOrder.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        // ========= BASIC CORE FIELDS ===========

        /// <summary>Short title for the work order (for UI/ERP/searching).</summary>
        [Required, StringLength(100)]
        [Column("title")]
        [Display(Name = "Naslov naloga")]
        public string Title { get; set; } = string.Empty;

        /// <summary>Detailed description of the work order.</summary>
        [Required, StringLength(255)]
        [Column("description")]
        [Display(Name = "Opis naloga")]
        public string Description { get; set; } = string.Empty;

        /// <summary>Task/Job to be performed (legacy for compatibility).</summary>
        [StringLength(255)]
        [Column("task_description")]
        [Display(Name = "Opis zadatka")]
        public string TaskDescription { get; set; } = string.Empty;

        /// <summary>Type (e.g., maintenance, calibration, inspection).</summary>
        [Required, StringLength(40)]
        [Column("type")]
        [Display(Name = "Tip naloga")]
        public string Type { get; set; } = string.Empty;

        /// <summary>Priority (Low/Medium/High/Urgent).</summary>
        [Required, StringLength(20)]
        [Column("priority")]
        [Display(Name = "Prioritet")]
        public string Priority { get; set; } = string.Empty;

        /// <summary>Status (Open/In Progress/Closed/Overdue).</summary>
        [Required, StringLength(32)]
        [Column("status")]
        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        // ========== DATES ==========
        /// <summary>Date opened.</summary>
        [Required]
        [Column("date_open")]
        [Display(Name = "Datum otvaranja")]
        public DateTime DateOpen { get; set; } = DateTime.UtcNow;

        /// <summary>Date due (planned close).</summary>
        [Column("due_date")]
        [Display(Name = "Rok izvršenja")]
        public DateTime? DueDate { get; set; }

        /// <summary>Date actually closed.</summary>
        [Column("date_close")]
        [Display(Name = "Datum zatvaranja")]
        public DateTime? DateClose { get; set; }

        /// <summary>Date actually closed (legacy/compatibility).</summary>
        [Column("closed_at")]
        [Display(Name = "Zatvoreno u")]
        public DateTime? ClosedAt { get; set; }

        // ========== USERS & ROLES ==========

        /// <summary>ID of the user who requested the work order.</summary>
        [Required]
        [Column("requested_by_id")]
        [Display(Name = "Zatražio korisnik")]
        public int RequestedById { get; set; }

        /// <summary>Navigation to requesting user.</summary>
        [ForeignKey("RequestedById")]
        public virtual User? RequestedBy { get; set; }

        /// <summary>ID of the user who created the work order.</summary>
        [Required]
        [Column("created_by_id")]
        [Display(Name = "Otvorio korisnik")]
        public int CreatedById { get; set; }

        /// <summary>Navigation to creator.</summary>
        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        /// <summary>ID of assigned technician.</summary>
        [Required]
        [Column("assigned_to_id")]
        [Display(Name = "Dodijeljeno tehničaru")]
        public int AssignedToId { get; set; }

        /// <summary>Navigation to assigned user.</summary>
        [ForeignKey("AssignedToId")]
        public virtual User? AssignedTo { get; set; }

        // ========== MACHINES & COMPONENTS ==========
        /// <summary>ID of related machine/asset.</summary>
        [Required]
        [Column("machine_id")]
        [Display(Name = "Stroj/oprema")]
        public int MachineId { get; set; }

        /// <summary>Navigation to machine.</summary>
        [ForeignKey("MachineId")]
        public virtual Machine? Machine { get; set; }

        /// <summary>ID of affected component.</summary>
        [Column("component_id")]
        [Display(Name = "Komponenta")]
        public int? ComponentId { get; set; }

        /// <summary>Navigation to component.</summary>
        [ForeignKey("ComponentId")]
        public virtual MachineComponent? Component { get; set; }

        // ========== CAPA & INCIDENT ==========
        [Column("capa_case_id")]
        [Display(Name = "CAPA slučaj")]
        public int? CapaCaseId { get; set; }

        [ForeignKey("CapaCaseId")]
        public virtual CapaCase? CapaCase { get; set; }

        [Column("incident_id")]
        [Display(Name = "Incident")]
        public int? IncidentId { get; set; }

        [ForeignKey("IncidentId")]
        public virtual Incident? Incident { get; set; }

        // ========== RESULTS / FINDINGS ==========
        /// <summary>Result or findings of the intervention.</summary>
        [Required]
        [Column("result")]
        [Display(Name = "Rezultat/nalaz")]
        public string Result { get; set; } = string.Empty;

        /// <summary>Free-form notes or comments (max 512 chars).</summary>
        [StringLength(512)]
        [Column("notes")]
        [Display(Name = "Napomena")]
        public string Notes { get; set; } = string.Empty;

        // ========== ADVANCED / FORENSIC ==========
        [StringLength(128)]
        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        public string DigitalSignature { get; set; } = string.Empty;

        [Column("last_modified")]
        [Display(Name = "Zadnja izmjena")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        [Column("last_modified_by_id")]
        [Display(Name = "Zadnji izmijenio")]
        public int? LastModifiedById { get; set; }

        [ForeignKey("LastModifiedById")]
        public virtual User? LastModifiedBy { get; set; }

        [StringLength(256)]
        [Column("device_info")]
        [Display(Name = "Forenzički uređaj/IP/session")]
        public string DeviceInfo { get; set; } = string.Empty;

        [StringLength(45)]
        [Column("source_ip")]
        [Display(Name = "Forenzički IP")]
        public string SourceIp { get; set; } = string.Empty;

        [StringLength(64)]
        [Column("session_id")]
        [Display(Name = "Forenzički session")]
        public string SessionId { get; set; } = string.Empty;

        [StringLength(512)]
        [Column("document_path")]
        public string DocumentPath { get; set; } = string.Empty;

        [Column("next_due")]
        public DateTime? NextDue { get; set; }

        [StringLength(100)]
        [Column("external_ref")]
        public string ExternalRef { get; set; } = string.Empty;

        [StringLength(128)]
        [Column("entry_hash")]
        public string EntryHash { get; set; } = string.Empty;

        [Column("audit_flag")]
        public bool AuditFlag { get; set; }

        [Column("anomaly_score")]
        public double? AnomalyScore { get; set; }

        // ========== PHOTOS, PARTS, COMMENTS, LOGS, ETC ==========
        [Display(Name = "Slike prije intervencije")]
        public List<int> PhotoBeforeIds { get; set; } = new();

        [Display(Name = "Slike nakon intervencije")]
        public List<int> PhotoAfterIds { get; set; } = new();

        [Display(Name = "Upotrijebljeni dijelovi")]
        public virtual ICollection<WorkOrderPart> UsedParts { get; set; } = new List<WorkOrderPart>();

        public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();

        public virtual ICollection<WorkOrderComment> Comments { get; set; } = new List<WorkOrderComment>();

        public virtual ICollection<WorkOrderStatusLog> StatusTimeline { get; set; } = new List<WorkOrderStatusLog>();

        public virtual ICollection<WorkOrderSignature> Signatures { get; set; } = new List<WorkOrderSignature>();

        public virtual ICollection<WorkOrderAudit> AuditTrail { get; set; } = new List<WorkOrderAudit>();

        // ========== COMPUTED PROPERTIES ==========
        /// <summary>Duration of work order (if closed).</summary>
        [NotMapped]
        [Display(Name = "Trajanje intervencije")]
        public TimeSpan? Duration => DateClose.HasValue ? DateClose - DateOpen : null;

        // ========== COPY/CLONE ==========
        /// <summary>
        /// DeepCopy – makes a full, isolated copy for dialog editing/rollback/audit/inspection.
        /// </summary>
        public WorkOrder DeepCopy()
        {
            return new WorkOrder
            {
                Id = this.Id,
                Title = this.Title,
                Description = this.Description,
                TaskDescription = this.TaskDescription,
                Type = this.Type,
                Priority = this.Priority,
                Status = this.Status,
                DateOpen = this.DateOpen,
                DueDate = this.DueDate,
                DateClose = this.DateClose,
                ClosedAt = this.ClosedAt,
                RequestedById = this.RequestedById,
                RequestedBy = this.RequestedBy,
                CreatedById = this.CreatedById,
                CreatedBy = this.CreatedBy,
                AssignedToId = this.AssignedToId,
                AssignedTo = this.AssignedTo,
                MachineId = this.MachineId,
                Machine = this.Machine,
                ComponentId = this.ComponentId,
                Component = this.Component,
                CapaCaseId = this.CapaCaseId,
                CapaCase = this.CapaCase,
                IncidentId = this.IncidentId,
                Incident = this.Incident,
                Result = this.Result,
                Notes = this.Notes,
                DigitalSignature = this.DigitalSignature,
                LastModified = this.LastModified,
                LastModifiedById = this.LastModifiedById,
                LastModifiedBy = this.LastModifiedBy,
                DeviceInfo = this.DeviceInfo,
                SourceIp = this.SourceIp,
                SessionId = this.SessionId,
                DocumentPath = this.DocumentPath,
                NextDue = this.NextDue,
                ExternalRef = this.ExternalRef,
                EntryHash = this.EntryHash,
                AuditFlag = this.AuditFlag,
                AnomalyScore = this.AnomalyScore,
                PhotoBeforeIds = new List<int>(this.PhotoBeforeIds),
                PhotoAfterIds = new List<int>(this.PhotoAfterIds),
                UsedParts = new List<WorkOrderPart>(this.UsedParts),
                Photos = new List<Photo>(this.Photos),
                Comments = new List<WorkOrderComment>(this.Comments),
                StatusTimeline = new List<WorkOrderStatusLog>(this.StatusTimeline),
                Signatures = new List<WorkOrderSignature>(this.Signatures),
                AuditTrail = new List<WorkOrderAudit>(this.AuditTrail)
            };
        }
    }
}
