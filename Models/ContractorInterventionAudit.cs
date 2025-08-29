// ============================================================================
// File: Models/ContractorInterventionAudit.cs
// Purpose: Simple POCO used by ContractorIntervention extensions/logging.
// ============================================================================

using System;

namespace YasGMP.Models
{
    public class ContractorInterventionAudit
    {
        public int Id { get; set; }
        public int InterventionId { get; set; }
        public int? UserId { get; set; }

        // e.g. "CREATE", "UPDATE", "DELETE", "ROLLBACK", "EXPORT"
        public string Action { get; set; } = string.Empty;

        public string? Details { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public string? SourceIp { get; set; }
        public string? DeviceInfo { get; set; }
        public string? SessionId { get; set; }
        public string? DigitalSignature { get; set; }

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Note { get; set; }
    }
}
