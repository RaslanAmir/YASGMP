namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>IncidentType</b> – Categorizes all types of incidents/events for regulatory, EHS, GMP, CAPA, and operational tracking.
    /// <para>
    /// Enables analytics, risk scoring, workflow automation, escalation, and compliance dashboards.
    /// Extensible for future types and localization.
    /// </para>
    /// </summary>
    public enum IncidentType
    {
        /// <summary>Generic (uncategorized) incident.</summary>
        Generic = 0,

        /// <summary>Equipment failure or breakdown.</summary>
        EquipmentFailure = 1,

        /// <summary>Utility failure (power, water, gas, HVAC, etc.).</summary>
        UtilityFailure = 2,

        /// <summary>Process deviation (process or parameter out of specification).</summary>
        Deviation = 3,

        /// <summary>Contamination incident (microbial, chemical, particulate...)</summary>
        Contamination = 4,

        /// <summary>Product defect, non-conformance, or OOS (Out of Specification).</summary>
        ProductDefect = 5,

        /// <summary>Personnel-related incident (training, behavior, access, safety).</summary>
        Personnel = 6,

        /// <summary>Health and safety incident (injury, near-miss, accident, exposure...)</summary>
        EHS = 7,

        /// <summary>Fire, explosion, or other catastrophic event.</summary>
        FireOrExplosion = 8,

        /// <summary>Loss/theft (materials, tools, product, documents...)</summary>
        LossOrTheft = 9,

        /// <summary>Data integrity incident (unauthorized access, data loss, system error...)</summary>
        DataIntegrity = 10,

        /// <summary>Cybersecurity incident (virus, ransomware, network attack...)</summary>
        CyberSecurity = 11,

        /// <summary>Utility out of range (temperature, humidity, environmental...)</summary>
        UtilityOutOfRange = 12,

        /// <summary>Alarm triggered (sensor, monitoring, physical security...)</summary>
        Alarm = 13,

        /// <summary>Unauthorized access/entry (physical or digital).</summary>
        UnauthorizedAccess = 14,

        /// <summary>Supplier or contractor issue (delay, non-conformance, documentation...)</summary>
        Supplier = 15,

        /// <summary>Regulatory or inspection finding.</summary>
        RegulatoryFinding = 16,

        /// <summary>Environmental incident (spill, pollution, emissions, waste...)</summary>
        Environmental = 17,

        /// <summary>Product recall/withdrawal.</summary>
        Recall = 18,

        /// <summary>Training gap or documentation issue.</summary>
        Training = 19,

        /// <summary>Inspection/audit deviation or observation.</summary>
        Inspection = 20,

        /// <summary>Physical security breach (intrusion, sabotage...)</summary>
        Security = 21,

        /// <summary>IT/OT infrastructure failure.</summary>
        ITInfrastructure = 22,

        /// <summary>Emergency evacuation or drill.</summary>
        Evacuation = 23,

        /// <summary>Custom or unclassified (future/extensible).</summary>
        Custom = 1000
    }
}
