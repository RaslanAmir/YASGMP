using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>AuditLogType</b> – Defines the type/category of an audit log entry.
    /// Enables precise forensic tracking and regulatory filtering across all YasGMP modules.
    /// <para>Used in all log/audit tables (system_event_log, forensic_user_change_log, capa_audit, etc).</para>
    /// </summary>
    public enum AuditLogType
    {
        /// <summary>
        /// Log for a creation event (record, user, file, etc).
        /// </summary>
        [Display(Name = "Kreiranje")]
        Create = 0,

        /// <summary>
        /// Log for an update/modify event.
        /// </summary>
        [Display(Name = "Ažuriranje")]
        Update = 1,

        /// <summary>
        /// Log for a deletion event (soft/hard delete).
        /// </summary>
        [Display(Name = "Brisanje")]
        Delete = 2,

        /// <summary>
        /// Data viewed/accessed (used for sensitive data tracking, privacy, compliance).
        /// </summary>
        [Display(Name = "Pregled podataka")]
        View = 3,

        /// <summary>
        /// Data or document export event (PDF, Excel, API, etc).
        /// </summary>
        [Display(Name = "Izvoz")]
        Export = 4,

        /// <summary>
        /// Data or document import event.
        /// </summary>
        [Display(Name = "Uvoz")]
        Import = 5,

        /// <summary>
        /// Digital or manual signature event.
        /// </summary>
        [Display(Name = "Potpisivanje")]
        Sign = 6,

        /// <summary>
        /// User login event (successful or attempted).
        /// </summary>
        [Display(Name = "Prijava korisnika")]
        UserLogin = 7,

        /// <summary>
        /// User logout event.
        /// </summary>
        [Display(Name = "Odjava korisnika")]
        UserLogout = 8,

        /// <summary>
        /// Automated or scheduled change (AI, scheduler, integration, etc).
        /// </summary>
        [Display(Name = "Automatska promjena")]
        Automated = 9,

        /// <summary>
        /// Rollback or undo operation.
        /// </summary>
        [Display(Name = "Rollback/Undo")]
        Rollback = 10,

        /// <summary>
        /// Archive operation (record, file, document).
        /// </summary>
        [Display(Name = "Arhiviranje")]
        Archive = 11,

        /// <summary>
        /// Restore or un-archive operation.
        /// </summary>
        [Display(Name = "Obnova/Restore")]
        Restore = 12,

        /// <summary>
        /// Approval or validation action.
        /// </summary>
        [Display(Name = "Odobravanje")]
        Approval = 13,

        /// <summary>
        /// CAPA module action.
        /// </summary>
        [Display(Name = "CAPA akcija")]
        CapaAction = 14,

        /// <summary>
        /// Incident reported or processed.
        /// </summary>
        [Display(Name = "Incident")]
        Incident = 15,

        /// <summary>
        /// Inspection event (internal, external, HALMED, etc).
        /// </summary>
        [Display(Name = "Inspekcija")]
        Inspection = 16,

        /// <summary>
        /// Custom or undefined event type (future proof).
        /// </summary>
        [Display(Name = "Custom/Other")]
        Custom = 1000
    }
}
