using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("iot_devices")]
    public class IotDevice
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("device_uid")]
        [StringLength(120)]
        public string DeviceUid { get; set; } = string.Empty;

        [Column("vendor")]
        [StringLength(120)]
        public string? Vendor { get; set; }

        [Column("model")]
        [StringLength(120)]
        public string? Model { get; set; }

        [Column("firmware")]
        [StringLength(80)]
        public string? Firmware { get; set; }

        [Column("gateway_id")]
        public int? GatewayId { get; set; }

        [Column("last_seen")]
        public DateTime? LastSeen { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
