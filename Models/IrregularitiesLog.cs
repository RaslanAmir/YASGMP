using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("irregularities_log")]
    public class IrregularitiesLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("related_type")]
        public string? RelatedType { get; set; }

        [Column("related_id")]
        public int? RelatedId { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("detected_at")]
        public DateTime? DetectedAt { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("note")]
        public string? Note { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("details")]
        public string? Details { get; set; }
    }
}
