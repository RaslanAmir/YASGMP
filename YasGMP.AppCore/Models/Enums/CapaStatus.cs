namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>CapaStatus</b> – Status for Corrective and Preventive Actions (CAPA) cases, fully GMP/ISO/21 CFR Part 11 compliant.
    /// <para>
    /// Used in <see cref="YasGMP.Models.CapaCase"/>, <see cref="YasGMP.Models.CapaAudit"/>, dashboards, workflow automation, and compliance reports.
    /// Includes all legacy, UI, and extensibility aliases for maximum compatibility.
    /// </para>
    /// </summary>
    public enum CapaStatus
    {
        /// <summary>CAPA is open and requires attention/action.</summary>
        Open = 0,

        /// <summary>CAPA is under investigation, assignment, or active remediation.</summary>
        InProgress = 1,

        /// <summary>CAPA is pending management or QA approval/signoff before closure or implementation.</summary>
        AwaitingApproval = 2,

        /// <summary>CAPA has been fully resolved and closed after verification of effectiveness.</summary>
        Closed = 3,

        /// <summary>CAPA was cancelled or invalidated (GMP justification required, always audited).</summary>
        Cancelled = 4,

        /// <summary>CAPA is archived (inactive, for records/forensic trace).</summary>
        Archived = 5,

        /// <summary>Previously closed CAPA is re-opened for further actions or follow-up.</summary>
        Reopened = 6,

        /// <summary>CAPA is overdue (missed due date, escalated for review/action).</summary>
        Overdue = 7,

        /// <summary>CAPA on hold, e.g., pending input from third party or external investigation.</summary>
        OnHold = 8,

        /// <summary>CAPA is under effectiveness review period (outcome pending).</summary>
        EffectivenessCheck = 9,

        /// <summary>CAPA escalated to management, audit, or regulatory authority.</summary>
        Escalated = 10,

        // ==== EXTENDED AND ALIAS STATUSES FOR VIEWMODEL/UI AND LEGACY COMPATIBILITY ====

        /// <summary>Status for any CAPA that is actively being investigated.</summary>
        Investigation = InProgress,

        /// <summary>Status for any CAPA that has just been created and is in the open state.</summary>
        Created = Open,

        /// <summary>Status for CAPA that is under action definition (plan created but not yet executed).</summary>
        ActionDefined = InProgress,

        /// <summary>Status for CAPA where action has been executed (implementation phase).</summary>
        ActionExecuted = InProgress,

        /// <summary>Status for CAPA that is awaiting formal approval (QA, management, regulator).</summary>
        ActionApproved = AwaitingApproval,

        /// <summary>Status for CAPA currently being verified for effectiveness.</summary>
        Verification = EffectivenessCheck,

        /// <summary>Status for CAPA marked as complete and closed.</summary>
        Complete = Closed,

        // ==== ALL-CAPS ALIASES FOR ABSOLUTE COMPATIBILITY WITH ALL LEGACY CODE ====

        /// <summary>Alias for <see cref="ActionApproved"/> for ALL CAPS compatibility.</summary>
        ACTION_APPROVED = ActionApproved,
        /// <summary>Alias for <see cref="ActionDefined"/> for ALL CAPS compatibility.</summary>
        ACTION_DEFINED = ActionDefined,
        /// <summary>Alias for <see cref="ActionExecuted"/> for ALL CAPS compatibility.</summary>
        ACTION_EXECUTED = ActionExecuted,
        /// <summary>Alias for <see cref="Closed"/> for ALL CAPS compatibility.</summary>
        CLOSED = Closed,
        /// <summary>Alias for <see cref="Investigation"/> for ALL CAPS compatibility.</summary>
        INVESTIGATION = Investigation,
        /// <summary>Alias for <see cref="Open"/> for ALL CAPS compatibility.</summary>
        OPEN = Open,
        /// <summary>Alias for <see cref="Verification"/> for ALL CAPS compatibility.</summary>
        VERIFICATION = Verification,
    }
}
