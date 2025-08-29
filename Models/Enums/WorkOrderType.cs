using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>WorkOrderType</b> — Categorizes every type of work order/task for the full asset lifecycle, CAPA, GMP, and regulatory compliance.
    /// <para>Supports all modern CMMS/asset management operations, inspections, audits, automation, and custom extensions.</para>
    /// </summary>
    public enum WorkOrderType
    {
        /// <summary>
        /// Scheduled preventive maintenance (PM) to avoid failures.
        /// </summary>
        [Display(Name = "Preventivni")]
        Preventive = 0,

        /// <summary>
        /// Corrective maintenance for breakdowns, faults, or incident repair.
        /// </summary>
        [Display(Name = "Korektivni")]
        Corrective = 1,

        /// <summary>
        /// Emergency/unscheduled intervention (urgent, critical).
        /// </summary>
        [Display(Name = "Vanredni")]
        Emergency = 2,

        /// <summary>
        /// Formal inspection (internal, GMP, HALMED, regulatory, periodic).
        /// </summary>
        [Display(Name = "Inspekcija")]
        Inspection = 3,

        /// <summary>
        /// Full GMP/ISO/CSV validation activity (IQ/OQ/PQ/URS/DQ/FAT/SAT).
        /// </summary>
        [Display(Name = "Validacija")]
        Validation = 4,

        /// <summary>
        /// Equipment/component calibration (metrology, instruments, sensors).
        /// </summary>
        [Display(Name = "Kalibracija")]
        Calibration = 5,

        /// <summary>
        /// New equipment or component installation (commissioning).
        /// </summary>
        [Display(Name = "Instalacija")]
        Installation = 6,

        /// <summary>
        /// Equipment/component move or transfer (relocation, repurpose).
        /// </summary>
        [Display(Name = "Premještanje")]
        Relocation = 7,

        /// <summary>
        /// CAPA-triggered work order (root cause, corrective/preventive action).
        /// </summary>
        [Display(Name = "CAPA akcija")]
        CAPA = 8,

        /// <summary>
        /// Incident or deviation management (deviation, nonconformance, audit finding).
        /// </summary>
        [Display(Name = "Incident/odstupanje")]
        Incident = 9,

        /// <summary>
        /// Regulatory or quality audit action.
        /// </summary>
        [Display(Name = "Audit/QA")]
        Audit = 10,

        /// <summary>
        /// Digitalization, documentation, or software change (automation, IT).
        /// </summary>
        [Display(Name = "Digitalizacija/IT")]
        ITChange = 11,

        /// <summary>
        /// Custom or special workflow (for expansion, legacy, integration, AI).
        /// </summary>
        [Display(Name = "Ostalo/Special")]
        Other = 100
    }
}
