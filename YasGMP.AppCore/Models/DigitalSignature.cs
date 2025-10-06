using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Digital signature record, schema-aligned with Region 17 in DatabaseService.
    /// Includes convenience, not-mapped properties for UI consumption.
    /// </summary>
    [Table("digital_signatures")]
    public class DigitalSignature
    {
        /// <summary>Primary key.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Target table/entity name.</summary>
        [Column("table_name")]
        [MaxLength(128)]
        public string? TableName { get; set; }

        /// <summary>Record ID in the target table.</summary>
        [Column("record_id")]
        public int RecordId { get; set; }

        /// <summary>Signing user id (FK).</summary>
        [Column("user_id")]
        public int UserId { get; set; }

        /// <summary>Digital signature hash / value.</summary>
        [Column("signature_hash")]
        [MaxLength(512)]
        public string? SignatureHash { get; set; }

        /// <summary>Signing method (pin/cert/fingerprint/...)</summary>
        [Column("method")]
        [MaxLength(64)]
        public string? Method { get; set; }

        /// <summary>Status (valid/revoked/...)</summary>
        [Column("status")]
        [MaxLength(32)]
        public string? Status { get; set; }

        /// <summary>UTC timestamp when signature was created.</summary>
        [Column("signed_at")]
        public DateTime? SignedAt { get; set; }

        /// <summary>Device information.</summary>
        [Column("device_info")]
        [MaxLength(256)]
        public string? DeviceInfo { get; set; }

        /// <summary>IP address at signing time.</summary>
        [Column("ip_address")]
        [MaxLength(64)]
        public string? IpAddress { get; set; }

        /// <summary>Optional note/reason.</summary>
        [Column("note")]
        [MaxLength(512)]
        public string? Note { get; set; }

        /// <summary>Optional session identifier captured during signing.</summary>
        [Column("session_id")]
        [MaxLength(128)]
        public string? SessionId { get; set; }

        // ---------------- Convenience (not mapped) used by ViewModels/UI ----------------

        /// <summary>Friendly user name (resolved by joins or UI layer).</summary>
        [NotMapped]
        public string? UserName { get; set; }

        /// <summary>UI alias for <see cref="SignedAt"/>.</summary>
        [NotMapped]
        public DateTime? SignedDate
        {
            get => SignedAt;
            set => SignedAt = value;
        }

        /// <summary>Convenience alias for certificate/public key info.</summary>
        [NotMapped]
        public string? CertificateInfo
        {
            get => PublicKey;
            set => PublicKey = value;
        }

        /// <summary>Whether the signature is revoked (derived from Status).</summary>
        [NotMapped]
        public bool IsRevoked => !string.IsNullOrEmpty(Status) && !Status.Equals("valid", StringComparison.OrdinalIgnoreCase);

        /// <summary>UI alias – maps to <see cref="Note"/>.</summary>
        [NotMapped]
        public string? RevocationReason
        {
            get => Note;
            set => Note = value;
        }

        /// <summary>Public key / certificate material – stored in DB if present.</summary>
        [Column("public_key")]
        public string? PublicKey { get; set; }
    }
}

