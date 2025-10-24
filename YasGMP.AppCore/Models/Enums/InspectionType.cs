namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>InspectionType</b> – Specifies types of inspections or audits performed in YasGMP.
    /// <para>
    /// Used in inspection scheduling, audit trails, analytics, compliance reporting, and workflow automation.
    /// </para>
    /// <b>Extensible for regulatory, internal, supplier, customer, or special inspections.</b>
    /// </summary>
    public enum InspectionType
    {
        /// <summary>
        /// Internal self-inspection (performed by own staff or QA team).
        /// </summary>
        Internal = 0,

        /// <summary>
        /// HALMED (Croatian Agency for Medicinal Products and Medical Devices) inspection.
        /// </summary>
        HALMED = 1,

        /// <summary>
        /// Any other third-party or external regulatory inspection (non-HALMED).
        /// </summary>
        External = 2,

        /// <summary>
        /// Dedicated GMP compliance audit (may overlap with other audits but follows GMP standards).
        /// </summary>
        GMP = 3,

        /// <summary>
        /// Quality system or ISO/QMS audit.
        /// </summary>
        Quality = 4,

        /// <summary>
        /// Maintenance-focused inspection (preventive/corrective).
        /// </summary>
        Maintenance = 5,

        /// <summary>
        /// Any other or custom/special inspection (specify details via additional fields).
        /// </summary>
        Other = 99
    }
}

