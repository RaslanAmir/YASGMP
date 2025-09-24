namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>SopActionType</b> – Enumerates all possible actions/events on a Standard Operating Procedure document.
    /// <para>
    /// Drives audit logs, change control, versioning, approvals, and full traceability.
    /// </para>
    /// </summary>
    public enum SopActionType
    {
        /// <summary>SOP created (first version).</summary>
        Create = 0,
        /// <summary>SOP updated (fields, document, details).</summary>
        Update = 1,
        /// <summary>SOP archived (not used, but retained for compliance).</summary>
        Archive = 2,
        /// <summary>SOP approved (by QA, management, or digital signature).</summary>
        Approve = 3,
        /// <summary>SOP invalidated (withdrawn from use).</summary>
        Invalidate = 4,
        /// <summary>Review action (QA, periodic, or regulatory review).</summary>
        Review = 5,
        /// <summary>New version of SOP issued.</summary>
        NewVersion = 6,
        /// <summary>SOP exported (PDF, Excel, XML, API, etc).</summary>
        Export = 7,
        /// <summary>SOP digitally signed.</summary>
        Sign = 8,
        /// <summary>SOP deleted (with GMP justification, only if allowed).</summary>
        Delete = 9,
        /// <summary>Manual override or force action (admin/superuser only).</summary>
        Override = 10,
        /// <summary>Restored from archive/obsolete (back to active status).</summary>
        Restore = 11,
        /// <summary>Other/custom (for future regulatory needs).</summary>
        Other = 1000
    }
}
