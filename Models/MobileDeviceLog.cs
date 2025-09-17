using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `mobile_device_log` table.</summary>
    [Table("mobile_device_log")]
    public class MobileDeviceLog
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the user id.</summary>
        [Column("user_id")]
        public int? UserId { get; set; }

        /// <summary>Gets or sets the device info.</summary>
        [Column("device_info")]
        [StringLength(255)]
        public string? DeviceInfo { get; set; }

        /// <summary>Gets or sets the login time.</summary>
        [Column("login_time")]
        public DateTime? LoginTime { get; set; }

        /// <summary>Gets or sets the logout time.</summary>
        [Column("logout_time")]
        public DateTime? LogoutTime { get; set; }

        /// <summary>Gets or sets the os version.</summary>
        [Column("os_version")]
        [StringLength(50)]
        public string? OsVersion { get; set; }

        /// <summary>Gets or sets the location.</summary>
        [Column("location")]
        [StringLength(100)]
        public string? Location { get; set; }

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
