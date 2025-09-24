using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `system_parameters` table.</summary>
    [Table("system_parameters")]
    public class SystemParameter
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the param name.</summary>
        [Column("param_name")]
        [StringLength(100)]
        public string? ParamName { get; set; }

        /// <summary>Gets or sets the param value.</summary>
        [Column("param_value")]
        public string? ParamValue { get; set; }

        /// <summary>Gets or sets the updated by.</summary>
        [Column("updated_by")]
        public int? UpdatedBy { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Gets or sets the note.</summary>
        [Column("note")]
        public string? Note { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the digital signature.</summary>
        [Column("digital_signature")]
        [StringLength(255)]
        public string? DigitalSignature { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        public virtual User? UpdatedByNavigation { get; set; }
    }
}
