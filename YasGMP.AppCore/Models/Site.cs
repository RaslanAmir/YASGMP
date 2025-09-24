using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `sites` table.</summary>
    [Table("sites")]
    public class Site
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the code.</summary>
        [Column("code")]
        [StringLength(20)]
        public string? Code { get; set; }

        /// <summary>Gets or sets the name.</summary>
        [Column("name")]
        [StringLength(100)]
        public string? Name { get; set; }

        /// <summary>Gets or sets the timezone.</summary>
        [Column("timezone")]
        [StringLength(40)]
        public string? Timezone { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
