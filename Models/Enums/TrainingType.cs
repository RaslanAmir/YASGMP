namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>TrainingType</b> – Enumerates all training, education, and competency types for GMP/CMMS users.
    /// <para>
    /// Used for staff qualification, compliance tracking, and audit reporting.
    /// </para>
    /// </summary>
    public enum TrainingType
    {
        /// <summary>General GMP training (regulations, best practices, GMP basics).</summary>
        GMP = 0,

        /// <summary>Standard Operating Procedure (SOP) training.</summary>
        SOP = 1,

        /// <summary>Machine or equipment-specific training.</summary>
        Machine = 2,

        /// <summary>Software or IT system training (ERP, QMS, CMMS, etc).</summary>
        Software = 3,

        /// <summary>Quality Management System (QMS) training.</summary>
        QMS = 4,

        /// <summary>Health, Safety, and Environment (HSE) training.</summary>
        HSE = 5,

        /// <summary>Audit/inspection preparation and response training.</summary>
        Audit = 6,

        /// <summary>Supplier/contractor qualification or onboarding.</summary>
        Supplier = 7,

        /// <summary>CAPA (Corrective and Preventive Actions) training.</summary>
        CAPA = 8,

        /// <summary>Other technical training (metrology, calibration, etc).</summary>
        Technical = 9,

        /// <summary>Leadership or management training.</summary>
        Management = 10,

        /// <summary>Custom or ad hoc training (special topics, events, etc).</summary>
        Custom = 1000
    }
}
