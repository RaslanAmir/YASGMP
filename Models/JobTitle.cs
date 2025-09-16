using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("job_titles")]
    public class JobTitle
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("title")]
        [StringLength(100)]
        public string? Title { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
