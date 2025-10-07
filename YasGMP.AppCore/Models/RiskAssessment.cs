using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>RiskAssessment</b> – Ultra-robust record for GMP/CMMS/QMS risk analysis, mitigation, audit, and compliance.
    /// <para>
    /// ✅ Tracks risk assessment events: title, category/area, detailed description, scoring (S×P×D),
    /// mitigation/action plan, owner/approval workflow, attachments and workflow history.<br/>
    /// ✅ Regulatory-ready (ICH Q9, 21 CFR Part 11, Annex 11, ISO 14971, HALMED).<br/>
    /// ✅ Full audit, digital signatures, escalation, document linkage, periodic review, forensics fields (IP/device/session).
    /// </para>
    /// </summary>
    public class RiskAssessment
    {
        /// <summary>Unique risk assessment ID (Primary Key).</summary>
        [Key]
        [Display(Name = "ID procjene rizika")]
        public int Id { get; set; }

        /// <summary>Risk assessment code (e.g., "RA-2024-01").</summary>
        [Required, StringLength(40)]
        [Display(Name = "Oznaka procjene")]
        public string Code { get; set; } = string.Empty;

        /// <summary>Title/summary of the risk assessment.</summary>
        [Required, StringLength(255)]
        [Display(Name = "Naziv")]
        public string Title { get; set; } = string.Empty;

        /// <summary>Detailed risk description/context (what/where/why/impact).</summary>
        [Display(Name = "Opis")]
        public string? Description { get; set; }

        /// <summary>Category of risk (process, equipment, supplier, IT, product, validation, other).</summary>
        [StringLength(64)]
        [Display(Name = "Kategorija")]
        public string? Category { get; set; }

        /// <summary>Department, process or asset area under analysis (optional).</summary>
        [StringLength(128)]
        [Display(Name = "Područje/proces/stroj")]
        public string? Area { get; set; }

        /// <summary>Assessment status (initiated, in_progress, pending_approval, effectiveness_check, closed, rejected).</summary>
        [StringLength(32)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "initiated";

        /// <summary>Assessor username (human-readable; FK optional).</summary>
        [StringLength(100)]
        [Display(Name = "Procijenio")]
        public string? AssessedBy { get; set; }

        /// <summary>UTC timestamp when the assessment was performed or initiated.</summary>
        [Display(Name = "Vrijeme procjene (UTC)")]
        public DateTime? AssessedAt { get; set; }

        /// <summary>Severity score (e.g., 1–5).</summary>
        [Display(Name = "Težina (S)")]
        public int Severity { get; set; }

        /// <summary>Probability score (e.g., 1–5).</summary>
        [Display(Name = "Vjerojatnost (P)")]
        public int Probability { get; set; }

        /// <summary>Detection score (e.g., 1–10 or 1–5; lower = better detectability).</summary>
        [Display(Name = "Detektabilnost (D)")]
        public int Detection { get; set; }

        /// <summary>Computed risk score (typically S × P × D).</summary>
        [Display(Name = "Rizik bodovi")]
        public int? RiskScore { get; set; }

        /// <summary>Risk level (Low, Medium, High, Critical).</summary>
        [StringLength(24)]
        [Display(Name = "Razina rizika")]
        public string RiskLevel { get; set; } = "Low";

        /// <summary>High-level mitigation strategy and implemented/required controls.</summary>
        [Display(Name = "Mjere smanjenja rizika")]
        public string? Mitigation { get; set; }

        /// <summary>Action plan details (tasks, owners, due dates, KPIs); free text or serialized JSON.</summary>
        [Display(Name = "Akcijski plan")]
        public string? ActionPlan { get; set; }

        /// <summary>Owner/responsible user (FK).</summary>
        [Display(Name = "Vlasnik/odgovorna osoba")]
        public int? OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the owner.
        /// </summary>
        [ForeignKey(nameof(OwnerId))]
        public User? Owner { get; set; }

        /// <summary>Approval user (FK).</summary>
        [Display(Name = "Odobrio korisnik")]
        public int? ApprovedById { get; set; }

        /// <summary>
        /// Gets or sets the approved by.
        /// </summary>
        [ForeignKey(nameof(ApprovedById))]
        public User? ApprovedBy { get; set; }

        /// <summary>Approval date/time (UTC).</summary>
        [Display(Name = "Datum odobravanja (UTC)")]
        public DateTime? ApprovedAt { get; set; }

        /// <summary>Next review/expiry date (for periodic review of risk).</summary>
        [Display(Name = "Datum revizije/isteka")]
        public DateTime? ReviewDate { get; set; }

        /// <summary>Digital signature of the latest change/approval (hash or certificate reference).</summary>
        [StringLength(255)]
        [Display(Name = "Digitalni potpis")]
        public string? DigitalSignature { get; set; }

        /// <summary>Free notes, escalation context, regulatory references.</summary>
        [StringLength(255)]
        [Display(Name = "Napomene")]
        public string? Note { get; set; }

        /// <summary>Attachments (file paths/URIs). Keep simple string paths for portability.</summary>
        [Display(Name = "Prilozi")]
        public List<string> Attachments { get; set; } = new();

        /// <summary>Workflow history (status changes, notes, assignments) – light model for UI and export.</summary>
        [Display(Name = "Povijest tijeka rada")]
        [NotMapped]
        public List<RiskWorkflowEntry> WorkflowHistory { get; set; } = new();

        /// <summary>Device info string captured on last change (browser/OS/host).</summary>
        [StringLength(200)]
        [Display(Name = "Uređaj")]
        public string? DeviceInfo { get; set; }

        /// <summary>Client/session correlation id (forensics).</summary>
        [StringLength(100)]
        [Display(Name = "Sesija")]
        public string? SessionId { get; set; }

        /// <summary>Source IP address of last change.</summary>
        [StringLength(45)]
        [Display(Name = "IP adresa")]
        public string? IpAddress { get; set; }

        /// <summary>Linked documents (SOPs, reports, evidence).</summary>
        [Display(Name = "Povezani dokumenti")]
        public List<DocumentControl> Documents { get; set; } = new();

        /// <summary>Full audit trail of risk assessment process.</summary>
        [Display(Name = "Evidencija aktivnosti")]
        public List<RiskAssessmentAuditLog> AuditLogs { get; set; } = new();
    }

    /// <summary>
    /// <b>RiskWorkflowEntry</b> – Lightweight workflow history line for a risk assessment (for UI/export).
    /// Use DB audit tables for authoritative history; this is a convenience projection.
    /// </summary>
    [NotMapped]
    public class RiskWorkflowEntry
    {
        /// <summary>UTC timestamp of the workflow event.</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Action label (initiate, update, approve, close, escalate …).</summary>
        public string? Action { get; set; }

        /// <summary>Actor username or display name.</summary>
        public string? PerformedBy { get; set; }

        /// <summary>Optional note/comment.</summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// <b>RiskAssessmentAuditLog</b> – Single audit entry for risk assessment events (creation, review, mitigation, approval, etc.).
    /// DB audit is authoritative; keep this class for navigation properties and exports.
    /// </summary>
    public class RiskAssessmentAuditLog
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the risk assessment id.
        /// </summary>
        [Display(Name = "ID procjene rizika")]
        public int RiskAssessmentId { get; set; }

        /// <summary>
        /// Gets or sets the risk assessment.
        /// </summary>
        [ForeignKey(nameof(RiskAssessmentId))]
        public RiskAssessment? RiskAssessment { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        [Display(Name = "Vrijeme događaja")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        [Display(Name = "Akcija")]
        public string? Action { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        [Display(Name = "Korisnik")]
        public int? UserId { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [Display(Name = "Opis događaja")]
        public string? Description { get; set; }
    }
}
