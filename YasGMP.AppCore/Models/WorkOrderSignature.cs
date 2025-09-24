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
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("work_order_id")]
        public int WorkOrderId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("signature_hash")]
        [StringLength(255)]
        public string? SignatureHash { get; set; }

        [Column("signed_at")]
        public DateTime? SignedAt { get; set; }

        [Column("pin_used")]
        [StringLength(20)]
        public string? PinUsed { get; set; }

        [Column("signature_type")]
        [StringLength(32)]
        public string SignatureTypeRaw { get; set; } = "zakljucavanje";

        [Column("note", TypeName = "text")]
        public string? Note { get; set; }

        [Column("reason_code")]
        [StringLength(64)]
        public string ReasonCode { get; set; } = string.Empty;

        [Column("reason_description")]
        [StringLength(255)]
        public string? ReasonDescription { get; set; }

        [Column("record_hash")]
        [StringLength(128)]
        public string RecordHash { get; set; } = string.Empty;

        [Column("record_version")]
        public int RecordVersion { get; set; } = 1;

        [Column("server_timezone")]
        [StringLength(64)]
        public string? ServerTimezone { get; set; }

        [Column("ip_address")]
        [StringLength(45)]
        public string? IpAddress { get; set; }

        [Column("device_info")]
        [StringLength(255)]
        public string? DeviceInfo { get; set; }

        [Column("session_id")]
        [StringLength(64)]
        public string? SessionId { get; set; }

        [Column("revision_no")]
        public int RevisionNo { get; set; } = 1;

        [Column("mfa_challenge")]
        [StringLength(128)]
        public string? MfaEvidence { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(WorkOrderId))]
        public virtual WorkOrder? WorkOrder { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

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
