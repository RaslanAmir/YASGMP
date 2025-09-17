using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `export_audit_log` table.</summary>
    [Table("export_audit_log")]
    public class ExportAuditLog
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the device info.</summary>
        [Column("device_info")]
        [StringLength(255)]
        public string? DeviceInfo { get; set; }

        /// <summary>Gets or sets the source ip.</summary>
        [Column("source_ip")]
        [StringLength(255)]
        public string? SourceIp { get; set; }

        /// <summary>Gets or sets the filter criteria.</summary>
        [Column("filter_criteria")]
        [StringLength(255)]
        public string? FilterCriteria { get; set; }

        /// <summary>Gets or sets the file path.</summary>
        [Column("file_path")]
        [StringLength(255)]
        public string? FilePath { get; set; }

        /// <summary>Gets or sets the timestamp.</summary>
        [Column("timestamp")]
        [StringLength(255)]
        public string? Timestamp { get; set; }

        /// <summary>Gets or sets the export type.</summary>
        [Column("export_type")]
        [StringLength(255)]
        public string? ExportType { get; set; }

        /// <summary>Gets or sets the user id.</summary>
        [Column("user_id")]
        public int? UserId { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
