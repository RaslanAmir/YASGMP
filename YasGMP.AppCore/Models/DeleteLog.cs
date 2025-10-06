using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `delete_log` table.</summary>
    [Table("delete_log")]
    public class DeleteLog
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the deleted at.</summary>
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        /// <summary>Gets or sets the deleted by.</summary>
        [Column("deleted_by")]
        public int? DeletedBy { get; set; }

        /// <summary>Gets or sets the table name.</summary>
        [Column("table_name")]
        [StringLength(100)]
        public string? TableName { get; set; }

        /// <summary>Gets or sets the record id.</summary>
        [Column("record_id")]
        public int? RecordId { get; set; }

        /// <summary>Gets or sets the delete type.</summary>
        [Column("delete_type")]
        public string? DeleteType { get; set; }

        /// <summary>Gets or sets the reason.</summary>
        [Column("reason")]
        public string? Reason { get; set; }

        /// <summary>Gets or sets the recoverable.</summary>
        [Column("recoverable")]
        public bool? Recoverable { get; set; }

        /// <summary>Gets or sets the backup file.</summary>
        [Column("backup_file")]
        [StringLength(255)]
        public string? BackupFile { get; set; }

        /// <summary>Gets or sets the source ip.</summary>
        [Column("source_ip")]
        [StringLength(45)]
        public string? SourceIp { get; set; }

        /// <summary>Gets or sets the note.</summary>
        [Column("note")]
        public string? Note { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Gets or sets the timestamp.</summary>
        [Column("timestamp")]
        public DateTime? Timestamp { get; set; }

        /// <summary>Gets or sets the action.</summary>
        [Column("action")]
        [StringLength(20)]
        public string? Action { get; set; }

        /// <summary>Gets or sets the details.</summary>
        [Column("details")]
        public string? Details { get; set; }

        [ForeignKey(nameof(DeletedBy))]
        public virtual User? DeletedByNavigation { get; set; }
    }
}

