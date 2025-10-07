using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents a reusable checklist template definition used across inspections and workflows.
    /// </summary>
    [Table("checklist_templates")]
    public class ChecklistTemplate
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        [Column("code")]
        [StringLength(60)]
        public string? Code { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Column("name")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the created by.
        /// </summary>
        [Column("created_by")]
        public int? CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
