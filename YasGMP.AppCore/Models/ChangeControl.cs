using YasGMP.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents the Change Control.
    /// </summary>
    [Table("change_controls")]
    public partial class ChangeControl
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [Column("description")]
        [MaxLength(255)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [Column("title")]
        [MaxLength(255)]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the date requested raw.
        /// </summary>
        [Column("date_requested")]
        [MaxLength(255)]
        public string? DateRequestedRaw { get; set; }

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        [Column("code")]
        [MaxLength(255)]
        public string? Code { get; set; }

        /// <summary>
        /// Gets or sets the status raw.
        /// </summary>
        [Column("status")]
        [MaxLength(255)]
        public string? StatusRaw { get; set; }

        /// <summary>
        /// Represents the status value.
        /// </summary>
        [NotMapped]
        public ChangeControlStatus Status
        {
            get => Enum.TryParse(StatusRaw, true, out ChangeControlStatus parsed) ? parsed : ChangeControlStatus.Draft;
            set => StatusRaw = value.ToString();
        }

        /// <summary>
        /// Gets or sets the requested by id.
        /// </summary>
        [Column("requested_by_id")]
        public int? RequestedById { get; set; }

        /// <summary>
        /// Gets or sets the requested by.
        /// </summary>
        [ForeignKey(nameof(RequestedById))]
        public User? RequestedBy { get; set; }

        /// <summary>
        /// Gets or sets the assigned to id.
        /// </summary>
        [Column("assigned_to_id")]
        public int? AssignedToId { get; set; }

        /// <summary>
        /// Gets or sets the assigned to.
        /// </summary>
        [ForeignKey(nameof(AssignedToId))]
        public User? AssignedTo { get; set; }

        /// <summary>
        /// Gets or sets the date assigned.
        /// </summary>
        [Column("date_assigned")]
        public DateTime? DateAssigned { get; set; }

        /// <summary>
        /// Gets or sets the last modified.
        /// </summary>
        [Column("last_modified")]
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// Gets or sets the last modified by id.
        /// </summary>
        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        /// <summary>
        /// Gets or sets the last modified by.
        /// </summary>
        [ForeignKey(nameof(LastModifiedById))]
        public User? LastModifiedBy { get; set; }

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
        /// Gets or sets the digital signature.
        /// </summary>
        [Column("digital_signature")]
        [MaxLength(512)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Gets or sets the source ip.
        /// </summary>
        [Column("source_ip")]
        [MaxLength(255)]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Gets or sets the session id.
        /// </summary>
        [Column("session_id")]
        [MaxLength(255)]
        public string? SessionId { get; set; }

        /// <summary>
        /// Gets or sets the device info.
        /// </summary>
        [Column("device_info")]
        [MaxLength(255)]
        public string? DeviceInfo { get; set; }
    }
}
