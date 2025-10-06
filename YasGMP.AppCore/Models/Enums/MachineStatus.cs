namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>MachineStatus</b> – Comprehensive status for machines/equipment/assets in a GMP/CMMS environment.
    /// <para>
    /// Supports maintenance planning, OEE, lifecycle management, regulatory tracking, dashboards, and downtime analytics.
    /// </para>
    /// </summary>
    public enum MachineStatus
    {
        /// <summary>Active (operational, in routine use).</summary>
        Active = 0,

        /// <summary>In maintenance (scheduled/preventive).</summary>
        Maintenance = 1,

        /// <summary>Corrective maintenance/repair in progress.</summary>
        Corrective = 2,

        /// <summary>Reserved (not for use, held for validation or project).</summary>
        Reserved = 3,

        /// <summary>Under qualification/validation (IQ/OQ/PQ/commissioning).</summary>
        Qualification = 4,

        /// <summary>Out of service (not currently operational, e.g., breakdown, shut down).</summary>
        OutOfService = 5,

        /// <summary>Scrapped (decommissioned and disposed).</summary>
        Scrapped = 6,

        /// <summary>Decommissioned (retired, not for use but still present onsite).</summary>
        Decommissioned = 7,

        /// <summary>Awaiting installation/start-up (new, yet to be deployed).</summary>
        AwaitingInstallation = 8,

        /// <summary>Upgraded (after major upgrade/rebuild, in post-modification state).</summary>
        Upgraded = 9,

        /// <summary>Audit hold (blocked due to audit/inspection action).</summary>
        AuditHold = 10,

        /// <summary>Quarantined (suspected non-conformance, contamination, etc).</summary>
        Quarantined = 11,

        /// <summary>Other/custom (future extensibility).</summary>
        Custom = 1000
    }
}

