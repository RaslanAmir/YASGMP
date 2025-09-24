using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `user_esignatures` table.</summary>
    [Table("user_esignatures")]
    public class UserEsignature
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the user id.</summary>
        [Column("user_id")]
        public int? UserId { get; set; }

        /// <summary>Gets or sets the signed at.</summary>
        [Column("signed_at")]
        public DateTime? SignedAt { get; set; }

        /// <summary>Gets or sets the action.</summary>
        [Column("action")]
        [StringLength(100)]
        public string? Action { get; set; }

        /// <summary>Gets or sets the table name.</summary>
        [Column("table_name")]
        [StringLength(100)]
        public string? TableName { get; set; }

        /// <summary>Gets or sets the record id.</summary>
        [Column("record_id")]
        public int? RecordId { get; set; }

        /// <summary>Gets or sets the signature hash.</summary>
        [Column("signature_hash")]
        [StringLength(255)]
        public string? SignatureHash { get; set; }

        /// <summary>Gets or sets the method.</summary>
        [Column("method")]
        public string? Method { get; set; }

        /// <summary>Gets or sets the status.</summary>
        [Column("status")]
        public string? Status { get; set; }

        /// <summary>Gets or sets the ip address.</summary>
        [Column("ip_address")]
        [StringLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }
    }
}
