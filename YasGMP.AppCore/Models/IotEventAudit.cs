using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `iot_event_audit` table.</summary>
    [Table("iot_event_audit")]
    public class IotEventAudit
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
