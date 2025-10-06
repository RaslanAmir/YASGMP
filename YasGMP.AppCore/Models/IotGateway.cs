using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `iot_gateways` table.</summary>
    [Table("iot_gateways")]
    public class IotGateway
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the code.</summary>
        [Column("code")]
        [StringLength(80)]
        public string? Code { get; set; }

        /// <summary>Gets or sets the name.</summary>
        [Column("name")]
        [StringLength(120)]
        public string? Name { get; set; }

        /// <summary>Gets or sets the location id.</summary>
        [Column("location_id")]
        public int? LocationId { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(LocationId))]
        public virtual Location? Location { get; set; }
    }
}

