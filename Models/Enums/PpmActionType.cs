// YasGMP.Models.PpmActionType.cs
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>PpmActionType</b> — Ultra-robust GMP-compliant action type enum for Preventive Maintenance Plan (PPM) audits.
    /// <para>Tracks every possible PPM lifecycle action: creation, update, execution, closure, suspension, escalation, etc.</para>
    /// <para>Extensible for future digital, IoT, AI, and advanced workflow scenarios.</para>
    /// </summary>
    public enum PpmActionType
    {
        /// <summary>PPM plan created</summary>
        [Description("Created")] 
        CREATE = 0,

        /// <summary>PPM plan updated</summary>
        [Description("Updated")]
        UPDATE = 1,

        /// <summary>PPM plan marked as executed (maintenance done)</summary>
        [Description("Executed")]
        EXECUTE = 2,

        /// <summary>PPM plan deleted</summary>
        [Description("Deleted")]
        DELETE = 3,

        /// <summary>PPM plan closed (completed, no further actions)</summary>
        [Description("Closed")]
        CLOSE = 4,

        /// <summary>PPM plan suspended (paused, not currently active)</summary>
        [Description("Suspended")]
        SUSPEND = 5,

        /// <summary>PPM plan re-activated (from suspension or closure)</summary>
        [Description("Reactivated")]
        REACTIVATE = 6,

        /// <summary>PPM plan escalated (to manager, for overdue/risk)</summary>
        [Description("Escalated")]
        ESCALATE = 7,

        /// <summary>IoT event or sensor reading linked to this PPM</summary>
        [Description("IoT Event Linked")]
        IOT_EVENT = 8,

        /// <summary>AI/ML prediction, advanced analytics, or anomaly log</summary>
        [Description("AI/Analytics")]
        AI_ANALYTICS = 9,

        /// <summary>Custom action or extensibility hook (reserved for future use)</summary>
        [Description("Custom Action")]
        CUSTOM = 99
    }
}
