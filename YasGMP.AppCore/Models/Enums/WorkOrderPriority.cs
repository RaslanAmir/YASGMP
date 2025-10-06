namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>WorkOrderPriority</b> – Priority level for work orders, interventions, and maintenance tasks.
    /// <para>
    /// Supports dashboards, auto-escalation, notification, and SLA response metrics.
    /// </para>
    /// </summary>
    public enum WorkOrderPriority
    {
        /// <summary>Low – can be scheduled later, not urgent.</summary>
        Low = 0,

        /// <summary>Medium – normal operational priority.</summary>
        Medium = 1,

        /// <summary>High – time-sensitive, important for operations.</summary>
        High = 2,

        /// <summary>Critical – immediate action required, potential regulatory or safety risk.</summary>
        Critical = 3,

        /// <summary>Planned – scheduled preventive or recurring maintenance.</summary>
        Planned = 4,

        /// <summary>Deferred – postponed by authorized personnel.</summary>
        Deferred = 5,

        /// <summary>Other (future escalation levels, automation, IoT trigger).</summary>
        Other = 1000
    }
}

