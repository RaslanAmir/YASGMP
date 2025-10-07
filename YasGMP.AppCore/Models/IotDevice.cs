using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `iot_devices` table.</summary>
    [Table("iot_devices")]
    public class IotDevice
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the device uid.</summary>
        [Column("device_uid")]
        [StringLength(120)]
        public string DeviceUid { get; set; } = string.Empty;

        /// <summary>Gets or sets the vendor.</summary>
        [Column("vendor")]
        [StringLength(120)]
        public string? Vendor { get; set; }

        /// <summary>Gets or sets the model.</summary>
        [Column("model")]
        [StringLength(120)]
        public string? Model { get; set; }

        /// <summary>Gets or sets the firmware.</summary>
        [Column("firmware")]
        [StringLength(80)]
        public string? Firmware { get; set; }

        /// <summary>Gets or sets the gateway id.</summary>
        [Column("gateway_id")]
        public int? GatewayId { get; set; }

        /// <summary>Gets or sets the last seen.</summary>
        [Column("last_seen")]
        public DateTime? LastSeen { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the gateway.
        /// </summary>
        [ForeignKey(nameof(GatewayId))]
        public virtual IotGateway? Gateway { get; set; }
    }
}
