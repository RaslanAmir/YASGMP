using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>WorkOrderActionType</b> — All possible actions on a work order, for audit, workflow, reporting, AI, API, and automation.
    /// <para>
    /// ✅ Extensible, future-proof, and aligned with GMP/CMMS/21 CFR Part 11 requirements.<br/>
    /// ✅ Suitable for regulatory, AI, digital signature, integration, and advanced audit needs.<br/>
    /// ✅ All values are localized for Croatian/English UI.
    /// </para>
    /// </summary>
    public enum WorkOrderActionType
    {
        /// <summary>Work order was created (initial entry).</summary>
        [Display(Name = "Kreirano")]
        Create = 0,

        /// <summary>Work order details were updated.</summary>
        [Display(Name = "Ažurirano")]
        Update = 1,

        /// <summary>Work order was deleted or archived (soft/hard delete).</summary>
        [Display(Name = "Obrisano/Arhivirano")]
        Delete = 2,

        /// <summary>Status was changed (Open, Closed, In Progress, etc).</summary>
        [Display(Name = "Promjena statusa")]
        StatusChange = 3,

        /// <summary>Work order digitally or manually signed.</summary>
        [Display(Name = "Potpisano (digitalno/manualno)")]
        Sign = 4,

        /// <summary>Attachment (photo, document) added.</summary>
        [Display(Name = "Prilog dodan")]
        AttachmentAdded = 5,

        /// <summary>Attachment removed or deleted.</summary>
        [Display(Name = "Prilog uklonjen")]
        AttachmentRemoved = 6,

        /// <summary>Comment, note, or revision added.</summary>
        [Display(Name = "Komentar/revizija dodana")]
        CommentAdded = 7,

        /// <summary>Work order exported (PDF, Excel, etc).</summary>
        [Display(Name = "Izvezeno (PDF/Excel)")]
        Export = 8,

        /// <summary>Work order imported (from external system).</summary>
        [Display(Name = "Uvezeno")]
        Import = 9,

        /// <summary>Rollback or undo operation performed.</summary>
        [Display(Name = "Vraćanje promjena")]
        Rollback = 10,

        /// <summary>Incident, deviation, or CAPA linked or created.</summary>
        [Display(Name = "Incident/CAPA povezan")]
        IncidentLinked = 11,

        /// <summary>Approval, validation, or QA sign-off.</summary>
        [Display(Name = "Odobreno/Validirano")]
        Approval = 12,

        /// <summary>Automated action (AI, API, scheduler, integration).</summary>
        [Display(Name = "Automatizirana promjena")]
        Automated = 13,

        /// <summary>Work order was assigned to a user/technician.</summary>
        [Display(Name = "Dodijeljeno korisniku")]
        Assigned = 14,

        /// <summary>Work order reassigned (to new user/group).</summary>
        [Display(Name = "Ponovno dodijeljeno")]
        Reassigned = 15,

        /// <summary>Work order reviewed (QA, supervisor).</summary>
        [Display(Name = "Pregledano")]
        Reviewed = 16,

        /// <summary>Work order escalated (urgency increased, management notified).</summary>
        [Display(Name = "Eskalirano")]
        Escalated = 17,

        /// <summary>Work order rejected (QA or supervisor).</summary>
        [Display(Name = "Odbijeno")]
        Rejected = 18,

        /// <summary>Work order printed (for paper trails, GMP).</summary>
        [Display(Name = "Ispisano")]
        Printed = 19,

        /// <summary>Work order scheduled or planned.</summary>
        [Display(Name = "Planirano")]
        Scheduled = 20,

        /// <summary>Work order closed (formally ended).</summary>
        [Display(Name = "Zatvoreno")]
        Closed = 21,

        /// <summary>Work order split into multiple orders.</summary>
        [Display(Name = "Podijeljeno")]
        Split = 22,

        /// <summary>Multiple work orders merged.</summary>
        [Display(Name = "Spojeno")]
        Merged = 23,

        /// <summary>Notification sent (email, SMS, alert).</summary>
        [Display(Name = "Obavijest poslana")]
        Notified = 24,

        // ====== Reserved for future extension ======
        /// <summary>Any other custom, unknown, or system-specific action.</summary>
        [Display(Name = "Custom")]
        Custom = 1000
    }
}
