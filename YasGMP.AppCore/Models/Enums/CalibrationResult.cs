namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>CalibrationResult</b> – Standardized outcome of any calibration or metrological verification process.
    /// <para>
    /// <b>Usage:</b> Used in <see cref="YasGMP.Models.Calibration"/>, <see cref="YasGMP.Models.SensorDataLog"/>, 
    /// reports, and audit logs. Supports GMP/GLP/ISO 17025 digital traceability.
    /// </para>
    /// </summary>
    public enum CalibrationResult
    {
        /// <summary>
        /// Calibration completed successfully; all values within tolerance/spec.
        /// </summary>
        Pass = 0,

        /// <summary>
        /// Calibration failed; at least one value outside of tolerance/spec.
        /// </summary>
        Fail = 1,

        /// <summary>
        /// Conditional pass – valid only under specific conditions or temporary (e.g., used with a note).
        /// </summary>
        Conditional = 2,

        /// <summary>
        /// Calibration completed, but operator/inspector added a special note or warning.
        /// </summary>
        Note = 3,

        /// <summary>
        /// Calibration was not possible due to missing equipment, access, or external factors.
        /// </summary>
        NotPerformed = 4,

        /// <summary>
        /// Calibration requires repeat/verification (e.g., environmental issue, disputed result).
        /// </summary>
        ReverifyRequired = 5,

        /// <summary>
        /// Calibration data uploaded digitally (e.g. via IoT device, cloud, LIMS), pending review.
        /// </summary>
        PendingReview = 6,

        /// <summary>
        /// Calibration result is obsolete/superseded (used for versioning/archiving).
        /// </summary>
        Archived = 99
    }
}

