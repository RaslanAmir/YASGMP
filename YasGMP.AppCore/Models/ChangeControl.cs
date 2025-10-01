using YasGMP.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    [Table("change_controls")]
    public partial class ChangeControl
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("description")]
        [MaxLength(255)]
        public string? Description { get; set; }

        [Column("title")]
        [MaxLength(255)]
        public string? Title { get; set; }

        [Column("date_requested")]
        [MaxLength(255)]
        public string? DateRequestedRaw { get; set; }

        [Column("code")]
        [MaxLength(255)]
        public string? Code { get; set; }

        [Column("status")]
        [MaxLength(255)]
        public string? StatusRaw { get; set; }

        [NotMapped]
        public ChangeControlStatus Status
        {
            get => Enum.TryParse(StatusRaw, true, out ChangeControlStatus parsed) ? parsed : ChangeControlStatus.Draft;
            set => StatusRaw = value.ToString();
        }

        [Column("requested_by_id")]
        public int? RequestedById { get; set; }

        [ForeignKey(nameof(RequestedById))]
        public User? RequestedBy { get; set; }

        [Column("assigned_to_id")]
        public int? AssignedToId { get; set; }

        [ForeignKey(nameof(AssignedToId))]
        public User? AssignedTo { get; set; }

        [Column("date_assigned")]
        public DateTime? DateAssigned { get; set; }

        [Column("last_modified")]
        public DateTime? LastModified { get; set; }

        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        [ForeignKey(nameof(LastModifiedById))]
        public User? LastModifiedBy { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("digital_signature")]
        [MaxLength(512)]
        public string? DigitalSignature { get; set; }

        [Column("source_ip")]
        [MaxLength(255)]
        public string? SourceIp { get; set; }

        [Column("session_id")]
        [MaxLength(255)]
        public string? SessionId { get; set; }

        [Column("device_info")]
        [MaxLength(255)]
        public string? DeviceInfo { get; set; }
    }
}
