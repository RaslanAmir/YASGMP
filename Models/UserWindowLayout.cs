using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `user_window_layouts` table.</summary>
    [Table("user_window_layouts")]
    public class UserWindowLayout
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the user id.</summary>
        [Column("user_id")]
        public int UserId { get; set; }

        /// <summary>Gets or sets the page type.</summary>
        [Column("page_type")]
        [StringLength(200)]
        public string PageType { get; set; } = string.Empty;

        /// <summary>Gets or sets the pos x.</summary>
        [Column("pos_x")]
        public int? PosX { get; set; }

        /// <summary>Gets or sets the pos y.</summary>
        [Column("pos_y")]
        public int? PosY { get; set; }

        /// <summary>Gets or sets the width.</summary>
        [Column("width")]
        public int Width { get; set; }

        /// <summary>Gets or sets the height.</summary>
        [Column("height")]
        public int Height { get; set; }

        /// <summary>Gets or sets the serialized layout snapshot.</summary>
        [Column("layout_xml")]
        public string? LayoutXml { get; set; }

        /// <summary>Gets or sets the saved at.</summary>
        [Column("saved_at")]
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;

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
