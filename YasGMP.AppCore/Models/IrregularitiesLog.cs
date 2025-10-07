using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `irregularities_log` table.</summary>
    [Table("irregularities_log")]
    public class IrregularitiesLog
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the related type.</summary>
        [Column("related_type")]
        public string? RelatedType { get; set; }

        /// <summary>Gets or sets the related id.</summary>
        [Column("related_id")]
        public int? RelatedId { get; set; }

        /// <summary>Gets or sets the user id.</summary>
        [Column("user_id")]
        public int? UserId { get; set; }

        /// <summary>Gets or sets the description.</summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>Gets or sets the detected at.</summary>
        [Column("detected_at")]
        public DateTime? DetectedAt { get; set; }

        /// <summary>Gets or sets the status.</summary>
        [Column("status")]
        public string? Status { get; set; }

        /// <summary>Gets or sets the note.</summary>
        [Column("note")]
        public string? Note { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Gets or sets the details.</summary>
        [Column("details")]
        public string? Details { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }
    }
}
