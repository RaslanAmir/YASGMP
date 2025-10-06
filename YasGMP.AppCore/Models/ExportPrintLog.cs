using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `export_print_log` table.</summary>
    [Table("export_print_log")]
    public class ExportPrintLog
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the user id.</summary>
        [Column("user_id")]
        public int? UserId { get; set; }

        /// <summary>Gets or sets the export time.</summary>
        [Column("export_time")]
        public DateTime? ExportTime { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Gets or sets the format.</summary>
        [Column("format")]
        public string? Format { get; set; }

        /// <summary>Gets or sets the table name.</summary>
        [Column("table_name")]
        [StringLength(100)]
        public string? TableName { get; set; }

        /// <summary>Gets or sets the filter used.</summary>
        [Column("filter_used")]
        public string? FilterUsed { get; set; }

        /// <summary>Gets or sets the file path.</summary>
        [Column("file_path")]
        [StringLength(255)]
        public string? FilePath { get; set; }

        /// <summary>Gets or sets the source ip.</summary>
        [Column("source_ip")]
        [StringLength(45)]
        public string? SourceIp { get; set; }

        /// <summary>Gets or sets the note.</summary>
        [Column("note")]
        public string? Note { get; set; }

        /// <summary>Gets or sets the timestamp.</summary>
        [Column("timestamp")]
        public DateTime? Timestamp { get; set; }

        /// <summary>Gets or sets the details.</summary>
        [Column("details")]
        public string? Details { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }
    }
}

