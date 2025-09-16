using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("user_window_layouts")]
    public class UserWindowLayout
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("page_type")]
        [StringLength(200)]
        public string PageType { get; set; } = string.Empty;

        [Column("pos_x")]
        public int? PosX { get; set; }

        [Column("pos_y")]
        public int? PosY { get; set; }

        [Column("width")]
        public int Width { get; set; }

        [Column("height")]
        public int Height { get; set; }

        [Column("saved_at")]
        public DateTime SavedAt { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
