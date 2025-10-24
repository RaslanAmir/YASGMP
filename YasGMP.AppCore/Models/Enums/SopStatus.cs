namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>SopStatus</b> – Full lifecycle status for SOPs and controlled documents.
    /// <para>
    /// Supports regulatory compliance (GMP, ISO, FDA), multi-stage approval, archival, and replacement.
    /// </para>
    /// </summary>
    public enum SopStatus
    {
        /// <summary>
        /// Document is in draft, can be edited and not yet reviewed.
        /// </summary>
        Draft = 0,

        /// <summary>
        /// Officially active and in use (current, controlled, approved for use).
        /// </summary>
        Active = 1,

        /// <summary>
        /// Under review (awaiting approval, feedback, or edit).
        /// </summary>
        UnderReview = 2,

        /// <summary>
        /// Explicitly approved (fully signed-off, pending activation).
        /// </summary>
        Approved = 3,

        /// <summary>
        /// Archived (retired, preserved for audit but not in active use).
        /// </summary>
        Archived = 4,

        /// <summary>
        /// Obsolete (withdrawn, no longer valid, replaced by new version).
        /// </summary>
        Obsolete = 5,

        /// <summary>
        /// Superseded (actively replaced by another document/version).
        /// </summary>
        Superseded = 6,

        /// <summary>
        /// Rejected during approval or review (bonus: trace failed docs!).
        /// </summary>
        Rejected = 7,

        /// <summary>
        /// Pending user acknowledgment/read-and-understand (bonus: compliance!).
        /// </summary>
        PendingAcknowledgement = 8
    }
}

