using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>WorkOrderStatus</b> — All possible statuses for a maintenance, calibration, validation, or inspection work order.
    /// Fully regulated for GMP, CAPA, 21 CFR Part 11, ISO, HALMED — and future expansion (automation, AI, mobile).
    /// </summary>
    public enum WorkOrderStatus
    {
        /// <summary>
        /// Work order is newly created, not yet assigned or started.
        /// </summary>
        [Display(Name = "Otvoren")]
        Open = 0,

        /// <summary>
        /// Work order is currently being processed.
        /// </summary>
        [Display(Name = "U tijeku")]
        InProgress = 1,

        /// <summary>
        /// Waiting for user/manager/supervisor approval (multi-stage workflow).
        /// </summary>
        [Display(Name = "Čeka odobrenje")]
        AwaitingApproval = 2,

        /// <summary>
        /// Pending QA/Quality/Validation review before closure.
        /// </summary>
        [Display(Name = "Čeka QA/reviziju")]
        PendingQAReview = 3,

        /// <summary>
        /// Waiting for digital signature(s) (compliance step).
        /// </summary>
        [Display(Name = "Čeka potpis")]
        PendingSignature = 4,

        /// <summary>
        /// On hold (waiting on parts, contractor, decision, info, etc.).
        /// </summary>
        [Display(Name = "Na čekanju")]
        OnHold = 5,

        /// <summary>
        /// Scheduled/planned (future, recurring, or auto-generated job).
        /// </summary>
        [Display(Name = "Planiran")]
        Planned = 6,

        /// <summary>
        /// Linked to open CAPA, incident, or deviation (escalated/critical).
        /// </summary>
        [Display(Name = "Povezan s CAPA/inc.")]
        IncidentLinked = 7,

        /// <summary>
        /// In formal revision, audit, or inspection (read-only state).
        /// </summary>
        [Display(Name = "Na reviziji/auditu")]
        UnderReview = 8,

        /// <summary>
        /// Work completed, all actions signed and approved, officially closed.
        /// </summary>
        [Display(Name = "Završen")]
        Closed = 9,

        /// <summary>
        /// Work order has been rejected (e.g., not valid, duplicate, not needed).
        /// </summary>
        [Display(Name = "Odbijen")]
        Rejected = 10,

        /// <summary>
        /// Work order was canceled before execution (by user or admin).
        /// </summary>
        [Display(Name = "Otkazan")]
        Canceled = 11,

        /// <summary>
        /// Deemed impossible to repair/resolve (linked to CAPA root cause).
        /// </summary>
        [Display(Name = "Nerješivo / CAPA")]
        Irreparable = 12,

        /// <summary>
        /// Escalated to management, QA, or external team.
        /// </summary>
        [Display(Name = "Eskalirano")]
        Escalated = 13,

        /// <summary>
        /// Abandoned, closed for inactivity or unresolved status (audit record).
        /// </summary>
        [Display(Name = "Napusteno/Neaktivno")]
        Abandoned = 14,

        /// <summary>
        /// Archived (hidden from active view, preserved for audit/history).
        /// </summary>
        [Display(Name = "Arhivirano")]
        Archived = 98,

        /// <summary>
        /// Custom status (reserved for future, legacy, or special workflows).
        /// </summary>
        [Display(Name = "Custom")]
        Custom = 1000
    }
}

