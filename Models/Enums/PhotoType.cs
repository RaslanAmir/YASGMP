namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>PhotoType</b> – Enumerates all types of photos/images used in the GMP/CMMS system.
    /// <para>
    /// Used for work orders, inspections, incidents, audits, documentation, before/after, and more.
    /// Supports full traceability, AI analysis, and regulatory forensics.
    /// </para>
    /// </summary>
    public enum PhotoType
    {
        /// <summary>Photo taken before an intervention or repair (reference).</summary>
        Before = 0,

        /// <summary>Photo taken after completion of work (proof/result).</summary>
        After = 1,

        /// <summary>Incident/defect photo (evidence of a problem).</summary>
        Incident = 2,

        /// <summary>Inspection or audit photo (GMP/CSV compliance).</summary>
        Inspection = 3,

        /// <summary>Photo of documentation, certificate, report, or attachment.</summary>
        Documentation = 4,

        /// <summary>Training or SOP illustration (for records).</summary>
        Training = 5,

        /// <summary>System screenshot (for bug reporting or audit trail).</summary>
        Screenshot = 6,

        /// <summary>Signature/certification photo (e.g., signed document, signature pad).</summary>
        Signature = 7,

        /// <summary>Calibration photo (instrument/gauge, display, or certificate).</summary>
        Calibration = 8,

        /// <summary>Supplier/contractor evidence (GMP audit).</summary>
        Supplier = 9,

        /// <summary>Other/miscellaneous (future proofing).</summary>
        Other = 1000
    }
}
