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
        [Key]
        public int Id { get; set; }

        [Required, StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [StringLength(64)]
        public string? IncidentType { get; set; }

        [StringLength(32)]
        public string Status { get; set; } = "reported";

        public string? Description { get; set; }

        // legacy fields (kept for compatibility)
        [StringLength(24)]
        public string Severity { get; set; } = "low";

        public DateTime DetectedAt { get; set; } = DateTime.Now;

        // reporter (new workflow)
        public string? ReportedBy { get; set; }
        public DateTime? ReportedAt { get; set; }

        // reporter as FK (legacy)
        public int? ReportedById { get; set; }

        [ForeignKey(nameof(ReportedById))]
        public User? ReportedByUser { get; set; }

        // assignment
        public string? AssignedTo { get; set; }
        public int? AssignedToId { get; set; }

        // details
        public string? Area { get; set; }
        public string? RootCause { get; set; }
        public int? ImpactScore { get; set; }
        public bool? LinkedCAPA { get; set; }

        // equipment (legacy)
        public int? MachineId { get; set; }

        [ForeignKey(nameof(MachineId))]
        public Machine? Machine { get; set; }

        public int? ComponentId { get; set; }

        [ForeignKey(nameof(ComponentId))]
        public MachineComponent? Component { get; set; }

        // resolution (legacy)
        public DateTime? ResolvedAt { get; set; }
        public int? ResolvedById { get; set; }

        [ForeignKey(nameof(ResolvedById))]
        public User? ResolvedBy { get; set; }

        [StringLength(255)]
        public string? Note { get; set; }

        // telemetry
        public string? DeviceInfo { get; set; }
        public string? SessionId { get; set; }
        public string? IpAddress { get; set; }

        // evidence
        public List<Photo> Photos { get; set; } = new();
        public List<Attachment> Attachments { get; set; } = new();

        // audit
        [InverseProperty(nameof(IncidentAuditLog.Incident))]
        public List<IncidentAuditLog> AuditLogs { get; set; } = new();

        [NotMapped]
        public List<IncidentAuditLog> WorkflowHistory { get; set; } = new();
    }

    /// <summary>
    /// <b>IncidentAuditLog</b> – Lightweight workflow/audit entry for <see cref="IncidentReport"/>.
    /// </summary>
    public class IncidentAuditLog
    {
        [Key]
        public int Id { get; set; }

        public int IncidentReportId { get; set; }

        [ForeignKey(nameof(IncidentReportId))]
        [InverseProperty(nameof(IncidentReport.AuditLogs))]
        public IncidentReport? Incident { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string? Action { get; set; }

        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public string? Description { get; set; }
    }
}
