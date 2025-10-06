using System;

namespace YasGMP.Models.DTO
{
    /// <summary>
    /// Forensic log entry for CAPA case actions (GMP, 21 CFR Part 11).
    /// </summary>
    public class CapaAuditLog
    {
        /// <summary>Primary key of the audit entry.</summary>
        public int Id { get; set; }

        /// <summary>ID of the CAPA case that this event refers to.</summary>
        public int CapaId { get; set; }

        /// <summary>Action performed (CREATE/UPDATE/DELETE/APPROVE/ROLLBACK/...)</summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>User who performed the action.</summary>
        public int UserId { get; set; }

        /// <summary>Source IP address.</summary>
        public string Ip { get; set; } = string.Empty;

        /// <summary>Device/host information.</summary>
        public string Device { get; set; } = string.Empty;

        /// <summary>Optional note or comment.</summary>
        public string Note { get; set; } = string.Empty;

        /// <summary>UTC timestamp of the action.</summary>
        public DateTime ActionAt { get; set; } = DateTime.UtcNow;
    }
}

