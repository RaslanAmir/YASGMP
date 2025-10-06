using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `user_training` table.</summary>
    [Table("user_training")]
    public class UserTraining
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the user id.</summary>
        [Column("user_id")]
        public int? UserId { get; set; }

        /// <summary>Gets or sets the training type.</summary>
        [Column("training_type")]
        [StringLength(100)]
        public string? TrainingType { get; set; }

        /// <summary>Gets or sets the training date.</summary>
        [Column("training_date")]
        public DateTime? TrainingDate { get; set; }

        /// <summary>Gets or sets the valid until.</summary>
        [Column("valid_until")]
        public DateTime? ValidUntil { get; set; }

        /// <summary>Gets or sets the certificate file.</summary>
        [Column("certificate_file")]
        [StringLength(255)]
        public string? CertificateFile { get; set; }

        /// <summary>Gets or sets the provider.</summary>
        [Column("provider")]
        [StringLength(100)]
        public string? Provider { get; set; }

        /// <summary>Gets or sets the status.</summary>
        [Column("status")]
        public string? Status { get; set; }

        /// <summary>Gets or sets the notes.</summary>
        [Column("notes")]
        public string? Notes { get; set; }

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

