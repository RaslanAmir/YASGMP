using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents a building within a site, storing identification codes, names, and timestamps.
    /// </summary>
    [Table("buildings")]
    public class Building
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the site id.
        /// </summary>
        [Column("site_id")]
        public int SiteId { get; set; }

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        [Column("code")]
        [StringLength(20)]
        public string? Code { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Column("name")]
        [StringLength(100)]
        public string? Name { get; set; }

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
