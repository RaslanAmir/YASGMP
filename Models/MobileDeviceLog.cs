using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("mobile_device_log")]
    public class MobileDeviceLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("device_info")]
        [StringLength(255)]
        public string? DeviceInfo { get; set; }

        [Column("login_time")]
        public DateTime? LoginTime { get; set; }

        [Column("logout_time")]
        public DateTime? LogoutTime { get; set; }

        [Column("os_version")]
        [StringLength(50)]
        public string? OsVersion { get; set; }

        [Column("location")]
        [StringLength(100)]
        public string? Location { get; set; }

        [Column("ip_address")]
        [StringLength(45)]
        public string? IpAddress { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
