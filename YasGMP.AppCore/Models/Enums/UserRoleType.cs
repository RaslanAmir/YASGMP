using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>UserRoleType</b> – UI- and logic-facing user roles for workflow, permissions, audit, and analytics.
    /// <para>
    /// - Used for system logic, RBAC, workflow routing, reporting, UI display (supports multi-language via <see cref="DisplayAttribute"/>).<br/>
    /// - Enables mapping to `UserRole` (technical), permissions, and auditing.<br/>
    /// - GMP, CSV, Annex 11, and 21 CFR Part 11 ready.<br/>
    /// - Designed for extensibility (add, localize, or map custom roles).
    /// </para>
    /// <remarks>
    /// <b>How to extend:</b> Add new roles with <see cref="DisplayAttribute"/> for UI localization.<br/>
    /// Keep <c>Custom</c> and reserved ranges for future-proofing and client-specific roles.
    /// </remarks>
    /// </summary>
    public enum UserRoleType
    {
        /// <summary>
        /// Basic technician/operator (can view/process assigned jobs).
        /// </summary>
        [Display(Name = "Tehničar")]
        Technician = 0,

        /// <summary>
        /// Supervisor or team lead (assigns, approves, escalates).
        /// </summary>
        [Display(Name = "Voditelj tima")]
        Supervisor = 1,

        /// <summary>
        /// Audit or quality team (audit trail, overrides, forensics).
        /// </summary>
        [Display(Name = "Auditor")]
        Auditor = 2,

        /// <summary>
        /// Department manager (full team control, budget, escalation, reports).
        /// </summary>
        [Display(Name = "Šef/Manager")]
        Manager = 3,

        /// <summary>
        /// Maintenance planner (schedules, preventive plans, resources).
        /// </summary>
        [Display(Name = "Planer održavanja")]
        Planner = 4,

        /// <summary>
        /// Storekeeper/inventory manager (warehouse, logistics, parts).
        /// </summary>
        [Display(Name = "Skladištar")]
        Storekeeper = 5,

        /// <summary>
        /// Calibration/validation officer (calibration, IQ/OQ/PQ, sign-off).
        /// </summary>
        [Display(Name = "Kalibracijski službenik")]
        CalibrationOfficer = 6,

        /// <summary>
        /// External contractor/vendor (sees only own interventions).
        /// </summary>
        [Display(Name = "Vanjski izvođač")]
        Contractor = 7,

        /// <summary>
        /// IT/system administrator (integration, security, config, backup).
        /// </summary>
        [Display(Name = "IT Administrator")]
        ITAdmin = 8,

        /// <summary>
        /// QMS admin (QMS config, process, audit, master data).
        /// </summary>
        [Display(Name = "QMS Administrator")]
        QmsAdmin = 9,

        /// <summary>
        /// Platform admin (general administration, see also SuperAdmin for ultimate authority).
        /// </summary>
        [Display(Name = "Admin")]
        Admin = 10,

        /// <summary>
        /// Super administrator (system owner, cannot be deleted, all access).
        /// </summary>
        [Display(Name = "Superadmin")]
        SuperAdmin = 99,

        /// <summary>
        /// Custom or undefined role (use for extension, migration, or integration).
        /// </summary>
        [Display(Name = "Custom/Other")]
        Custom = 1000
    }
}

