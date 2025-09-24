using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>IncidentAction</b> – Represents a single corrective/preventive step in a GMP/CMMS incident workflow.
    /// <para>
    /// ⭐ SUPER-MEGA-ULTRA ROBUST:<br/>
    /// - Tracks performer, time, action type, full description, result, root cause<br/>
    /// - Full navigation to users, attachments, digital signature, device/IP, modification chain<br/>
    /// - 100% audit ready, supports timeline analytics and Part 11 forensics
    /// </para>
    /// </summary>
    public class IncidentAction
    {
        /// <summary>Unique identifier for the incident action (Primary Key).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Foreign key linking this action to its parent incident.</summary>
        [Required]
        public int IncidentId { get; set; }

        /// <summary>Navigation property to the related incident.</summary>
        [ForeignKey(nameof(IncidentId))]
        public Incident? Incident { get; set; }

        /// <summary>Type/classification of the action (investigation, containment, correction, closure, etc.).</summary>
        [Required, StringLength(100)]
        [Display(Name = "Vrsta radnje")]
        public string ActionType { get; set; } = string.Empty;

        /// <summary>Detailed description of what was done during this action.</summary>
        [StringLength(1000)]
        [Display(Name = "Opis radnje")]
        public string? Description { get; set; }

        /// <summary>Timestamp when the action was performed.</summary>
        [Required]
        [Display(Name = "Vrijeme radnje")]
        public DateTime ActionAt { get; set; } = DateTime.UtcNow;

        /// <summary>ID of the user who performed this action (FK).</summary>
        public int UserId { get; set; }

        /// <summary>Navigation to the user who performed this action.</summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>Result or conclusion of this step (passed, contained, failed, escalated, etc.).</summary>
        [StringLength(200)]
        [Display(Name = "Rezultat")]
        public string? Result { get; set; }

        /// <summary>Root cause identified during this step (optional).</summary>
        [StringLength(300)]
        [Display(Name = "Uzrok")]
        public string? RootCause { get; set; }

        /// <summary>List of file attachments (photos, documents, reports) linked to this action.</summary>
        public List<Attachment> Attachments { get; set; } = new();

        /// <summary>Digital signature or cryptographic hash of this action (for compliance).</summary>
        [StringLength(128)]
        [Display(Name = "Digitalni potpis")]
        public string? DigitalSignature { get; set; }

        /// <summary>Device information or IP address from which this action was logged (forensics).</summary>
        [StringLength(64)]
        [Display(Name = "Izvor (IP/uređaj)")]
        public string? SourceIp { get; set; }

        /// <summary>Additional notes or comments about this action.</summary>
        [StringLength(500)]
        [Display(Name = "Bilješka")]
        public string? Note { get; set; }

        /// <summary>Timestamp of the last modification (audit).</summary>
        [Display(Name = "Zadnja izmjena")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>ID of the user who last modified this action (audit).</summary>
        public int? LastModifiedById { get; set; }

        /// <summary>Navigation to the user who last modified this action.</summary>
        [ForeignKey(nameof(LastModifiedById))]
        public User? LastModifiedBy { get; set; }

        /// <summary>Indicates whether the root cause was identified during this action.</summary>
        [NotMapped]
        public bool IsRootCauseIdentified => !string.IsNullOrWhiteSpace(RootCause);

        /// <summary>Creates a deep copy of the incident action for rollback or audit inspections.</summary>
        public IncidentAction DeepCopy()
        {
            return new IncidentAction
            {
                Id = this.Id,
                IncidentId = this.IncidentId,
                Incident = this.Incident,
                ActionType = this.ActionType,
                Description = this.Description,
                ActionAt = this.ActionAt,
                UserId = this.UserId,
                User = this.User,
                Result = this.Result,
                RootCause = this.RootCause,
                Attachments = new List<Attachment>(this.Attachments.ConvertAll(a => a.DeepCopy())),
                DigitalSignature = this.DigitalSignature,
                SourceIp = this.SourceIp,
                Note = this.Note,
                LastModified = this.LastModified,
                LastModifiedById = this.LastModifiedById,
                LastModifiedBy = this.LastModifiedBy
            };
        }

        /// <summary>Returns a human-readable string for debugging or logging purposes.</summary>
        public override string ToString()
        {
            return $"{ActionType} @ {ActionAt:yyyy-MM-dd HH:mm} by User#{UserId} – Result: {Result}";
        }
    }
}
