using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>TrainingRecord</b> – Super ultra mega robust master record for employee training, GMP/CMMS compliance, and e-learning.
    /// <para>
    /// ✅ Tracks all regulatory, technical, safety, SOP, and equipment trainings (internal/external, e-learning, on-site).<br/>
    /// ✅ Supports full linkage to users, roles, departments, trainers, training modules, SOPs, certificates, audit, and digital signatures.<br/>
    /// ✅ Forensic-ready: attendance, attachments, session history, skill matrix, expiry, re-training, escalation, and regulatory inspection.<br/>
    /// ✅ Extensible for e-learning (quiz, result, media), group/individual, workflow (approval, acknowledgment), multi-language, and mobile.
    /// </para>
    /// <para>
    /// This class also exposes a set of <b>[NotMapped] bridge properties</b> so older/newer ViewModels
    /// with slightly different names compile without any schema change (e.g. <c>Title</c> → <c>Name</c>,
    /// <c>TrainingType</c> → <c>Type</c>, etc.).
    /// </para>
    /// </summary>
    public class TrainingRecord
    {
        /// <summary>Unique training record ID (Primary Key).</summary>
        [Key]
        [Display(Name = "ID zapisa obuke")]
        public int Id { get; set; }

        /// <summary>Training code/identifier (e.g., TR-2024-007).</summary>
        [Required, StringLength(50)]
        [Display(Name = "Šifra obuke")]
        public string Code { get; set; } = string.Empty;

        /// <summary>Name or title of training (e.g., "GMP Awareness", "SOP-123 Revision 2").</summary>
        [Required, StringLength(255)]
        [Display(Name = "Naziv obuke")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Detailed description of training content or purpose.</summary>
        [Display(Name = "Opis obuke")]
        public string Description { get; set; } = string.Empty;

        /// <summary>Training type (SOP, EHS, Equipment, GMP, Soft Skills, E-learning, On-site, etc.).</summary>
        [StringLength(48)]
        [Display(Name = "Tip obuke")]
        public string Type { get; set; } = string.Empty;

        /// <summary>Format (internal, external, blended, e-learning, on-site, remote).</summary>
        [StringLength(32)]
        [Display(Name = "Format")]
        public string Format { get; set; } = string.Empty;

        /// <summary>Date and time of training session.</summary>
        [Display(Name = "Datum i vrijeme")]
        public DateTime TrainingDate { get; set; } = DateTime.Now;

        /// <summary>Expiry/retraining due date (for recurring/periodic).</summary>
        [Display(Name = "Datum isteka / ponovne obuke")]
        public DateTime? ExpiryDate { get; set; }

        /// <summary>Training status (scheduled, planned, assigned, pending_approval, completed, closed, overdue, expired, rejected, etc.).</summary>
        [StringLength(32)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "scheduled";

        /// <summary>User ID of trainer (FK).</summary>
        [Display(Name = "Trener/edukator")]
        public int? TrainerId { get; set; }

        /// <summary>Navigation to trainer.</summary>
        [ForeignKey(nameof(TrainerId))]
        public User Trainer { get; set; } = null!;

        /// <summary>Main attendee/employee (FK).</summary>
        [Display(Name = "Korisnik (polaznik)")]
        public int? TraineeId { get; set; }

        /// <summary>Navigation to trainee.</summary>
        [ForeignKey(nameof(TraineeId))]
        public User Trainee { get; set; } = null!;

        /// <summary>Optional: list of all attendees (group training).</summary>
        [Display(Name = "Polaznici")]
        public List<User> Attendees { get; set; } = new List<User>();

        /// <summary>Related role/department (for skill matrix, regulatory compliance).</summary>
        [Display(Name = "Uloga / Odjel")]
        public int? RoleId { get; set; }

        /// <summary>Navigation to role.</summary>
        [ForeignKey(nameof(RoleId))]
        public Role Role { get; set; } = null!;

        /// <summary>Related SOP, Document, or Equipment (FK).</summary>
        [Display(Name = "Dokument/SOP")]
        public int? DocumentId { get; set; }

        /// <summary>Navigation to document.</summary>
        [ForeignKey(nameof(DocumentId))]
        public DocumentControl Document { get; set; } = null!;

        /// <summary>List of related documents, SOPs, or certificates.</summary>
        [Display(Name = "Dokumenti / Certifikati")]
        public List<DocumentControl> Documents { get; set; } = new List<DocumentControl>();

        /// <summary>Certificate number (for regulatory proof/tracing).</summary>
        [StringLength(100)]
        [Display(Name = "Broj certifikata")]
        public string CertificateNumber { get; set; } = string.Empty;

        /// <summary>Training score (for tests/quiz, e-learning).</summary>
        [Display(Name = "Rezultat testa / kviza")]
        public decimal? TestScore { get; set; }

        /// <summary>True if training includes an online/e-learning module.</summary>
        [Display(Name = "E-learning")]
        public bool IsELearning { get; set; } = false;

        /// <summary>Digital signature of the trainee (acknowledgement, regulatory).</summary>
        [StringLength(255)]
        [Display(Name = "Digitalni potpis (polaznik)")]
        public string TraineeSignature { get; set; } = string.Empty;

        /// <summary>Digital signature of the trainer (forensics, regulatory).</summary>
        [StringLength(255)]
        [Display(Name = "Digitalni potpis (trener)")]
        public string TrainerSignature { get; set; } = string.Empty;

        /// <summary>List of attached files (proof, photos, evidence).</summary>
        [Display(Name = "Prilozi")]
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();

        /// <summary>Audit log for all training events (creation, attendance, results, approval).</summary>
        [Display(Name = "Audit log")]
        public List<TrainingAuditLog> AuditLogs { get; set; } = new List<TrainingAuditLog>();

        /// <summary>Notes, regulatory comments, inspection history.</summary>
        [StringLength(255)]
        [Display(Name = "Napomene")]
        public string Note { get; set; } = string.Empty;

        // ---------------------------------------------------------------------
        // BRIDGE PROPERTIES (for ViewModel compatibility without schema changes)
        // ---------------------------------------------------------------------

        /// <summary>Bridge to <see cref="Name"/>; present for compatibility with ViewModels using <c>Title</c>.</summary>
        [NotMapped]
        public string Title
        {
            get => Name;
            set => Name = value ?? string.Empty;
        }

        /// <summary>Bridge to <see cref="Type"/>; present for compatibility with ViewModels using <c>TrainingType</c>.</summary>
        [NotMapped]
        public string TrainingType
        {
            get => Type;
            set => Type = value ?? string.Empty;
        }

        /// <summary>Planner username (UI-only; persisted via audit/system logs as needed).</summary>
        [NotMapped]
        public string PlannedBy { get; set; } = string.Empty;

        /// <summary>Planned timestamp (UI-only).</summary>
        [NotMapped]
        public DateTime PlannedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Bridge to <see cref="TraineeId"/>; used by ViewModels as a simple assignee field.</summary>
        [NotMapped]
        public int? AssignedTo
        {
            get => TraineeId;
            set => TraineeId = value;
        }

        /// <summary>Display name of the assignee (UI-only).</summary>
        [NotMapped]
        public string AssignedToName { get; set; } = string.Empty;

        /// <summary>Due date for finishing the training (UI-only).</summary>
        [NotMapped]
        public DateTime? DueDate { get; set; }

        /// <summary>Effectiveness check flag (UI-only).</summary>
        [NotMapped]
        public bool EffectivenessCheck { get; set; }

        /// <summary>Free-form workflow history (UI-only).</summary>
        [NotMapped]
        public List<string> WorkflowHistory { get; set; } = new List<string>();

        /// <summary>Device info (forensics, UI-only bridge).</summary>
        [NotMapped]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>Session Id (forensics, UI-only bridge).</summary>
        [NotMapped]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>IP Address (forensics, UI-only bridge).</summary>
        [NotMapped]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>Linked SOP code/name (UI-only bridge when no FK is present).</summary>
        [NotMapped]
        public string LinkedSOP { get; set; } = string.Empty;
    }

    /// <summary>
    /// <b>TrainingAuditLog</b> – Full forensic audit of every action and change for training records.
    /// </summary>
    public class TrainingAuditLog
    {
        /// <summary>Primary Key.</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>FK to <see cref="TrainingRecord"/>.</summary>
        [Display(Name = "ID obuke")]
        public int TrainingRecordId { get; set; }

        /// <summary>Navigation to training record.</summary>
        [ForeignKey(nameof(TrainingRecordId))]
        public TrainingRecord TrainingRecord { get; set; } = null!;

        /// <summary>Event timestamp.</summary>
        [Display(Name = "Vrijeme događaja")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>Action performed.</summary>
        [Display(Name = "Akcija")]
        public string Action { get; set; } = string.Empty;

        /// <summary>User who caused the event (optional).</summary>
        [Display(Name = "Korisnik")]
        public int? UserId { get; set; }

        /// <summary>Navigation to user.</summary>
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        /// <summary>Description / details.</summary>
        [StringLength(255)]
        [Display(Name = "Opis")]
        public string Description { get; set; } = string.Empty;
    }
}

