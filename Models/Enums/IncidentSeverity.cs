namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>IncidentSeverity</b> – Enumerates all risk and impact levels for incidents, deviations, or CAPA events.
    /// <para>
    /// Used for criticality scoring, CAPA triggers, reporting, and dashboard prioritization.
    /// </para>
    /// </summary>
    public enum IncidentSeverity
    {
        /// <summary>Low impact – minor deviation, no GMP/quality impact.</summary>
        Low = 0,

        /// <summary>Medium impact – potential quality/system issue, needs follow-up.</summary>
        Medium = 1,

        /// <summary>High impact – GMP/compliance risk, regulatory notification may be required.</summary>
        High = 2,

        /// <summary>Critical – immediate threat to product quality, patient safety, or regulatory status.</summary>
        Critical = 3,

        /// <summary>GMP/CSV specific alert (triggers full investigation and reporting).</summary>
        GMP = 10,

        /// <summary>Compliance/legal severity (may affect licensing or operation).</summary>
        Compliance = 11,

        /// <summary>Other/future extension.</summary>
        Other = 1000
    }
}
