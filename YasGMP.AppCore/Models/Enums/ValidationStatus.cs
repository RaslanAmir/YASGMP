namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>ValidationStatus</b> – All possible states of a GMP validation/qualification record.
    /// Fully compliant with EU GMP Annex 15, ISO 13485, 21 CFR Part 11. Future-proof for digital workflow, audit, dashboard, reporting.
    /// </summary>
    public enum ValidationStatus
    {
        /// <summary>
        /// Validation is planned but not yet started (draft).
        /// </summary>
        Planned = 0,

        /// <summary>
        /// Validation activity has started (protocol issued/initiated).
        /// </summary>
        InProgress = 1,

        /// <summary>
        /// Validation is completed and has passed all acceptance criteria.
        /// </summary>
        Successful = 2,

        /// <summary>
        /// Validation completed but did not meet all acceptance criteria.
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Validation has been reviewed and approved by authorized personnel.
        /// </summary>
        Approved = 4,

        /// <summary>
        /// Validation is pending approval after completion (QA or supervisor review).
        /// </summary>
        AwaitingApproval = 5,

        /// <summary>
        /// Validation under review (audit/inspection or CAPA related).
        /// </summary>
        UnderReview = 6,

        /// <summary>
        /// Validation record is expired (requires revalidation).
        /// </summary>
        Expired = 7,

        /// <summary>
        /// Validation has been cancelled before execution or completion.
        /// </summary>
        Cancelled = 8,

        /// <summary>
        /// Validation record archived (for long-term retention, not active).
        /// </summary>
        Archived = 9,

        /// <summary>
        /// Validation has been suspended (paused, on hold, or blocked).
        /// </summary>
        Suspended = 10,

        /// <summary>
        /// Validation record superseded by a newer version/protocol.
        /// </summary>
        Superseded = 11,

        /// <summary>
        /// Validation record was closed and then reopened (e.g., new finding, CAPA).
        /// </summary>
        Reopened = 12,

        /// <summary>
        /// Validation rejected by reviewer/QA (non-compliant).
        /// </summary>
        Rejected = 13,

        /// <summary>
        /// Any other custom or unknown state (future-proof, integration, migration, etc.).
        /// </summary>
        Other = 99
    }
}

