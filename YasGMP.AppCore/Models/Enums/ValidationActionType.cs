using System;

namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>ValidationActionType</b> – Standardized types of actions performed on GMP validation records.
    /// <para>
    /// Fully audit-ready: every creation, update, execution, approval, rollback, export, and custom/unknown operation.
    /// Used in <see cref="ValidationAudit"/> and for digital signature/audit trail compliance.
    /// </para>
    /// <para>
    /// Recommended by: EU GMP Annex 15, ICH Q2, 21 CFR Part 11, FDA guidance.
    /// </para>
    /// </summary>
    public enum ValidationActionType
    {
        /// <summary>
        /// Validation record created (IQ/OQ/PQ/URS/DQ/other).
        /// </summary>
        Create = 0,

        /// <summary>
        /// Validation record updated (any field changed, document/version added).
        /// </summary>
        Update = 1,

        /// <summary>
        /// Execution of validation protocol (actual qualification/inspection/test performed).
        /// </summary>
        Execute = 2,

        /// <summary>
        /// Validation record deleted (with audit, rarely allowed).
        /// </summary>
        Delete = 3,

        /// <summary>
        /// Validation record viewed/inspected (audit access).
        /// </summary>
        View = 4,

        /// <summary>
        /// Validation approved/signed by authorized person (with digital or manual signature).
        /// </summary>
        Approve = 5,

        /// <summary>
        /// Validation record rejected or failed approval.
        /// </summary>
        Reject = 6,

        /// <summary>
        /// Rollback to a previous validation state/version (with audit entry).
        /// </summary>
        Rollback = 7,

        /// <summary>
        /// Validation exported (PDF, Excel, etc. for audit or sharing).
        /// </summary>
        Export = 8,

        /// <summary>
        /// Validation record archived (for retention/retirement, but not deleted).
        /// </summary>
        Archive = 9,

        /// <summary>
        /// Validation record restored (from archive or rollback).
        /// </summary>
        Restore = 10,

        /// <summary>
        /// Automated/AI-driven change (e.g., scheduler, API, script).
        /// </summary>
        Automated = 11,

        /// <summary>
        /// Manual override by admin/supervisor (requires justification).
        /// </summary>
        Override = 12,

        /// <summary>
        /// Any other action not classified (future-proof).
        /// </summary>
        Other = 99
    }
}

