using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>CapaActionType</b> – Enumerates all standardized action types for CAPA (Corrective and Preventive Actions) audit logging.
    /// <para>
    /// Used in CAPA modules, audit trails, regulatory reporting, and CAPA workflow automation.
    /// Fully aligned with GMP, ISO 13485, ICH Q10, and 21 CFR Part 11.
    /// Extensible for any future CAPA/QA/CSV scenario!
    /// </para>
    /// </summary>
    public enum CapaActionType
    {
        /// <summary>New CAPA record created.</summary>
        [Display(Name = "Create")]
        Create = 0,
        /// <summary>Legacy alias for Create (compatibility, UPPER_SNAKE_CASE).</summary>
        CREATE = Create,

        /// <summary>Existing CAPA record updated (fields, files, status, etc.).</summary>
        [Display(Name = "Update")]
        Update = 1,
        /// <summary>Legacy alias for Update (compatibility, UPPER_SNAKE_CASE).</summary>
        UPDATE = Update,

        /// <summary>CAPA investigation started (formal root cause phase).</summary>
        [Display(Name = "Investigation Start")]
        InvestigationStart = 2,
        /// <summary>Legacy alias for InvestigationStart (compatibility, UPPER_SNAKE_CASE).</summary>
        INVESTIGATION_START = InvestigationStart,

        /// <summary>Root cause analysis documented and completed.</summary>
        [Display(Name = "Root Cause Analysis")]
        RootCauseAnalysis = 3,

        /// <summary>Corrective action(s) planned and documented.</summary>
        [Display(Name = "Corrective Action Planning")]
        CorrectiveActionPlanning = 4,
        /// <summary>Legacy alias for CorrectiveActionPlanning (compatibility, UPPER_SNAKE_CASE).</summary>
        ACTION_PLAN_DEFINED = CorrectiveActionPlanning,

        /// <summary>Corrective action implemented (action taken).</summary>
        [Display(Name = "Corrective Action")]
        CorrectiveAction = 5,
        /// <summary>Legacy alias for CorrectiveAction (compatibility, UPPER_SNAKE_CASE).</summary>
        ACTION_EXECUTED = CorrectiveAction,

        /// <summary>Preventive action(s) planned and documented.</summary>
        [Display(Name = "Preventive Action Planning")]
        PreventiveActionPlanning = 6,

        /// <summary>Preventive action implemented (action taken).</summary>
        [Display(Name = "Preventive Action")]
        PreventiveAction = 7,

        /// <summary>CAPA effectiveness/verification review.</summary>
        [Display(Name = "Verification/Effectiveness")]
        Verification = 8,
        /// <summary>Legacy alias for Verification (compatibility, UPPER_SNAKE_CASE).</summary>
        VERIFICATION = Verification,

        /// <summary>CAPA formally reviewed by QA/management.</summary>
        [Display(Name = "QA/Management Review")]
        ManagementReview = 9,

        /// <summary>CAPA record closed successfully (with effectiveness check).</summary>
        [Display(Name = "Close")]
        Close = 10,
        /// <summary>Legacy alias for Close (compatibility, UPPER_SNAKE_CASE).</summary>
        CLOSE = Close,

        /// <summary>CAPA record re-opened (after closure, for follow-up or new findings).</summary>
        [Display(Name = "Reopen")]
        Reopen = 11,

        /// <summary>CAPA record deleted (requires justification and audit).</summary>
        [Display(Name = "Delete")]
        Delete = 12,
        /// <summary>Legacy alias for Delete (compatibility, UPPER_SNAKE_CASE).</summary>
        DELETE = Delete,

        /// <summary>CAPA digitally signed (user approval, acknowledgment).</summary>
        [Display(Name = "Sign/Approve")]
        Sign = 13,
        /// <summary>Legacy alias for Sign (compatibility, UPPER_SNAKE_CASE).</summary>
        APPROVE = Sign,

        /// <summary>CAPA exported (PDF, Excel, etc) for inspection/reporting.</summary>
        [Display(Name = "Export")]
        Export = 14,

        /// <summary>Manual override by admin/supervisor (requires justification).</summary>
        [Display(Name = "Override")]
        Override = 15,

        /// <summary>CAPA archived (inactive but preserved for audit).</summary>
        [Display(Name = "Archive")]
        Archive = 16,

        /// <summary>Archived CAPA restored (reopened for actions).</summary>
        [Display(Name = "Restore")]
        Restore = 17,

        /// <summary>CAPA linked to another event (incident, deviation, inspection...)</summary>
        [Display(Name = "Linked Event")]
        LinkEvent = 18,

        /// <summary>CAPA status change (generic, fallback for status workflow tracking).</summary>
        [Display(Name = "Status Change")]
        StatusChange = 19,

        /// <summary>CAPA workflow auto-escalation (AI/scheduler-triggered).</summary>
        [Display(Name = "Automated Escalation")]
        Automated = 20,

        /// <summary>CAPA custom or unknown action (for extensibility).</summary>
        [Display(Name = "Custom/Other")]
        Custom = 1000
    }
}
