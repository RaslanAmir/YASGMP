using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YasGMP.Models.Enums;

namespace YasGMP.Models
{
    /// <summary>
    /// Captures digital signature events for work orders, including hash, signer and type metadata.
    /// </summary>
    [Table("work_order_signatures")]
    public partial class WorkOrderSignature
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the work order id.
        /// </summary>
        [Column("work_order_id")]
        public int WorkOrderId { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        [Column("user_id")]
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the signature hash.
        /// </summary>
        [Column("signature_hash")]
        [StringLength(255)]
        public string? SignatureHash { get; set; }

        /// <summary>
        /// Gets or sets the signed at.
        /// </summary>
        [Column("signed_at")]
        public DateTime? SignedAt { get; set; }

        /// <summary>
        /// Gets or sets the pin used.
        /// </summary>
        [Column("pin_used")]
        [StringLength(20)]
        public string? PinUsed { get; set; }

        /// <summary>
        /// Gets or sets the signature type raw.
        /// </summary>
        [Column("signature_type")]
        [StringLength(32)]
        public string SignatureTypeRaw { get; set; } = "zakljucavanje";

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        [Column("note", TypeName = "text")]
        public string? Note { get; set; }

        /// <summary>
        /// Gets or sets the reason code.
        /// </summary>
        [Column("reason_code")]
        [StringLength(64)]
        public string ReasonCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the reason description.
        /// </summary>
        [Column("reason_description")]
        [StringLength(255)]
        public string? ReasonDescription { get; set; }

        /// <summary>
        /// Gets or sets the record hash.
        /// </summary>
        [Column("record_hash")]
        [StringLength(128)]
        public string RecordHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the record version.
        /// </summary>
        [Column("record_version")]
        public int RecordVersion { get; set; } = 1;

        /// <summary>
        /// Gets or sets the server timezone.
        /// </summary>
        [Column("server_timezone")]
        [StringLength(64)]
        public string? ServerTimezone { get; set; }

        /// <summary>
        /// Gets or sets the ip address.
        /// </summary>
        [Column("ip_address")]
        [StringLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the device info.
        /// </summary>
        [Column("device_info")]
        [StringLength(255)]
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// Gets or sets the session id.
        /// </summary>
        [Column("session_id")]
        [StringLength(64)]
        public string? SessionId { get; set; }

        /// <summary>
        /// Gets or sets the revision no.
        /// </summary>
        [Column("revision_no")]
        public int RevisionNo { get; set; } = 1;

        /// <summary>
        /// Gets or sets the mfa evidence.
        /// </summary>
        [Column("mfa_challenge")]
        [StringLength(128)]
        public string? MfaEvidence { get; set; }

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the work order.
        /// </summary>
        [ForeignKey(nameof(WorkOrderId))]
        public virtual WorkOrder? WorkOrder { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        /// <summary>
        /// Represents the signature type value.
        /// </summary>
        [NotMapped]
        public WorkOrderSignatureType SignatureType
        {
            get
            {
                return SignatureTypeRaw switch
                {
                    "odobrenje" => WorkOrderSignatureType.Approval,
                    "potvrda" => WorkOrderSignatureType.ExecutionConfirmation,
                    "zakljucavanje" => WorkOrderSignatureType.Lock,
                    _ => Enum.TryParse(SignatureTypeRaw, true, out WorkOrderSignatureType parsed)
                        ? parsed
                        : WorkOrderSignatureType.Custom
                };
            }
            set
            {
                SignatureTypeRaw = value switch
                {
                    WorkOrderSignatureType.Approval => "odobrenje",
                    WorkOrderSignatureType.ExecutionConfirmation => "potvrda",
                    WorkOrderSignatureType.Lock => "zakljucavanje",
                    _ => value.ToString().ToLowerInvariant()
                };
            }
        }
    }
}
