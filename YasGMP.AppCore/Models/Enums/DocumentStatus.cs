namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>DocumentStatus</b> – Workflow status for any controlled document, SOP, or record in the system.
    /// <para>
    /// Fully supports versioning, archiving, GMP audit, digital signature, and compliance checks.
    /// </para>
    /// </summary>
    public enum DocumentStatus
    {
        /// <summary>Draft document – not yet approved.</summary>
        Draft = 0,

        /// <summary>Awaiting review (pending QA or management approval).</summary>
        UnderReview = 1,

        /// <summary>Active (approved and current version).</summary>
        Active = 2,

        /// <summary>Obsolete (replaced by a new version, no longer used).</summary>
        Obsolete = 3,

        /// <summary>Archived for regulatory or legal retention.</summary>
        Archived = 4,

        /// <summary>Invalidated (formally revoked, e.g. failed audit).</summary>
        Invalidated = 5,

        /// <summary>Superseded by a newer version.</summary>
        Superseded = 6,

        /// <summary>Pending approval/signature (waiting for authorized users).</summary>
        PendingApproval = 7,

        /// <summary>Rejected after review.</summary>
        Rejected = 8,

        /// <summary>Other/future extension.</summary>
        Other = 1000
    }
}

