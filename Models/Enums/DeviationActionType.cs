using System;

namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>DeviationActionType</b> – All possible actions/events in deviation (non-conformance) lifecycle for audit/logging.
    /// <para>
    /// Used in <see cref="DeviationAudit"/> and throughout the DeviationService and deviation management workflow.<br/>
    /// Fully extensible for regulatory compliance, analytics, AI/ML, and reporting.
    /// </para>
    /// </summary>
    public enum DeviationActionType
    {
        /// <summary>
        /// Deviation record created (initial entry).
        /// </summary>
        CREATE = 0,

        /// <summary>
        /// Deviation record updated (field changes, minor edits).
        /// </summary>
        UPDATE = 1,

        /// <summary>
        /// Deviation deleted (with full audit trail).
        /// </summary>
        DELETE = 2,

        /// <summary>
        /// Investigation started (assigned to investigator).
        /// </summary>
        INVESTIGATION_START = 10,

        /// <summary>
        /// Root cause defined/documented.
        /// </summary>
        ROOT_CAUSE_DEFINED = 20,

        /// <summary>
        /// Linked to a CAPA record.
        /// </summary>
        CAPA_LINKED = 25,

        /// <summary>
        /// Deviation record closed (all actions complete).
        /// </summary>
        CLOSE = 30,

        /// <summary>
        /// Deviation reopened (after closure, if needed).
        /// </summary>
        REOPEN = 40,

        /// <summary>
        /// Deviation escalated (management notified or higher authority).
        /// </summary>
        ESCALATE = 50,

        /// <summary>
        /// Risk score changed or reassessed.
        /// </summary>
        RISK_SCORE_CHANGED = 60,

        /// <summary>
        /// Deviation reassigned to different user or department.
        /// </summary>
        REASSIGN = 70,

        /// <summary>
        /// Related attachment(s) added.
        /// </summary>
        ATTACHMENT_ADDED = 80,

        /// <summary>
        /// Related attachment(s) removed.
        /// </summary>
        ATTACHMENT_REMOVED = 81,

        /// <summary>
        /// Comment or note added.
        /// </summary>
        COMMENT_ADDED = 90,

        /// <summary>
        /// Signature or approval captured (digital/handwritten).
        /// </summary>
        SIGNED = 100,

        /// <summary>
        /// Exported for reporting or regulatory purposes.
        /// </summary>
        EXPORT = 200,

        /// <summary>
        /// System or AI anomaly detected or flagged.
        /// </summary>
        AI_ANOMALY = 300,

        /// <summary>
        /// Custom or undefined action (for future extensibility).
        /// </summary>
        OTHER = 999
    }
}
