namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>ValidationType</b> – Enumerates all types of GMP qualification/validation records.
    /// <para>
    /// Used for assets, equipment, components, and system compliance. Directly supports 21 CFR Part 11, EU GMP, ISO 13485.
    /// </para>
    /// </summary>
    public enum ValidationType
    {
        /// <summary>Installation Qualification.</summary>
        IQ = 0,
        /// <summary>Operational Qualification.</summary>
        OQ = 1,
        /// <summary>Performance Qualification.</summary>
        PQ = 2,
        /// <summary>User Requirement Specification.</summary>
        URS = 3,
        /// <summary>Design Qualification.</summary>
        DQ = 4,
        /// <summary>Factory Acceptance Test.</summary>
        FAT = 5,
        /// <summary>Site Acceptance Test.</summary>
        SAT = 6,
        /// <summary>Computerized System Validation.</summary>
        CSV = 7,
        /// <summary>Re-qualification or re-validation.</summary>
        Requalification = 8,
        /// <summary>Initial Qualification (legacy equipment, migration).</summary>
        Initial = 9,
        /// <summary>Retrospective validation.</summary>
        Retrospective = 10,
        /// <summary>Prospective validation.</summary>
        Prospective = 11,
        /// <summary>Risk Assessment/Validation (e.g., FMEA, HACCP).</summary>
        RiskAssessment = 12,
        /// <summary>Other/custom type (for extensions).</summary>
        Other = 1000
    }
}

