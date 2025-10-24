using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `config_change_log` table.</summary>
    [Table("config_change_log")]
    public class ConfigChangeLog
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the change time.</summary>
        [Column("change_time")]
        public DateTime? ChangeTime { get; set; }

        /// <summary>Gets or sets the changed by.</summary>
        [Column("changed_by")]
        public int? ChangedBy { get; set; }

        /// <summary>Gets or sets the config name.</summary>
        [Column("config_name")]
        [StringLength(255)]
        public string? ConfigName { get; set; }

        /// <summary>Gets or sets the old value.</summary>
        [Column("old_value")]
        public string? OldValue { get; set; }

        /// <summary>Gets or sets the new value.</summary>
        [Column("new_value")]
        public string? NewValue { get; set; }

        /// <summary>Gets or sets the change type.</summary>
        [Column("change_type")]
        public string? ChangeType { get; set; }

        /// <summary>Gets or sets the note.</summary>
        [Column("note")]
        public string? Note { get; set; }

        /// <summary>Gets or sets the source ip.</summary>
        [Column("source_ip")]
        [StringLength(45)]
        public string? SourceIp { get; set; }

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

        [ForeignKey(nameof(ChangedBy))]
        public virtual User? ChangedByNavigation { get; set; }
    }
}

