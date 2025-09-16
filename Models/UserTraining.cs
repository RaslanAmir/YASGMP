using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("user_training")]
    public class UserTraining
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("training_type")]
        [StringLength(100)]
        public string? TrainingType { get; set; }

        [Column("training_date")]
        public DateTime? TrainingDate { get; set; }

        [Column("valid_until")]
        public DateTime? ValidUntil { get; set; }

        [Column("certificate_file")]
        [StringLength(255)]
        public string? CertificateFile { get; set; }

        [Column("provider")]
        [StringLength(100)]
        public string? Provider { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
