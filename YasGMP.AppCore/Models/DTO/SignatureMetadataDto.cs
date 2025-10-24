using System;

namespace YasGMP.Models.DTO
{
    /// <summary>
    /// Canonical metadata captured whenever an electronic signature is taken.
    /// </summary>
    public sealed class SignatureMetadataDto
    {
        /// <summary>Database identifier for the persisted signature record, when available.</summary>
        public int? Id { get; set; }

        /// <summary>Deterministic hash generated for the signed payload.</summary>
        public string? Hash { get; set; }

        /// <summary>Human-readable method (e.g., Password, SmartCard, AD) used to capture the signature.</summary>
        public string? Method { get; set; }

        /// <summary>Status returned by the signing workflow (e.g., Approved, Cancelled, Failed).</summary>
        public string? Status { get; set; }

        /// <summary>Optional operator-provided note or reason captured alongside the signature.</summary>
        public string? Note { get; set; }

        /// <summary>Session identifier recorded at capture time.</summary>
        public string? Session { get; set; }

        /// <summary>Client device information (workstation name, browser agent, etc.).</summary>
        public string? Device { get; set; }

        /// <summary>Optional IP address associated with the signature capture.</summary>
        public string? IpAddress { get; set; }
    }
}


