using System;
using System.ComponentModel.DataAnnotations;
using YasGMP.Models.Enums;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>ValidationAudit</b> – GMP/21 CFR Part 11 compliant audit log entry for validation activities.
    /// <para>✔ Tracks every action performed on a validation (CREATE, UPDATE, EXECUTE, DELETE, etc.).</para>
    /// <para>✔ Supports forensic details, digital signatures, and full traceability.</para>
    /// </summary>
    public class ValidationAudit
    {
        /// <summary>Unique identifier for the audit record (Primary Key).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>ID of the validation to which this audit entry relates.</summary>
        [Required]
        public int ValidationId { get; set; }

        /// <summary>ID of the user who performed the action.</summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Type of action performed (CREATE, UPDATE, EXECUTE, DELETE, etc.).
        /// Uses <see cref="ValidationActionType"/> enum for standardization.
        /// </summary>
        [Required]
        public ValidationActionType Action { get; set; }

        /// <summary>Timestamp when the action occurred (UTC).</summary>
        [Required]
        public DateTime ChangedAt { get; set; }

        /// <summary>Additional details describing the action (reason, comments, context).</summary>
        [MaxLength(1000)]
        public string Details { get; set; } = string.Empty;           // CS8618 fix: default init

        /// <summary>Digital signature (SHA-256 or similar) ensuring integrity and non-repudiation.</summary>
        [MaxLength(256)]
        public string DigitalSignature { get; set; } = string.Empty;  // CS8618 fix

        /// <summary>IP address of the user/device performing the action (forensic log).</summary>
        [MaxLength(64)]
        public string SourceIp { get; set; } = string.Empty;          // CS8618 fix

        /// <summary>Device/host information where the action originated (forensic evidence).</summary>
        [MaxLength(128)]
        public string DeviceInfo { get; set; } = string.Empty;        // CS8618 fix
    }
}

