using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>IncidentReport</b> – Modern incident reporting DTO/entity with legacy-compatible fields.
    /// Fully instrumented for dashboards, workflows, and audit trails.
    /// </summary>
    public class IncidentReport
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [Required, StringLength(255)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the incident type.
        /// </summary>
        [StringLength(64)]
        public string? IncidentType { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        [StringLength(32)]
        public string Status { get; set; } = "reported";
        /// <summary>
        /// Gets or sets the description.
        /// </summary>

        public string? Description { get; set; }

        // legacy fields (kept for compatibility)
        /// <summary>
        /// Gets or sets the severity.
        /// </summary>
        [StringLength(24)]
        public string Severity { get; set; } = "low";
        /// <summary>
        /// Gets or sets the detected at.
        /// </summary>

        public DateTime DetectedAt { get; set; } = DateTime.Now;

        // reporter (new workflow)
        /// <summary>
        /// Gets or sets the reported by.
        /// </summary>
        public string? ReportedBy { get; set; }
        /// <summary>
        /// Gets or sets the reported at.
        /// </summary>
        public DateTime? ReportedAt { get; set; }

        // reporter as FK (legacy)
        /// <summary>
        /// Gets or sets the reported by id.
        /// </summary>
        public int? ReportedById { get; set; }

        /// <summary>
        /// Gets or sets the reported by user.
        /// </summary>
        [ForeignKey(nameof(ReportedById))]
        public User? ReportedByUser { get; set; }

        // assignment
        /// <summary>
        /// Gets or sets the assigned to.
        /// </summary>
        public string? AssignedTo { get; set; }
        /// <summary>
        /// Gets or sets the assigned to id.
        /// </summary>
        public int? AssignedToId { get; set; }

        // details
        /// <summary>
        /// Gets or sets the area.
        /// </summary>
        public string? Area { get; set; }
        /// <summary>
        /// Gets or sets the root cause.
        /// </summary>
        public string? RootCause { get; set; }
        /// <summary>
        /// Gets or sets the impact score.
        /// </summary>
        public int? ImpactScore { get; set; }
        /// <summary>
        /// Gets or sets the linked capa.
        /// </summary>
        public bool? LinkedCAPA { get; set; }

        // equipment (legacy)
        /// <summary>
        /// Gets or sets the machine id.
        /// </summary>
        public int? MachineId { get; set; }

        /// <summary>
        /// Gets or sets the machine.
        /// </summary>
        [ForeignKey(nameof(MachineId))]
        public Machine? Machine { get; set; }
        /// <summary>
        /// Gets or sets the component id.
        /// </summary>

        public int? ComponentId { get; set; }

        /// <summary>
        /// Gets or sets the component.
        /// </summary>
        [ForeignKey(nameof(ComponentId))]
        public MachineComponent? Component { get; set; }

        // resolution (legacy)
        /// <summary>
        /// Gets or sets the resolved at.
        /// </summary>
        public DateTime? ResolvedAt { get; set; }
        /// <summary>
        /// Gets or sets the resolved by id.
        /// </summary>
        public int? ResolvedById { get; set; }

        /// <summary>
        /// Gets or sets the resolved by.
        /// </summary>
        [ForeignKey(nameof(ResolvedById))]
        public User? ResolvedBy { get; set; }

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        [StringLength(255)]
        public string? Note { get; set; }

        // telemetry
        /// <summary>
        /// Gets or sets the device info.
        /// </summary>
        public string? DeviceInfo { get; set; }
        /// <summary>
        /// Gets or sets the session id.
        /// </summary>
        public string? SessionId { get; set; }
        /// <summary>
        /// Gets or sets the ip address.
        /// </summary>
        public string? IpAddress { get; set; }

        // evidence
        /// <summary>
        /// Gets or sets the photos.
        /// </summary>
        public List<Photo> Photos { get; set; } = new();
        /// <summary>
        /// Gets or sets the attachments.
        /// </summary>
        public List<Attachment> Attachments { get; set; } = new();

        // audit
        /// <summary>
        /// Gets or sets the audit logs.
        /// </summary>
        [InverseProperty(nameof(IncidentAuditLog.Incident))]
        public List<IncidentAuditLog> AuditLogs { get; set; } = new();

        /// <summary>
        /// Gets or sets the workflow history.
        /// </summary>
        [InverseProperty(nameof(IncidentAuditLog.WorkflowIncident))]
        public List<IncidentAuditLog> WorkflowHistory { get; set; } = new();
    }

    /// <summary>
    /// <b>IncidentAuditLog</b> – Lightweight workflow/audit entry for <see cref="IncidentReport"/>.
    /// </summary>
    public class IncidentAuditLog
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the incident report id.
        /// </summary>

        public int IncidentReportId { get; set; }

        /// <summary>
        /// Gets or sets the incident.
        /// </summary>
        [ForeignKey(nameof(IncidentReportId))]
        [InverseProperty(nameof(IncidentReport.AuditLogs))]
        public IncidentReport? Incident { get; set; }
        /// <summary>
        /// Gets or sets the workflow incident report id.
        /// </summary>

        public int? WorkflowIncidentReportId { get; set; }

        /// <summary>
        /// Gets or sets the workflow incident.
        /// </summary>
        [ForeignKey(nameof(WorkflowIncidentReportId))]
        [InverseProperty(nameof(IncidentReport.WorkflowHistory))]
        public IncidentReport? WorkflowIncident { get; set; }
        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>

        public DateTime Timestamp { get; set; } = DateTime.Now;
        /// <summary>
        /// Gets or sets the action.
        /// </summary>

        public string? Action { get; set; }
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>

        public int? UserId { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
        /// <summary>
        /// Gets or sets the description.
        /// </summary>

        public string? Description { get; set; }
    }
}
